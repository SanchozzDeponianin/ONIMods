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

namespace BetterPlantTending
{
    using static ModAssets;

    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            ModOptions.Reload();
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
            if (DlcManager.IsContentSubscribed(DlcManager.DLC2_ID))
                TreesPatches.SpaceTree_ResolveTooltipCallback_Patch();
        }

        #region gas consumption
        // Оксихрен
        [HarmonyPatch(typeof(OxyfernConfig), nameof(OxyfernConfig.CreatePrefab))]
        private static class OxyfernConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                __result.AddOrGet<TendedOxyfern>();
                if (ModOptions.Instance.oxyfern_fix_output_cell)
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
                __instance.GetComponent<TendedColdBreather>().ApplyModifier();
            }
        }

        // солёная лоза, ну и заодно и неиспользуемый кактус, они оба на одной основе сделаны
        [HarmonyPatch]
        private static class SaltPlantConfig_CreatePrefab
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                if (ModOptions.Instance.saltplant_adjust_gas_consumption)
                    yield return typeof(SaltPlantConfig).GetMethodSafe(nameof(SaltPlantConfig.CreatePrefab), false);
                if (DlcManager.IsExpansion1Active() && ModOptions.Instance.hydrocactus_adjust_gas_consumption)
                    yield return typeof(FilterPlantConfig).GetMethodSafe(nameof(FilterPlantConfig.CreatePrefab), false);
            }

            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<TendedSaltPlant>().consumptionRate = __result.GetComponent<ElementConsumer>().consumptionRate;
            }
        }

        // синяя алоэ и диночтототам, оба два похожи как Ctrl-C Ctrl-V
        [HarmonyPatch(typeof(BlueGrassConfig), nameof(BlueGrassConfig.CreatePrefab))]
        private static class BlueGrassConfig_CreatePrefab
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC2_ID)
                && ModOptions.Instance.blue_grass_adjust_gas_consumption;

            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<TendedBlueGrass>();
            }
        }

        [HarmonyPatch(typeof(DinofernConfig), nameof(DinofernConfig.CreatePrefab))]
        private static class DinofernConfig_CreatePrefab
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC4_ID)
                && ModOptions.Instance.dinofern_adjust_gas_consumption;

            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<TendedDinofern>();
            }
        }

        [HarmonyPatch]
        private static class BlueGrass_Dinofern_SetConsumptionRate
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                if (DlcManager.IsContentSubscribed(DlcManager.DLC2_ID)
                    && ModOptions.Instance.blue_grass_adjust_gas_consumption)
                    yield return typeof(BlueGrass).GetMethodSafe(nameof(BlueGrass.SetConsumptionRate), false);
                if (DlcManager.IsContentSubscribed(DlcManager.DLC4_ID)
                    && ModOptions.Instance.dinofern_adjust_gas_consumption)
                    yield return typeof(Dinofern).GetMethodSafe(nameof(Dinofern.SetConsumptionRate), false);
            }

            private static void Postfix(KMonoBehaviour __instance, ElementConsumer ___elementConsumer, ReceptacleMonitor __receptacleMonitor)
            {
                // тут дикость учитывается дважды: внутри Growing ххх.SetConsumptionRate
                float base_growth_rate = __receptacleMonitor.Replanted ? CROPS.GROWTH_RATE : CROPS.WILD_GROWTH_RATE;
                float multiplier = __instance.GetAttributes().Get(fakeGrowingRate.AttributeId).GetTotalValue() / base_growth_rate;
                ___elementConsumer.consumptionRate *= multiplier;
                ___elementConsumer.RefreshConsumptionRate();
            }
        }

        // возможность дафать семена
        [HarmonyPatch(typeof(Dinofern), "OnSpawn")]
        private static class Dinofern_OnSpawn
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC4_ID)
                && ModOptions.Instance.dinofern_can_give_seeds;

            private static void Postfix(Dinofern __instance)
            {
                __instance.GetComponent<SeedProducer>().seedInfo.productionType = SeedProducer.ProductionType.Harvest;
            }
        }
        #endregion
        #region CritterTrapPlant
        // растение-ловушка:
        // производство газа  пропорционально её скорости роста
        [HarmonyPatch(typeof(CritterTrapPlant.StatesInstance), nameof(CritterTrapPlant.StatesInstance.AddGas))]
        private static class CritterTrapPlant_StatesInstance_AddGas
        {
            private static float base_growth_rate;
            private static bool Prepare()
            {
                base_growth_rate = ModOptions.Instance.critter_trap_decrease_gas_production_by_wildness
                    ? CROPS.GROWTH_RATE : CROPS.WILD_GROWTH_RATE;
                return DlcManager.IsExpansion1Active()
                    && ModOptions.Instance.critter_trap_adjust_gas_production;
            }

            private static void Prefix(CritterTrapPlant.StatesInstance __instance, ref float dt)
            {
                var growth_rate = __instance.master.GetAttributes().Get(Db.Get().Amounts.Maturity.deltaAttribute.Id).GetTotalValue();
                dt *= growth_rate / base_growth_rate;
            }
        }

        // возможность дафать семена
        [HarmonyPatch(typeof(CritterTrapPlant), "OnSpawn")]
        private static class CritterTrapPlant_OnSpawn
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active()
                && ModOptions.Instance.critter_trap_can_give_seeds;

            private static void Postfix(CritterTrapPlant __instance)
            {
                __instance.GetComponent<SeedProducer>().seedInfo.productionType = SeedProducer.ProductionType.Harvest;
            }
        }
        #endregion
        #region SapTree
        // резиногое дерего:
        // делаем дерего убобряемым
        [HarmonyPatch(typeof(SapTreeConfig), nameof(SapTreeConfig.CreatePrefab))]
        private static class SapTreeConfig_CreatePrefab
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active()
                && ModOptions.Instance.allow_tinker_saptree;

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
                    // широкое на широкое, иначе дупли получают по  морде и занимаются хернёй
                    if (go.TryGetComponent(out Tinkerable tinkerable))
                    {
                        tinkerable.SetOffsetTable(OffsetGroups.InvertedWideTable);
                        go.GetComponent<Storage>().SetOffsetTable(OffsetGroups.InvertedWideTable);
                    }
                };
            }
        }

        // ускоряем поедание жранины и выделение резины
        [HarmonyPatch]
        private static class SapTree_StatesInstance_EatFoodItem_Ooze
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active()
                && ModOptions.Instance.allow_tinker_saptree;

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
            private static bool Prepare() => DlcManager.IsExpansion1Active()
                && ModOptions.Instance.allow_tinker_saptree;

            /*
            тут в норме dt = 1 поэтому можно просто умножить. если клеи поменяют, то придется городить чтото посложнее
            --- float mass = pickupable.GetComponent<Edible>().Calories * 0.001f * base.def.kcalorieToKGConversionRatio;
            +++ float mass = pickupable.GetComponent<Edible>().Calories * 0.001f * base.def.kcalorieToKGConversionRatio * dt;
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Transpile(instructions, original, transpiler);
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
        #endregion

        // подавим нотификацию когда собирается гнилой мутантный урожай
        [HarmonyPatch(typeof(RotPile), nameof(RotPile.TryCreateNotification))]
        private static class RotPile_TryCreateNotification
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();
            private static bool Prefix(RotPile __instance)
            {
                return __instance.GetProperName() != global::STRINGS.ITEMS.FOOD.ROTPILE.NAME;
            }
        }

        // дополнительные семена безурожайных растений
        [HarmonyPatch]
        private static class EntityTemplates_CreateAndRegisterSeedForPlant
        {
            private static MethodBase TargetMethod()
            {
                return typeof(EntityTemplates).GetOverloadWithMostArguments(nameof(EntityTemplates.CreateAndRegisterSeedForPlant),
                    true, typeof(GameObject), typeof(IHasDlcRestrictions));
            }

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
            private static void Postfix(Tinkerable __instance, WorkerBase worker)
            {
                if (__instance.TryGetComponent<ExtraSeedProducer>(out var producer))
                    producer.CreateExtraSeed(worker);
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
                    if (ModOptions.Instance.prevent_tending_grown_or_wilting)
                    {
                        __instance.Subscribe((int)GameHashes.Wilt, ___OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.WiltRecover, ___OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.Grow, ___OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.CropSleep, ___OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.CropWakeUp, ___OnEffectRemovedDelegate);
                        if (ModOptions.Instance.space_tree_adjust_productivity)
                            __instance.Subscribe((int)GameHashes.TagsChanged, OnTagsChanged);
                    }
                }
            }

            private static readonly Action<Tinkerable> QueueUpdateChore = typeof(Tinkerable).Detour<Action<Tinkerable>>("QueueUpdateChore");

            private static readonly EventSystem.IntraObjectHandler<Tinkerable> OnTagsChanged = new((tinkerable, data) =>
            {
                if (((TagChangedEventData)data).tag == SpaceTreePlant.SpaceTreeReadyForHarvest)
                    QueueUpdateChore(tinkerable);
            });
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
                    if (ModOptions.Instance.prevent_tending_grown_or_wilting)
                    {
                        if (__instance.HasTag(GameTags.Wilting)) // засохло
                        {
                            __result = true;
                            return;
                        }
                        // полностью выросло или не растёт
                        if (__instance.TryGetComponent<Growing>(out var growing) && (growing.ReachedNextHarvest() || !growing.IsGrowing()))
                        {
                            __result = true;
                            return;
                        }
                        // ветка сиропового дерева:
                        if (ModOptions.Instance.space_tree_adjust_productivity)
                        {
                            // ускорение сиропа включено => дерево заполнено сиропом и ожидает сбора
                            if (__instance.HasTag(SpaceTreePlant.SpaceTreeReadyForHarvest))
                            {
                                __result = true;
                                return;
                            }
                        }
                        else
                        {
                            // ускорение сиропа выключено => ветка полностью выросла
                            var stbi = __instance.GetSMI<SpaceTreeBranch.Instance>();
                            if (!stbi.IsNullOrStopped() && stbi.IsBranchFullyGrown)
                            {
                                __result = true;
                                return;
                            }
                        }
                    }
                    if (__instance.TryGetComponent<ExtraSeedProducer>(out var producer) && !producer.ShouldFarmTinkerTending)
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
        // а также доставать растений в горшках и ящиках
        [HarmonyPatch(typeof(ClimbableTreeMonitor.Instance), "FindClimbableTreeVisitor")]
        private static class ClimbableTreeMonitor_Instance_FindClimbableTree
        {
            private static bool CanReachBelow(Navigator navigator, int cell, KMonoBehaviour plant)
            {
                return navigator.CanReach(cell) || (plant.HasTag(GameTags.PlantedOnFloorVessel) && navigator.CanReach(Grid.CellBelow(cell)));
            }
            private static void AddPlant(List<KMonoBehaviour> list, KMonoBehaviour plant)
            {
                if (plant != null && plant.TryGetComponent<ExtraSeedProducer>(out var producer) && producer.ExtraSeedAvailable)
                {
                    list.Add(plant);
                }
            }
            /*
            var plant = obj as KMonoBehaviour;
            ...
        ---     if (navigator.CanReach(cell))
        +++     if (CanReachBelow(navigator, cell, plant))
            ...
                var trunk = target.GetComponent<ForestTreeSeedMonitor>();
                var locker = target.GetComponent<StorageLocker>();
        +++     AddPlant(context.targets, plant);
            ...
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions, MethodBase method)
            {
                var context_type = typeof(ClimbableTreeMonitor.Instance)
                    .GetNestedType("FindClimableTreeContext", BindingFlags.Public | BindingFlags.NonPublic);
                var context = method.GetParameters().FirstOrDefault(p => p.ParameterType == context_type);
                var targets = context_type?.GetFieldSafe("targets", false);
                var getComponent = typeof(Component).GetMethod(nameof(Component.GetComponent), Type.EmptyTypes).MakeGenericMethod(typeof(StorageLocker));
                var canReach = typeof(Navigator).GetMethodSafe(nameof(Navigator.CanReach), false, typeof(int));
                var canReachBelow = typeof(ClimbableTreeMonitor_Instance_FindClimbableTree).GetMethodSafe(nameof(CanReachBelow), true, PPatchTools.AnyArguments);
                var addPlant = typeof(ClimbableTreeMonitor_Instance_FindClimbableTree).GetMethodSafe(nameof(AddPlant), true, PPatchTools.AnyArguments);

                if (context == null || targets == null || getComponent == null || canReach == null || canReachBelow == null || addPlant == null)
                    return false;

                int i = instructions.FindIndex(inst => inst.Is(OpCodes.Isinst, typeof(KMonoBehaviour)));
                if (i == -1 || !instructions[i + 1].IsStloc())
                    return false;
                var plant = instructions[i + 1].GetMatchingLoadInstruction();

                i = instructions.FindIndex(i, inst => inst.Calls(canReach));
                if (i == -1)
                    return false;

                int j = instructions.FindIndex(i, inst => inst.Calls(getComponent));
                if (j == -1 || !instructions[j + 1].IsStloc())
                    return false;

                j += 2;
                instructions.Insert(j++, context.GetLoadArgInstruction());
                instructions.Insert(j++, new CodeInstruction(OpCodes.Ldfld, targets));
                instructions.Insert(j++, plant);
                instructions.Insert(j++, new CodeInstruction(OpCodes.Call, addPlant));

                instructions.Insert(i++, plant);
                instructions[i] = new CodeInstruction(OpCodes.Call, canReachBelow);
                return true;
            }
        }

        [HarmonyPatch(typeof(TreeClimbStates), "GetClimbableCell")]
        private static class TreeClimbStates_GetClimbableCell
        {
            private static void Postfix(TreeClimbStates.Instance smi, ref int __result)
            {
                var plant = smi.sm.target.Get(smi);
                if (plant != null && plant.HasTag(GameTags.PlantedOnFloorVessel) && smi.gameObject.TryGetComponent(out Navigator navigator))
                {
                    int below = Grid.CellBelow(__result);
                    int cost = navigator.GetNavigationCost(__result);
                    int cost_below = navigator.GetNavigationCost(below);
                    if (cost_below != PathProber.InvalidCost && (cost == PathProber.InvalidCost || cost_below < cost))
                        __result = below;
                }
            }
        }

        [HarmonyPatch(typeof(TreeClimbStates), "Rummage")]
        private static class TreeClimbStates_Rummage
        {
            private static bool Prefix(TreeClimbStates.Instance smi)
            {
                var target = smi.sm.target.Get(smi);
                if (target != null && target.TryGetComponent<ExtraSeedProducer>(out var producer))
                {
                    producer.ExtractExtraSeed();
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
                return TranspilerUtils.Transpile(instructions, original, transpiler);
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
                var plants = new List<KMonoBehaviour>();
                foreach (var entry in entries)
                {
                    var kmb = entry.obj as KMonoBehaviour;
                    if (kmb != null && (kmb.GetMyWorldId() == myWorldId)
                        && (kmb.TryGetComponent<Crop>(out _) || (kmb.TryGetComponent<ExtraSeedProducer>(out var producer) && producer.ShouldDivergentTending)))
                    {
                        plants.Add(kmb);
                    }
                }
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
                if (ModOptions.Instance.prevent_tending_grown_or_wilting && !growing.IsGrowing())
                    return true;
                return growing.ReachedNextHarvest();
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Transpile(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions, TranspilerUtils.Log log)
            {
                var smi = typeof(CropTendingStates).GetMethodSafe("FindCrop", false, typeof(CropTendingStates.Instance))?.GetParameters()
                    ?.First(p => p.ParameterType == typeof(CropTendingStates.Instance));
                var IsGrown = typeof(Growing).GetMethodSafe(nameof(Growing.IsGrown), false);
                var GetWorldItems = typeof(Components.Cmps<Crop>).GetMethodSafe(nameof(Components.Cmps<Crop>.GetWorldItems), false, typeof(int), typeof(bool));
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

        // чтобы жучинкусы могли достать до растений с любой соседней клетки
        // а не только слева справа от самой нижней или верхней (для растущих вниз)
        // чтобы могли убобрить свисающие с потолка пинчи
        [HarmonyPatch(typeof(CropTendingStates), "FindCrop")]
        private static class CropTendingStates_FindCrop_Mk2
        {
            /*
                var prefabID = crop.GetComponent<KPrefabID>();
                ...
                int cell = Grid.PosToCell(crop);
                int[] cells;
            +++ if (GetOccupiedCells(prefabID, ref cells))
            +++     goto label;
                cells = new int[] { Grid.CellLeft(cell), Grid.CellRight(cell) };
                if (prefabID.HasTag(GameTags.PlantedOnFloorVessel))
                {
                    cells = new int[] { Grid.CellLeft(cell), Grid.CellRight(cell), Grid.CellDownLeft(cell), Grid.CellDownRight(cell) };
                }
            +++ label:
                int num4 = 100;
                int num5 = Grid.InvalidCell;
            */
            // массив клетки слева и справа от реально занятых растением клеток, плюс ещё две снизу если растение в горшке
            private static bool GetOccupiedCells(KPrefabID plant, ref int[] cells)
            {
                if (plant != null && plant.TryGetComponent(out OccupyArea area))
                {
                    int pos = Grid.PosToCell(plant);
                    bool vessel = plant.HasTag(GameTags.PlantedOnFloorVessel);
                    var offsets = area.OccupiedCellsOffsets;
                    int count = offsets.Length;
                    int[] array;
                    if (vessel)
                        array = new int[(count + area.GetWidthInCells()) * 2];
                    else
                        array = new int[count * 2];
                    int i = 0;
                    for (int j = 0; j < count; j++)
                    {
                        int cell = Grid.OffsetCell(pos, offsets[j]);
                        array[i++] = Grid.CellLeft(cell);
                        array[i++] = Grid.CellRight(cell);
                    }
                    if (vessel)
                    {
                        for (int k = 0; k < count; k++)
                        {
                            var offset_below = new CellOffset(offsets[k].x, offsets[k].y - 1);
                            if (Array.IndexOf(offsets, offset_below) == -1)
                            {
                                int cell = Grid.OffsetCell(pos, offset_below);
                                array[i++] = Grid.CellLeft(cell);
                                array[i++] = Grid.CellRight(cell);
                            }
                        }
                    }
                    cells = array;
                    return true;
                }
                return false;
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL)
            {
                return TranspilerUtils.Transpile(instructions, original, IL, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions, ILGenerator IL)
            {
                var getKPrefabID = typeof(Component).GetMethodSafe(nameof(Component.GetComponent), false).MakeGenericMethod(typeof(KPrefabID));
                var posToCell = typeof(Grid).GetMethodSafe(nameof(Grid.PosToCell), true, typeof(KMonoBehaviour));
                var cellRight = typeof(Grid).GetMethodSafe(nameof(Grid.CellRight), true, typeof(int));
                var invalidCell = typeof(Grid).GetFieldSafe(nameof(Grid.InvalidCell), true);
                var getCells = typeof(CropTendingStates_FindCrop_Mk2).GetMethodSafe(nameof(GetOccupiedCells), true, PPatchTools.AnyArguments);

                if (getKPrefabID == null || posToCell == null || cellRight == null || invalidCell == null || getCells == null)
                    return false;

                int i = instructions.FindIndex(inst => inst.Calls(getKPrefabID));
                if (i == -1 || !instructions[i + 1].IsStloc())
                    return false;
                var local_KPrefabID = (LocalBuilder)instructions[i + 1].operand;

                int j = instructions.FindIndex(i, inst => inst.Calls(posToCell));
                if (j == -1 || !instructions[j + 1].IsStloc())
                    return false;

                int k = instructions.FindIndex(j, inst => inst.Calls(cellRight));
                if (k == -1 || !(instructions[k + 1].opcode == OpCodes.Stelem_I4) || !instructions[k + 2].IsStloc())
                    return false;
                var local_cells = (LocalBuilder)instructions[k + 2].operand;

                int m = instructions.FindIndex(k, inst => inst.LoadsField(invalidCell));
                if (m == -1 || !instructions[m + 1].IsStloc())
                    return false;

                int n = instructions.FindLastIndex(m, inst => inst.IsStloc(local_cells));
                if (n == -1)
                    return false;

                var label = IL.DefineLabel();
                instructions[n + 1].labels.Add(label);
                j++;
                instructions.Insert(++j, TranspilerUtils.GetLoadLocalInstruction(local_KPrefabID.LocalIndex));
                instructions.Insert(++j, TranspilerUtils.GetLoadLocalInstruction(local_cells.LocalIndex, true));
                instructions.Insert(++j, new CodeInstruction(OpCodes.Call, getCells));
                instructions.Insert(++j, new CodeInstruction(OpCodes.Brtrue, label));
                return true;
            }
        }

        // чтобы жучинкусы могли достать до растений в декоративных горшках с пола
        // также как они это могут с плантербохом
        // плюс с проверкой если сторонний мод добавил декоратифные семена в фермерские блоки
        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            foreach (var go in Assets.GetPrefabsWithComponent<PlantablePlot>())
            {
                if (go.TryGetComponent(out PlantablePlot plot) && plot.HasDepositTag(GameTags.DecorSeed)
                    && !go.TryGetComponent(out SimCellOccupier _))
                {
                    plot.tagOnPlanted = GameTags.PlantedOnFloorVessel;
                }
            }
        }

        // todo: перепроверить, особенно с новой лозой
        // вопервых исправление неконсистентности поглощения твердых удобрений засохшими растениями после загрузки сейфа
        // патчим FertilizationMonitor чтобы был больше похож на IrrigationMonitor
        // вовторых останавливаем поглощения воды/удобрений при других причинах отсутствии роста,
        // для ентого внедряем собственный компонент
        private static bool ShouldAbsorb(StateMachine.Instance smi)
        {
            bool absorb = !smi.gameObject.HasTag(GameTags.Wilting);
            if (absorb && ModOptions.Instance.prevent_fertilization_irrigation_not_growning
                && smi.gameObject.TryGetComponent<ExtendedFertilizationIrrigationMonitor>(out var monitor))
                absorb = absorb && monitor.ShouldAbsorb;
            return absorb;
        }

        [HarmonyPatch(typeof(FertilizationMonitor.Instance), nameof(FertilizationMonitor.Instance.StartAbsorbing))]
        private static class FertilizationMonitor_Instance_StartAbsorbing
        {
            private static bool Prefix(FertilizationMonitor.Instance __instance)
            {
                bool absorb = ShouldAbsorb(__instance);
                if (!absorb)
                    __instance.StopAbsorbing();
                return absorb;
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
                    .Enter(ExtendedFertilizationIrrigationMonitor.Subscribe)
                    .Exit(ExtendedFertilizationIrrigationMonitor.Unsubscribe);
            }
        }

        [HarmonyPatch(typeof(IrrigationMonitor.Instance), nameof(IrrigationMonitor.Instance.UpdateAbsorbing))]
        private static class IrrigationMonitor_Instance_UpdateIrrigation
        {
            private static void Prefix(IrrigationMonitor.Instance __instance, ref bool allow)
            {
                allow = allow && ShouldAbsorb(__instance);
            }
        }

        [HarmonyPatch(typeof(IrrigationMonitor), nameof(IrrigationMonitor.InitializeStates))]
        private static class IrrigationMonitor_InitializeStates
        {
            private static void Postfix(IrrigationMonitor __instance)
            {
                __instance.replanted.irrigated.absorbing
                    .Enter(ExtendedFertilizationIrrigationMonitor.Subscribe)
                    .Exit(ExtendedFertilizationIrrigationMonitor.Unsubscribe);
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

        // чтобы пересчитать необходимость поглощения воды/удобрений
        // когда меняется состояние ветки
        [HarmonyPatch(typeof(PlantBranch), nameof(PlantBranch.InitializeStates))]
        private static class PlantBranch_InitializeStates
        {
            private static void Postfix(PlantBranch __instance)
            {
                __instance.root.EventHandler(GameHashes.Grow, (smi, data) =>
                {
                    if (smi.HasTrunk)
                        ExtendedFertilizationIrrigationMonitor.QueueUpdateAbsorbing(smi.trunk);
                });
            }
        }

        [HarmonyPatch(typeof(SpaceTreePlant.Instance), "OnBranchWiltStateChanged")]
        private static class SpaceTreePlant_Instance_OnBranchWiltStateChanged
        {
            private static void Postfix(SpaceTreePlant.Instance __instance)
            {
                ExtendedFertilizationIrrigationMonitor.QueueUpdateAbsorbing(__instance);
            }
        }

        // чтобы пересчитать необходимость поглощения воды/удобрений
        // когда сироповое дерево не/может производить сироп
        [HarmonyPatch(typeof(SpaceTreePlant), nameof(SpaceTreePlant.InitializeStates))]
        private static class SpaceTreePlant_InitializeStates
        {
            private static void Postfix(SpaceTreePlant __instance)
            {
                __instance.production.producing
                    .Enter(ExtendedFertilizationIrrigationMonitor.QueueUpdateAbsorbing)
                    .Exit(ExtendedFertilizationIrrigationMonitor.QueueUpdateAbsorbing);
            }
        }
    }
}
