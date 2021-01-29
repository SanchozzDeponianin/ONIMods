#define DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Klei.AI;
using TUNING;
using UnityEngine;

using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;

using static BetterPlantTending.BetterPlantTendingAssets;

namespace BetterPlantTending
{
    internal static class BetterPlantTendingPatches
    {

        public static void OnLoad()
        {
            PUtil.InitLibrary();
            PUtil.RegisterPatchClass(typeof(BetterPlantTendingPatches));
            POptions.RegisterOptions(typeof(BetterPlantTendingOptions));
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
        internal static class OxyfernConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                __result.AddOrGet<TendedOxyfern>();
            }
        }

        [HarmonyPatch(typeof(Oxyfern), nameof(Oxyfern.SetConsumptionRate))]
        internal static class Oxyfern_SetConsumptionRate
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
        internal static class ColdBreatherConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                __result.AddOrGet<TendedColdBreather>();
            }
        }

        [HarmonyPatch(typeof(ColdBreather), "OnReplanted")]
        internal static class ColdBreather_OnReplanted
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
        internal static class ElementConsumer_UpdateStatusItem
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
        [HarmonyPatch(typeof(ForestTreeConfig), nameof(ForestTreeConfig.CreatePrefab))]
        internal static class ForestTreeConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<TendedForestTree>();
            }
        }

        // в ванилле ветка имеет "тинкерабле" и  "эффекты"
        // но её практически никогда не убобряют
        // с модом тоже
        // однако крайне редко удается случайно отловить когда убобряют
        // как то связано с засыханием и ростом новых веток
        // поэтому - грязный хак чтобы не могли убобрить
        [HarmonyPatch(typeof(ForestTreeBranchConfig), "CreatePrefab")]
        internal static class ForestTreeBranchConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                __result.GetComponent<Tinkerable>().tinkerMaterialTag = GameTags.Void;
            }
        }

        // применить эффект при росте новой ветки дерева
        [HarmonyPatch(typeof(TreeBud), "OnSpawn")]
        internal static class TreeBud_OnSpawn
        {
            private static void Prefix(TreeBud __instance, Ref<BuddingTrunk> ___buddingTrunk)
            {
                TendedForestTree.ApplyModifierToBranch(__instance, ___buddingTrunk.Get());
            }
        }

        // дополнительные семена безурожайных растений
        [HarmonyPatch(typeof(EntityTemplates), nameof(EntityTemplates.CreateAndRegisterSeedForPlant))]
        internal static class EntityTemplates_CreateAndRegisterSeedForPlant
        {
            private static void Postfix(GameObject plant)
            {
                if (plant.GetComponent<Crop>() == null)
                {
                    plant.AddOrGet<ExtraSeedProducer>();
                    Tinkerable.MakeFarmTinkerable(plant);
                }
            }
        }

        // заспавним доп семя при убобрении фермерами
        [HarmonyPatch(typeof(Tinkerable), "OnCompleteWork")]
        internal static class Tinkerable_OnCompleteWork
        {
            private static void Postfix(Tinkerable __instance, Worker worker)
            {
                var extra = __instance.GetComponent<ExtraSeedProducer>();
                if (extra != null)
                {
                    extra.CreateExtraSeed(worker);
                }
            }
        }

        // предотвращаем повторное убобрение фермерами если доп семя заспавнилось
        [HarmonyPatch(typeof(Tinkerable), "HasEffect")]
        internal static class Tinkerable_HasEffect
        {
            private static void Postfix(Tinkerable __instance, ref bool __result)
            {
                var extra = __instance.GetComponent<ExtraSeedProducer>();
                if (extra != null)
                {
                    __result = __result || !extra.ShouldFarmTinkerTending;
                }
                // todo: может быть. прикрутить запрет убобрять засохшие или полностью выросшие.
            }
        }

        // todo: починить баг с прокачкой механики вместо фермерства

        // научиваем белочек делать экстракцию декоративных безурожайных семян
        [HarmonyPatch(typeof(ClimbableTreeMonitor.Instance), "FindClimbableTree")]
        internal static class ClimbableTreeMonitor_Instance_FindClimbableTree
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
#if DEBUG
                Debug.Log("---------------------------------- ORIGINAL ----------------------------------");
                PPatchTools.DumpMethodBody(instructionsList);
#endif
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
#if DEBUG
                Debug.Log("---------------------------------- PATCHED  ----------------------------------");
                PPatchTools.DumpMethodBody(instructionsList);
