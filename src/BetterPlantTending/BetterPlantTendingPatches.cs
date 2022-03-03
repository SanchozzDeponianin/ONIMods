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

        [PLibMethod(RunAt.AfterModsLoad)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS)/*, writeStringsTemplate: true*/);
        }

        // добавляем атрибуты и модификаторы 
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
            }
        }

        [HarmonyPatch(typeof(Oxyfern), nameof(Oxyfern.SetConsumptionRate))]
        private static class Oxyfern_SetConsumptionRate
        {
            private static void Postfix(Oxyfern __instance, ElementConsumer ___elementConsumer, ElementConverter ___elementConverter)
            {
                float multiplier = __instance.GetAttributes().Get(OxyfernThroughput).GetTotalValue();
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
                __result.AddOrGet<TendedColdBreather>();
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

#if false
        // todo: нужно перепроверить это!
        // фикс для элементконсумера, чтобы статуситем не исчезал после обновления
        [HarmonyPatch(typeof(ElementConsumer), "UpdateStatusItem")]
        private static class ElementConsumer_UpdateStatusItem
        {
            private static void Prefix(ElementConsumer __instance, ref System.Guid ___statusHandle, KSelectable ___selectable)
            {
                if (__instance.showInStatusPanel && ___statusHandle != System.Guid.Empty)
                {
                    ___selectable.RemoveStatusItem(___statusHandle);
                    ___statusHandle = System.Guid.Empty;
                }
            }
        }
#endif

        // дерево
        // в ванили отдельные верки не будут убобрять после загрузки сейва
        // так как до них не доходит событие OnUpdateRoom
        // потому что ветки забанены в RoomProberе
        // чиним это
        // todo: сделать опцией
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
                __instance.Subscribe((int)GameHashes.UpdateRoom, OnUpdateRoomDelegate);
            }
        }

        // дополнительные семена безурожайных растений
        // todo: растение ловушка тоже не даёт семян. обдумать это.
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
                var extra = __instance.GetComponent<ExtraSeedProducer>();
                if (extra != null && !extra.ExtraSeedAvailable)
                {
                    // шанс получить семя за счет навыка фермера
                    float seedChance = worker.GetComponent<AttributeConverters>().Get(ExtraSeedTendingChance).Evaluate();
                    // множитель длительности эффекта.
                    float effectMultiplier =
                        worker.GetAttributes().Get(Db.Get().Attributes.Get(__instance.effectAttributeId)).GetTotalValue() * __instance.effectMultiplier +
                        1f;
                    // чем выше навык, тем дольше эффект, тем реже убобряют, поэтому перемножаем чтобы выровнять шансы

                    Debug.Log($"Tinkerable ExtraSeed effectMultiplier={effectMultiplier}, seedChance={seedChance}");

                    extra.CreateExtraSeed(seedChance * effectMultiplier);
                }
            }
        }

        // предотвращаем убобрение фермерами 
        // если растение засохло или полностью выросло
        // или декоротивное доп семя заспавнилось
        // todo: проверить спящие растения типа газотравы
        [HarmonyPatch(typeof(Tinkerable), "OnPrefabInit")]
        private static class Tinkerable_OnPrefabInit
        {
            private static void Postfix(Tinkerable __instance, EventSystem.IntraObjectHandler<Tinkerable> ___OnEffectRemovedDelegate)
            {
                if (__instance.tinkerMaterialTag == FarmStationConfig.TINKER_TOOLS)
                {
                    // чтобы обновить чору после того как белка извлекла семя
                    __instance.Subscribe((int)GameHashes.SeedProduced, ___OnEffectRemovedDelegate);
                    // чтобы обновить чору когда растение засыхает/растёт/выросло
                    if (BetterPlantTendingOptions.Instance.PreventTendingGrownOrWilting)
                    {
                        __instance.Subscribe((int)GameHashes.Wilt, ___OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.WiltRecover, ___OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.Grow, ___OnEffectRemovedDelegate);
                        //__instance.Subscribe((int)GameHashes.Harvest, ___OnEffectRemovedDelegate);
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
                    if (BetterPlantTendingOptions.Instance.PreventTendingGrownOrWilting)
                    {
                        if (__instance.HasTag(GameTags.Wilting)) // засохло
                        {
                            __result = true;
                            return;
                        }
                        if (__instance.GetComponent<Growing>()?.IsGrown() ?? false) // полностью выросло
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

        // научиваем белочек делать экстракцию декоративных безурожайных семян
        [HarmonyPatch(typeof(ClimbableTreeMonitor.Instance), "FindClimbableTree")]
        private static class ClimbableTreeMonitor_Instance_FindClimbableTree
        {
            private static void AddPlant(List<KMonoBehaviour> list, KMonoBehaviour plant)
            {
                if (plant?.GetComponent<ExtraSeedProducer>()?.ExtraSeedAvailable ?? false)
                {
                    list.Add(plant);
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
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var getComponent = typeof(Component).GetMethod(nameof(Component.GetComponent), new Type[0]).MakeGenericMethod(typeof(StorageLocker));
                var addPlant = typeof(ClimbableTreeMonitor_Instance_FindClimbableTree).GetMethodSafe(nameof(AddPlant), true, PPatchTools.AnyArguments);

                bool result = false;
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    var instruction = instructionsList[i];
                    if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info) && getComponent == info)
                    {
                        var instruction_ldlocs = instructionsList[i - 1];
                        ++i;
                        instructionsList.Insert(++i, new CodeInstruction(OpCodes.Ldloc_1));
                        instructionsList.Insert(++i, new CodeInstruction(instruction_ldlocs));
                        instructionsList.Insert(++i, new CodeInstruction(OpCodes.Call, addPlant));
                        result = true;
#if DEBUG
                        PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                    }
                }
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }
        }

        [HarmonyPatch(typeof(TreeClimbStates), "Rummage")]
        private static class TreeClimbStates_Rummage
        {
            private static void Postfix(TreeClimbStates.Instance smi)
            {
                smi.sm.target.Get(smi)?.GetComponent<ExtraSeedProducer>()?.ExtractExtraSeed();
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
                return (Component)go.GetComponent<Growing>() ?? go.GetComponent<ExtraSeedProducer>();
            }
            /*
        --- Growing growing = go.GetComponent<Growing>();
        +++ Growing growing = go.GetComponent<Growing>() ?? go.GetComponent<ExtraSeedProducer>();
	        bool flag = growing == null;
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var getComponent = typeof(GameObject).GetMethod(nameof(GameObject.GetComponent), new Type[0]).MakeGenericMethod(typeof(Growing));
                var getTwoComponents = typeof(GameUtil_GetPlantEffectDescriptors).GetMethodSafe(nameof(GetTwoComponents), true, PPatchTools.AnyArguments);

                bool result = false;
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    var instruction = instructionsList[i];
                    if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info) && getComponent == info)
                    {
                        instructionsList[i] = new CodeInstruction(OpCodes.Call, getTwoComponents);
                        result = true;
#if DEBUG
                        PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                    }
                }
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }
        }

        // todo: починить баг с прокачкой механики вместо фермерства


        // научиваем жучинкусов убобрять безурожайные растения, такие как холодых и оксихрен, и декоративочка
        // использован достаточно грязный хак. но все работает.
        [HarmonyPatch(typeof(CropTendingStates), "FindCrop")]
        private static class CropTendingStates_FindCrop
        {
            private static readonly int radius = (int)Math.Sqrt((int)(typeof(CropTendingStates).GetFieldSafe("MAX_SQR_EUCLIDEAN_DISTANCE", true)?.GetRawConstantValue() ?? 625));

            // поиск растений для убобрения.
            // Клеи зачем-то перебирают список всех урожайных растений на карте
            // а мы заменим на GameScenePartitioner
            private static List<KMonoBehaviour> FindPlants(int myWorldId, CropTendingStates.Instance smi)
            {
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
                if (BetterPlantTendingOptions.Instance.PreventTendingGrownOrWilting && !growing.IsGrowing())
                    return true;
                return growing.IsGrown();
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var IsGrown = typeof(Growing).GetMethodSafe("IsGrown", false, PPatchTools.AnyArguments);
                var GetWorldItems = typeof(Components.Cmps<Crop>).GetMethodSafe("GetWorldItems", false, PPatchTools.AnyArguments);
                var findPlants = typeof(CropTendingStates_FindCrop).GetMethodSafe(nameof(FindPlants), true, PPatchTools.AnyArguments);
                var isNotNeedTending = typeof(CropTendingStates_FindCrop).GetMethodSafe(nameof(IsNotNeedTending), true, PPatchTools.AnyArguments);

                bool r1 = false, r2 = false;
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    var instruction = instructionsList[i];
                    /*
                    foreach (Crop worldItem in 
                ---         Components.Crops.GetWorldItems(smi.gameObject.GetMyWorldId()))
                +++         FindPlants(smi.gameObject.GetMyWorldId(), smi))
                    */
                    if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info) && info == GetWorldItems)
                    {
                        instructionsList[i] = new CodeInstruction(OpCodes.Ldarg_1);
                        instructionsList.Insert(++i, new CodeInstruction(OpCodes.Call, findPlants));
                        r1 = true;
#if DEBUG
                        PUtil.LogDebug($"'{methodName}' Transpiler #1 injected");
#endif
                    }
                    /*        
                    Growing growing = worldItem.GetComponent<Growing>();
	            ---	if (!(growing != null && growing.IsGrown()) && блаблабла ...)
                +++ if (!(growing != null && isNotNeedTending(growing)) && блаблабла ...)
                    */
                    if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info2) && info2 == IsGrown)
                    {
                        instructionsList[i] = new CodeInstruction(OpCodes.Call, isNotNeedTending);
                        r2 = true;
#if DEBUG
                        PUtil.LogDebug($"'{methodName}' Transpiler #2 injected");
#endif
                        break;
                    }
                }
                if (!r1 || !r2)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }
        }

        // растение-ловушка: производство газа  пропорционально её скорости роста
        // todo: сделать опционально
        [HarmonyPatch(typeof(CritterTrapPlant.StatesInstance), nameof(CritterTrapPlant.StatesInstance.AddGas))]
        private static class CritterTrapPlant_StatesInstance_AddGas
        {
            private static readonly IDetouredField<CritterTrapPlant, ReceptacleMonitor> RM = PDetours.DetourField<CritterTrapPlant, ReceptacleMonitor>("rm");

            private static void Prefix(CritterTrapPlant.StatesInstance __instance, ref float dt)
            {
                var id = Db.Get().Amounts.Maturity.deltaAttribute.Id;
                var ai = __instance.master.GetAttributes().Get(id);
                dt *= ai.GetTotalValue() / (RM.Get(__instance.master).Replanted ? CROPS.GROWTH_RATE : CROPS.WILD_GROWTH_RATE);
            }
        }
    }
}
