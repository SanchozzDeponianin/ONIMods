using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Klei.AI;
using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using static BetterPlantTending.BetterPlantTendingAssets;

namespace BetterPlantTending
{
    internal sealed class BetterPlantTendingPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(BetterPlantTendingPatches));
            new POptions().RegisterOptions(this, typeof(BetterPlantTendingOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            Init();
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            LoadOptions();
        }

        // Оксихрен
        [HarmonyPatch(typeof(OxyfernConfig), nameof(OxyfernConfig.CreatePrefab))]
        private static class OxyfernConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                __result.AddOrGet<TendedOxyfern>();
                if (BetterPlantTendingOptions.Instance.oxyfern_fix_output_cell)
                    __result.GetComponent<ElementConverter>().outputElements[0].outputElementOffset.y += 0.5f;
            }
        }

        [HarmonyPatch(typeof(Oxyfern), nameof(Oxyfern.SetConsumptionRate))]
        private static class Oxyfern_SetConsumptionRate
        {
            private static void Postfix(Oxyfern __instance, ElementConsumer ___elementConsumer, ElementConverter ___elementConverter)
            {
                // дикость уже учитывается внутри Oxyfern.SetConsumptionRate
                float multiplier = __instance.GetAttributes().Get(fakeGrowingRate.AttributeId).GetTotalValue() / CROPS.GROWTH_RATE;
                ___elementConsumer.consumptionRate *= multiplier;
                ___elementConsumer.RefreshConsumptionRate();
                ___elementConverter.SetWorkSpeedMultiplier(multiplier);
            }
        }

        // Холодых
        [HarmonyPatch(typeof(ColdBreatherConfig), nameof(ColdBreatherConfig.CreatePrefab))]
        private static class ColdBreatherConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                __result.AddOrGet<TendedColdBreather>().emitRads = __result.GetComponent<RadiationEmitter>()?.emitRads ?? 0f;
            }
        }

        [HarmonyPatch(typeof(ColdBreather), "OnReplanted")]
        private static class ColdBreather_OnReplanted
        {
            private static void Postfix(ColdBreather __instance)
            {
                __instance.GetComponent<TendedColdBreather>()?.ApplyModifier();
            }
        }

        // деревянное дерево
        // в ванили отдельные ветки не будут убобрять после загрузки сейва
        // так как до них не доходит событие OnUpdateRoom
        // потому что ветки забанены в RoomProberе
        // чиним это
        [HarmonyPatch(typeof(BuddingTrunk), "OnPrefabInit")]
        private static class BuddingTrunk_OnPrefabInit
        {
            private static readonly EventSystem.IntraObjectHandler<BuddingTrunk> OnUpdateRoomDelegate =
                new EventSystem.IntraObjectHandler<BuddingTrunk>((component, data) => RetriggerOnUpdateRoom(component, data));

            private static void RetriggerOnUpdateRoom(BuddingTrunk trunk, object data)
            {
                for (int i = 0; i < ForestTreeConfig.NUM_BRANCHES; i++)
                {
                    trunk.GetBranchAtPosition(i)?.Trigger((int)GameHashes.UpdateRoom, data);
                }
            }

            private static void Postfix(BuddingTrunk __instance)
            {
                if (BetterPlantTendingOptions.Instance.tree_fix_tinkering_branches)
                    __instance.Subscribe((int)GameHashes.UpdateRoom, OnUpdateRoomDelegate);
            }
        }

        // разблокируем возможность мутации
        [HarmonyPatch(typeof(BuddingTrunk), "ExtractExtraSeed")]
        private static class BuddingTrunk_ExtractExtraSeed
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            private static GameObject AddMutation(GameObject seed, BuddingTrunk trunk)
            {
                if (BetterPlantTendingOptions.Instance.tree_unlock_mutation)
                {
                    var trunk_mutant = trunk.GetComponent<MutantPlant>();
                    var seed_mutant = seed.GetComponent<MutantPlant>();
                    if (trunk_mutant != null && trunk_mutant.IsOriginal
                        && seed_mutant != null && trunk.GetComponent<SeedProducer>().RollForMutation())
                    {
                        seed_mutant.Mutate();
                    }
                }
                return seed;
            }
            /*
            ...
                Util.KInstantiate(Assets.GetPrefab("ForestTreeSeed"), position)
            +++     .AddMutation(this)
                    .SetActive(true);
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var kInstantiate = typeof(Util).GetMethodSafe(nameof(Util.KInstantiate), true, typeof(GameObject), typeof(Vector3));
                var addMutation = typeof(BuddingTrunk_ExtractExtraSeed).GetMethodSafe(nameof(AddMutation), true, PPatchTools.AnyArguments);
                if (kInstantiate != null && addMutation != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(kInstantiate))
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, addMutation));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // применяем мутацию от ствола на ветку
        [HarmonyPatch(typeof(TreeBud), nameof(TreeBud.SetTrunkPosition))]
        private static class TreeBud_SetTrunkPosition
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            private static readonly IDetouredField<PlantSubSpeciesCatalog, HashSet<Tag>> identifiedSubSpecies =
                PDetours.DetourField<PlantSubSpeciesCatalog, HashSet<Tag>>("identifiedSubSpecies");

            private static void Postfix(TreeBud __instance, BuddingTrunk budding_trunk)
            {
                var budding_mutant = budding_trunk?.GetComponent<MutantPlant>();
                if (budding_mutant != null && !budding_mutant.IsOriginal)
                {
                    var branch_mutant = __instance.GetComponent<MutantPlant>();
                    budding_mutant.CopyMutationsTo(branch_mutant);
                    // принудительно но по тихому идентифицируем мутацию ветки
                    identifiedSubSpecies.Get(PlantSubSpeciesCatalog.Instance).Add(branch_mutant.SubSpeciesID);
                }
            }
        }

        // солёная лоза, ну и заодно и неиспользуемый кактус, они оба на одной основе сделаны
        [HarmonyPatch]
        private static class SaltPlantConfig_CreatePrefab
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new List<MethodBase>()
                {
                    typeof(SaltPlantConfig).GetMethodSafe(nameof(SaltPlantConfig.CreatePrefab), false),
                    typeof(FilterPlantConfig).GetMethodSafe(nameof(FilterPlantConfig.CreatePrefab), false),
                };
            }

            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<TendedSaltPlant>().consumptionRate = __result.GetComponent<ElementConsumer>().consumptionRate;
            }
        }

        // растение-ловушка:
        // производство газа  пропорционально её скорости роста
        [HarmonyPatch(typeof(CritterTrapPlant.StatesInstance), nameof(CritterTrapPlant.StatesInstance.AddGas))]
        private static class CritterTrapPlant_StatesInstance_AddGas
        {
            private static void Prefix(CritterTrapPlant.StatesInstance __instance, ref float dt)
            {
                if (BetterPlantTendingOptions.Instance.critter_trap_adjust_gas_production)
                {
                    var growth_rate = __instance.master.GetAttributes().Get(Db.Get().Amounts.Maturity.deltaAttribute.Id).GetTotalValue();
                    var base_growth_rate = BetterPlantTendingOptions.Instance.critter_trap_decrease_gas_production_by_wildness ? CROPS.GROWTH_RATE : CROPS.WILD_GROWTH_RATE;
                    dt *= growth_rate / base_growth_rate;
                }
            }
        }

        // возможность дафать семена
        [HarmonyPatch(typeof(CritterTrapPlant), "OnSpawn")]
        private static class CritterTrapPlant_OnSpawn
        {
            private static void Postfix(CritterTrapPlant __instance)
            {
                if (BetterPlantTendingOptions.Instance.critter_trap_can_give_seeds)
                    __instance.GetComponent<SeedProducer>().seedInfo.productionType = SeedProducer.ProductionType.Harvest;
            }
        }

        // резиногое дерего:
        // делаем дерего убобряемым
        [HarmonyPatch(typeof(SapTreeConfig), nameof(SapTreeConfig.CreatePrefab))]
        private static class SapTreeConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                var prefabID = __result.GetComponent<KPrefabID>();
                prefabID.AddTag(GameTags.Plant);
                prefabID.prefabInitFn += go =>
                {
                    var attributes = go.GetAttributes();
                    attributes.Add(Db.Get().Amounts.Maturity.deltaAttribute);
                    attributes.Add(fakeGrowingRate);
                };
                prefabID.prefabSpawnFn += go =>
                {
                    var tinkerable = go.GetComponent<Tinkerable>();
                    // широкое на широкое, иначе дупли получают по  морде и занимаются хернёй
                    if (BetterPlantTendingOptions.Instance.allow_tinker_saptree)
                    {
                        tinkerable.SetOffsetTable(OffsetGroups.InvertedWideTable);
                        go.GetComponent<Storage>().SetOffsetTable(OffsetGroups.InvertedWideTable);
                    }
                    else
                        tinkerable.tinkerMaterialTag = GameTags.Void;
                };
            }
        }

        // ускоряем поедание жранины и выделение резины
        [HarmonyPatch]
        private static class SapTree_StatesInstance_EatFoodItem_Ooze
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new List<MethodBase>()
                {
                    typeof(SapTree.StatesInstance).GetMethodSafe(nameof(SapTree.StatesInstance.EatFoodItem), false, PPatchTools.AnyArguments),
                    typeof(SapTree.StatesInstance).GetMethodSafe(nameof(SapTree.StatesInstance.Ooze), false, PPatchTools.AnyArguments),
                };
            }

            private static void Prefix(SapTree.StatesInstance __instance, ref float dt)
            {
                dt *= __instance.gameObject.GetAttributes().Get(fakeGrowingRate.AttributeId).GetTotalValue() / CROPS.GROWTH_RATE;
            }
        }

        // ускоряем конверсию жранины в резины
        [HarmonyPatch(typeof(SapTree.StatesInstance), nameof(SapTree.StatesInstance.EatFoodItem))]
        private static class SapTree_StatesInstance_EatFoodItem
        {
            /*
            тут в норме dt = 1 поэтому можно просто умножить. если клеи поменяют, то придется городить чтото посложнее
            --- float mass = pickupable.GetComponent<Edible>().Calories * 0.001f * base.def.kcalorieToKGConversionRatio;
            +++ float mass = pickupable.GetComponent<Edible>().Calories * 0.001f * base.def.kcalorieToKGConversionRatio * dt;
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var Ratio = typeof(SapTree.Def).GetFieldSafe(nameof(SapTree.Def.kcalorieToKGConversionRatio), false);
                var dt = typeof(SapTree.StatesInstance).GetMethodSafe(nameof(SapTree.StatesInstance.EatFoodItem), false, typeof(float))?.GetParameters()?.First(p => p.ParameterType == typeof(float) && p.Name == "dt");
                if (Ratio != null && dt != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].LoadsField(Ratio))
                        {
                            instructions.Insert(++i, TranspilerUtils.GetLoadArgInstruction(dt));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Mul));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // дополнительные семена безурожайных растений
        [HarmonyPatch(typeof(EntityTemplates), nameof(EntityTemplates.CreateAndRegisterSeedForPlant))]
        private static class EntityTemplates_CreateAndRegisterSeedForPlant
        {
            private static void Postfix(GameObject plant)
            {
                if (plant.GetComponent<Crop>() == null)
                {
                    plant.AddOrGet<ExtraSeedProducer>().isNotDecorative = plant.HasTag(OxyfernConfig.ID) || plant.HasTag(ColdBreatherConfig.ID);
                    Tinkerable.MakeFarmTinkerable(plant);
                }
            }
        }

        // заспавним доп семя при убобрении фермерами
        [HarmonyPatch(typeof(Tinkerable), "OnCompleteWork")]
        private static class Tinkerable_OnCompleteWork
        {
            private static void Postfix(Tinkerable __instance, Worker worker)
            {
                __instance.GetComponent<ExtraSeedProducer>()?.CreateExtraSeed(worker);
            }
        }

        // предотвращаем убобрение фермерами 
        // если растение засохло или полностью выросло
        // или декоротивное доп семя заспавнилось
        // при изменении состояния растения нужно перепроверить задачу
        // заодно чиним что качается механика заместо фермерства
        [HarmonyPatch(typeof(Tinkerable), "OnPrefabInit")]
        private static class Tinkerable_OnPrefabInit
        {
            private static void Postfix(Tinkerable __instance, ref AttributeConverter ___attributeConverter, ref string ___skillExperienceSkillGroup, EventSystem.IntraObjectHandler<Tinkerable> ___OnEffectRemovedDelegate)
            {
                if (__instance.tinkerMaterialTag == FarmStationConfig.TINKER_TOOLS)
                {
                    ___attributeConverter = Db.Get().AttributeConverters.PlantTendSpeed;
                    ___skillExperienceSkillGroup = Db.Get().SkillGroups.Farming.Id;
                    // чтобы обновить чору после того как белка извлекла семя
                    __instance.Subscribe((int)GameHashes.SeedProduced, ___OnEffectRemovedDelegate);
                    // чтобы обновить чору когда растение засыхает/растёт/выросло
                    if (BetterPlantTendingOptions.Instance.prevent_tending_grown_or_wilting)
                    {
                        __instance.Subscribe((int)GameHashes.Wilt, ___OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.WiltRecover, ___OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.Grow, ___OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.CropSleep, ___OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.CropWakeUp, ___OnEffectRemovedDelegate);
                    }
                }
            }
        }

        // если убобрение не нужно - эмулируем как будто оно уже есть
        [HarmonyPatch(typeof(Tinkerable), "HasEffect")]
        private static class Tinkerable_HasEffect
        {
            private static void Postfix(Tinkerable __instance, ref bool __result)
            {
                if (__result)
                    return;
                if (__instance.tinkerMaterialTag == FarmStationConfig.TINKER_TOOLS)
                {
                    if (BetterPlantTendingOptions.Instance.prevent_tending_grown_or_wilting)
                    {
                        if (__instance.HasTag(GameTags.Wilting)) // засохло
                        {
                            __result = true;
                            return;
                        }
                        var growing = __instance.GetComponent<Growing>();
                        if (growing != null && (growing.ReachedNextHarvest() || !growing.IsGrowing())) // полностью выросло или не растёт
                        {
                            __result = true;
                            return;
                        }
                    }
                    if (!__instance.GetComponent<ExtraSeedProducer>()?.ShouldFarmTinkerTending ?? false)
                    {
                        __result = true;
                        return;
                    }
                }
            }
        }

        // ишшо одно место чиним что качается механика заместо фермерства
        [HarmonyPatch(typeof(TinkerStation), "OnPrefabInit")]
        private static class TinkerStation_OnPrefabInit
        {
            private static void Postfix(TinkerStation __instance, ref AttributeConverter ___attributeConverter, ref string ___skillExperienceSkillGroup)
            {
                if (__instance.outputPrefab == FarmStationConfig.TINKER_TOOLS)
                {
                    ___attributeConverter = Db.Get().AttributeConverters.HarvestSpeed;
                    ___skillExperienceSkillGroup = Db.Get().SkillGroups.Farming.Id;
                }
            }
        }

        // научиваем белочек делать экстракцию декоративных безурожайных семян
        [HarmonyPatch(typeof(ClimbableTreeMonitor.Instance), "FindClimbableTree")]
        private static class ClimbableTreeMonitor_Instance_FindClimbableTree
        {
            private static void AddPlant(List<KMonoBehaviour> list, KMonoBehaviour plant)
            {
                if (plant != null && plant.TryGetComponent<ExtraSeedProducer>(out var producer) && producer.ExtraSeedAvailable)
                {
                    list?.Add(plant);
                }
            }
            /*
            var targets = ListPool<KMonoBehaviour, ClimbableTreeMonitor>.Allocate(); 
            ...
                var budding_trunk = target.GetComponent<BuddingTrunk>();
			    var locker = target.GetComponent<StorageLocker>();
        +++     AddPlant(targets, target);
            ...
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions, TranspilerUtils.Log log)
            {
                var allocate = typeof(ListPool<KMonoBehaviour, ClimbableTreeMonitor>).GetMethodSafe(nameof(ListPool<KMonoBehaviour, ClimbableTreeMonitor>.Allocate), true);
                var getComponent = typeof(Component).GetMethod(nameof(Component.GetComponent), Type.EmptyTypes).MakeGenericMethod(typeof(StorageLocker));
                var addPlant = typeof(ClimbableTreeMonitor_Instance_FindClimbableTree).GetMethodSafe(nameof(AddPlant), true, PPatchTools.AnyArguments);
                if (allocate != null && getComponent != null && addPlant != null)
                {
                    CodeInstruction ldloc_list = null;
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(allocate) && instructions[i + 1].IsStloc())
                        {
                            ldloc_list = TranspilerUtils.GetMatchingLoadInstruction(instructions[i + 1]);
                            log.Step(1);
                            continue;
                        }
                        if (ldloc_list != null && instructions[i].Calls(getComponent) && instructions[i - 1].IsLdloc())
                        {
                            var ldloc_target = new CodeInstruction(instructions[i - 1]);
                            ++i;
                            instructions.Insert(++i, ldloc_list);
                            instructions.Insert(++i, ldloc_target);
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, addPlant));
                            log.Step(2);
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(TreeClimbStates), "Rummage")]
        private static class TreeClimbStates_Rummage
        {
            private static bool Prefix(TreeClimbStates.Instance smi)
            {
                var extraSeedProducer = smi.sm.target.Get(smi)?.GetComponent<ExtraSeedProducer>();
                if (extraSeedProducer != null)
                {
                    extraSeedProducer.ExtractExtraSeed();
                    return false;
                }
                return true;
            }
        }

        // исправление для отображения шанса доп семян в интерфейсе ферм и кодексе.
        // заодно начал отображаться и декор.
        // а то обычно для неурожайных растений "эффекты" вообще не отображаются.
        // грязновато. но ладно.
        [HarmonyPatch(typeof(GameUtil), nameof(GameUtil.GetPlantEffectDescriptors))]
        private static class GameUtil_GetPlantEffectDescriptors
        {
            private static Component GetTwoComponents(GameObject go)
            {
                if (go != null)
                {
                    if (go.TryGetComponent<Growing>(out var growing))
                        return growing;
                    if (go.TryGetComponent<ExtraSeedProducer>(out var producer))
                        return producer;
                }
                return null;
            }
            /*
        --- Growing growing = go.GetComponent<Growing>();
        +++ Growing growing = go.GetComponent<Growing>() ?? go.GetComponent<ExtraSeedProducer>();
	        bool flag = growing == null;
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var getComponent = typeof(GameObject).GetMethod(nameof(GameObject.GetComponent), Type.EmptyTypes).MakeGenericMethod(typeof(Growing));
                var getTwoComponents = typeof(GameUtil_GetPlantEffectDescriptors).GetMethodSafe(nameof(GetTwoComponents), true, PPatchTools.AnyArguments);
                if (getComponent != null && getTwoComponents != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(getComponent))
                        {
                            instructions[i] = new CodeInstruction(OpCodes.Call, getTwoComponents);
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // научиваем жучинкусов убобрять безурожайные растения, такие как холодых и оксихрен, и декоративочка
        // использован достаточно грязный хак. но все работает.
        [HarmonyPatch(typeof(CropTendingStates), "FindCrop")]
        private static class CropTendingStates_FindCrop
        {
            private static readonly int radius = (int)Math.Sqrt((int)(typeof(CropTendingStates).GetFieldSafe("MAX_SQR_EUCLIDEAN_DISTANCE", true)?.GetRawConstantValue() ?? 625));

            // поиск растений для убобрения.
            // Клеи зачем-то перебирают список всех урожайных растений на карте
            // а мы заменим на GameScenePartitioner
            private static List<KMonoBehaviour> FindPlants(CropTendingStates.Instance smi)
            {
                int myWorldId = smi.gameObject.GetMyWorldId();
                var entries = ListPool<ScenePartitionerEntry, GameScenePartitioner>.Allocate();
                var search_extents = new Extents(Grid.PosToCell(smi.master.transform.GetPosition()), radius);
                GameScenePartitioner.Instance.GatherEntries(search_extents, GameScenePartitioner.Instance.plants, entries);
                var plants = entries
                    .Select((e) => e.obj as KMonoBehaviour)
                    .Where((kmb) => (kmb.GetMyWorldId() == myWorldId)
                        && ((kmb.GetComponent<Crop>() != null) || (kmb.GetComponent<ExtraSeedProducer>()?.ShouldDivergentTending ?? false)))
                    .ToList();
                entries.Recycle();
                return plants;
            }

            // проверка что растение нуждается в убобрении.
            // растения не нужно убобрять если полностью выросли или не растут
            // заменяем простую клеевскую проверку на эту:
            private static bool IsNotNeedTending(Growing growing)
            {
                if (growing == null)
                    return true;
                if (BetterPlantTendingOptions.Instance.prevent_tending_grown_or_wilting && !growing.IsGrowing())
                    return true;
                return growing.ReachedNextHarvest();
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions, TranspilerUtils.Log log)
            {
                var smi = typeof(CropTendingStates).GetMethodSafe("FindCrop", false, typeof(CropTendingStates.Instance))?.GetParameters()
                    ?.First(p => p.ParameterType == typeof(CropTendingStates.Instance));
                var IsGrown = typeof(Growing).GetMethodSafe(nameof(Growing.IsGrown), false);
                var GetWorldItems = typeof(Components.Cmps<Crop>).GetMethodSafe(nameof(Components.Cmps<Crop>.GetWorldItems), false, PPatchTools.AnyArguments);
                var findPlants = typeof(CropTendingStates_FindCrop).GetMethodSafe(nameof(FindPlants), true, PPatchTools.AnyArguments);
                var isNotNeedTending = typeof(CropTendingStates_FindCrop).GetMethodSafe(nameof(IsNotNeedTending), true, PPatchTools.AnyArguments);

                bool result1 = false, result2 = false;
                if (smi != null && IsGrown != null && GetWorldItems != null && findPlants != null && isNotNeedTending != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        var instruction = instructions[i];
                        /*
                        foreach (Crop worldItem in 
                    ---         Components.Crops.GetWorldItems(smi.gameObject.GetMyWorldId(), false))
                    +++         FindPlants(smi))
                        */
                        if (instruction.Calls(GetWorldItems))
                        {
                            for (int j = 0; j < GetWorldItems.GetParameters().Length; j++)
                                instructions.Insert(i++, new CodeInstruction(OpCodes.Pop));
                            instructions.Insert(i++, TranspilerUtils.GetLoadArgInstruction(smi));
                            instructions[i] = new CodeInstruction(OpCodes.Call, findPlants);
                            log.Step(1);
                            result1 = true;
                        }
                        /*        
                        Growing growing = worldItem.GetComponent<Growing>();
                    ---	if (!(growing != null && growing.IsGrown()) && блаблабла ...)
                    +++ if (!(growing != null && isNotNeedTending(growing)) && блаблабла ...)
                        */
                        if (instruction.Calls(IsGrown))
                        {
                            instructions[i] = new CodeInstruction(OpCodes.Call, isNotNeedTending);
                            log.Step(2);
                            result2 = true;
                            break;
                        }
                    }
                }
                return result1 && result2;
            }
        }

        // вопервых исправление неконсистентности поглощения твердых удобрений засохшими растениями после загрузки сейфа
        // патчим FertilizationMonitor чтобы был больше похож на IrrigationMonitor
        // вовторых останавливаем поглощения воды/удобрений при других причинах отсутствии роста,
        // с учетом нюансов: ветки дерева
        // для ентого внедряем собственный компонент
        [HarmonyPatch(typeof(FertilizationMonitor.Instance), nameof(FertilizationMonitor.Instance.StartAbsorbing))]
        private static class FertilizationMonitor_Instance_StartAbsorbing
        {
            private static bool Prefix(FertilizationMonitor.Instance __instance)
            {
                if (__instance.gameObject.HasTag(GameTags.Wilting)
                    || (BetterPlantTendingOptions.Instance.prevent_fertilization_irrigation_not_growning
                        && !__instance.GetComponent<ExtendedFertilizationIrrigationMonitor>().ShouldAbsorbing()))
                {
                    __instance.StopAbsorbing();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(FertilizationMonitor), nameof(FertilizationMonitor.InitializeStates))]
        private static class FertilizationMonitor_InitializeStates
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return PPatchTools.ReplaceConstant(instructions, (int)GameHashes.WiltRecover, (int)GameHashes.TagsChanged, true);
            }

            private static void Postfix(FertilizationMonitor __instance)
            {
                __instance.replanted.fertilized.absorbing
                    .Enter(smi => smi.GetComponent<ExtendedFertilizationIrrigationMonitor>().Subscribe())
                    .Exit(smi => smi.GetComponent<ExtendedFertilizationIrrigationMonitor>().Unsubscribe());
            }
        }

        [HarmonyPatch(typeof(IrrigationMonitor.Instance), nameof(IrrigationMonitor.Instance.UpdateAbsorbing))]
        private static class IrrigationMonitor_Instance_UpdateIrrigation
        {
            private static void Prefix(IrrigationMonitor.Instance __instance, ref bool allow)
            {
                allow = allow && !__instance.gameObject.HasTag(GameTags.Wilting);
                if (BetterPlantTendingOptions.Instance.prevent_fertilization_irrigation_not_growning)
                    allow = allow && __instance.GetComponent<ExtendedFertilizationIrrigationMonitor>().ShouldAbsorbing();
            }
        }

        [HarmonyPatch(typeof(IrrigationMonitor), nameof(IrrigationMonitor.InitializeStates))]
        private static class IrrigationMonitor_InitializeStates
        {
            private static void Postfix(IrrigationMonitor __instance)
            {
                __instance.replanted.irrigated.absorbing
                    .Enter(smi => smi.GetComponent<ExtendedFertilizationIrrigationMonitor>().Subscribe())
                    .Exit(smi => smi.GetComponent<ExtendedFertilizationIrrigationMonitor>().Unsubscribe());
            }
        }

        [HarmonyPatch]
        private static class EntityTemplates_ExtendPlantToFertilizableIrrigated
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new List<MethodBase>()
                {
                    typeof(EntityTemplates).GetMethodSafe(nameof(EntityTemplates.ExtendPlantToFertilizable), true, PPatchTools.AnyArguments),
                    typeof(EntityTemplates).GetMethodSafe(nameof(EntityTemplates.ExtendPlantToIrrigated), true, typeof(GameObject), typeof(PlantElementAbsorber.ConsumeInfo[])),
                };
            }

            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<ExtendedFertilizationIrrigationMonitor>();
            }
        }

        // ретриггерим от веток дерева на ствол, чтобы пересчитать необходимость поглощения воды/удобрений
        private static readonly EventSystem.IntraObjectHandler<TreeBud> OnGrowDelegate =
            new EventSystem.IntraObjectHandler<TreeBud>((component, data) =>
                component?.buddingTrunk?.Get()?.Trigger((int)GameHashes.Grow));

        [HarmonyPatch(typeof(TreeBud), "SubscribeToTrunk")]
        private static class TreeBud_SubscribeToTrunk
        {
            private static void Postfix(TreeBud __instance)
            {
                if (BetterPlantTendingOptions.Instance.prevent_fertilization_irrigation_not_growning)
                    __instance.Subscribe((int)GameHashes.Grow, OnGrowDelegate);
            }
        }

        [HarmonyPatch(typeof(TreeBud), "UnsubscribeToTrunk")]
        private static class TreeBud_UnsubscribeToTrunk
        {
            private static void Postfix(TreeBud __instance)
            {
                __instance.Unsubscribe((int)GameHashes.Grow, OnGrowDelegate, true);
            }
        }
    }
}