#endif               
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }
        }

        [HarmonyPatch(typeof(TreeClimbStates), "Rummage")]
        internal static class TreeClimbStates_Rummage
        {
            private static void Postfix(TreeClimbStates.Instance smi)
            {
                smi.sm.target.Get(smi)?.GetComponent<ExtraSeedProducer>()?.ExtractExtraSeed();
            }
        }

#if EXPANSION1

        // научиваем жучинкусов убобрять безурожайные растения, такие как холодых и оксихрен, и декоративочка
        // а также корректируем убобрение дерева
        // использован достаточно грязный хак. но все работает.
        [HarmonyPatch(typeof(CropTendingStates), "FindCrop")]
        internal static class CropTendingStates_FindCrop
        {
            private static bool IsNotNeedTending(Growing growing)
            {
                if (growing == null)
                    return true;
                if (growing.HasTag(ForestTreeBranchConfig.ID)) // не нужно убобрять отдельные ветки
                    return true;
                if (growing.HasTag(ForestTreeConfig.ID)) // дерево
                {
                    if (growing.IsGrown())
                    {
                        // не нужно убобрять дерево если все ветки выросли
                        var buddingTrunk = growing.GetComponent<BuddingTrunk>();
                        if (buddingTrunk != null)
                        {
                            for (int i = 0; i < ForestTreeConfig.NUM_BRANCHES; i++)
                            {
                                var growingBranch = buddingTrunk.GetBranchAtPosition(i)?.GetComponent<Growing>();
                                if (growingBranch != null && !growingBranch.IsGrown())
                                {
                                    return false;
                                }
                            }
                        }
                        return true;
                    }
                    else
                        return false;
                }
                // остальные растения не нужно убобрять если выросли
                return growing.IsGrown();
            }

            // поиск растений для убобрения.
            // Клеи зачем-то перебирают список всех урожайных растений на карте
            // а мы заменим на GameScenePartitioner
            private static List<KMonoBehaviour> FindPlants(int myWorldId, CropTendingStates.Instance smi)
            {
                var entries = ListPool<ScenePartitionerEntry, GameScenePartitioner>.Allocate();
                // todo: вычислять радиус из константы
                var search_extents = new Extents(Grid.PosToCell(smi.master.transform.GetPosition()), 25);
                GameScenePartitioner.Instance.GatherEntries(search_extents, GameScenePartitioner.Instance.plants, entries);
                var plants = entries
                    .Select((e) => e.obj as KMonoBehaviour)
                    .Where((kmb) => (kmb.GetMyWorldId() == myWorldId)
                        && ((kmb.GetComponent<Crop>() != null) || ( kmb.GetComponent<ExtraSeedProducer>()?.ShouldDivergentTending ?? false)))
                    .ToList();
                entries.Recycle();
                return plants;
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
#if DEBUG
                Debug.Log("---------------------------------- ORIGINAL ----------------------------------");
                PPatchTools.DumpMethodBody(instructionsList);
#endif
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
#if DEBUG
                Debug.Log("---------------------------------- PATCHED  ----------------------------------");
                PPatchTools.DumpMethodBody(instructionsList);
#endif               
                if (!r1 || !r2)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }

            // todo: для отладки, потом убрать
            private static void Postfix(CropTendingStates.Instance smi)
            {
                var x = smi.sm.targetCrop.Get(smi);
                Debug.Log("selected Tending plant: " + x.GetProperName());
                if (x is null)
                    return;
                var t = x.GetComponent<KPrefabID>().Tags;
                string s = "";
                foreach (var tt in t)
                {
                    s = s + " " + tt.Name;
                }
                Debug.Log("selected plant Tags: " + s);
            }
        }

        // растение-ловушка: производство газа  пропорционально её скорости роста
        // todo: сделать опционально
        [HarmonyPatch(typeof(CritterTrapPlant.StatesInstance), nameof(CritterTrapPlant.StatesInstance.AddGas))]
        internal static class CritterTrapPlant_StatesInstance_AddGas
        {
            private static readonly IDetouredField<CritterTrapPlant, ReceptacleMonitor> RM = PDetours.DetourField<CritterTrapPlant, ReceptacleMonitor>("rm");

            private static void Prefix(CritterTrapPlant.StatesInstance __instance, ref float dt)
            {
                var id = Db.Get().Amounts.Maturity.deltaAttribute.Id;
                var ai = __instance.master.GetAttributes().Get(id);
                dt *= ai.GetTotalValue() / (RM.Get(__instance.master).Replanted ? CROPS.GROWTH_RATE : CROPS.WILD_GROWTH_RATE);
            }
        }
#endif
    }
}
