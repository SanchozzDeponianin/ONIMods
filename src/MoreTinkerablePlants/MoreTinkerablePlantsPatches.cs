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

namespace MoreTinkerablePlants
{
    internal static class MoreTinkerablePlantsPatches
    {
        internal const float THROUGHPUT_BASE_VALUE = 1;
        internal const float THROUGHPUT_MULTIPLIER = 3;

        internal static Attribute ColdBreatherThroughput;
        internal static Attribute OxyfernThroughput;

        private static AttributeModifier ColdBreatherThroughputModifier;
        private static AttributeModifier OxyfernThroughputModifier;

        public static void OnLoad()
        {
            PUtil.InitLibrary();
            PUtil.RegisterPatchClass(typeof(MoreTinkerablePlantsPatches));
            POptions.RegisterOptions(typeof(MoreTinkerablePlantsOptions));
        }

        [PLibMethod(RunAt.AfterModsLoad)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        // добавляем атрибуты и модификаторы 
        // для более лючшего отображения в интерфейсе
        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            var db = Db.Get();
            var effectFarmTinker = db.effects.Get(TinkerableEffectMonitor.FARM_TINKER_EFFECT_ID);

            ColdBreatherThroughput = new Attribute(nameof(ColdBreatherThroughput), false, Attribute.Display.General, false, THROUGHPUT_BASE_VALUE);
            ColdBreatherThroughput.SetFormatter(new PercentAttributeFormatter());
            db.Attributes.Add(ColdBreatherThroughput);

            ColdBreatherThroughputModifier = new AttributeModifier(ColdBreatherThroughput.Id, THROUGHPUT_MULTIPLIER - THROUGHPUT_BASE_VALUE);
            effectFarmTinker.Add(ColdBreatherThroughputModifier);

            OxyfernThroughput = new Attribute(nameof(OxyfernThroughput), false, Attribute.Display.General, false, THROUGHPUT_BASE_VALUE);
            OxyfernThroughput.SetFormatter(new PercentAttributeFormatter());
            db.Attributes.Add(OxyfernThroughput);

            OxyfernThroughputModifier = new AttributeModifier(OxyfernThroughput.Id, THROUGHPUT_MULTIPLIER - THROUGHPUT_BASE_VALUE);
            effectFarmTinker.Add(OxyfernThroughputModifier);

            // todo: добавить модификаторы для жучинкусов
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            MoreTinkerablePlantsOptions.Reload();
            ColdBreatherThroughputModifier.SetValue(MoreTinkerablePlantsOptions.Instance.ColdBreatherThroughputMultiplier - THROUGHPUT_BASE_VALUE);
            OxyfernThroughputModifier.SetValue(MoreTinkerablePlantsOptions.Instance.OxyfernThroughputMultiplier - THROUGHPUT_BASE_VALUE);
        }

        // Оксихрен
        [HarmonyPatch(typeof(OxyfernConfig), nameof(OxyfernConfig.CreatePrefab))]
        internal static class OxyfernConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                __result.AddOrGet<TinkerableOxyfern>();
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
                __result.AddOrGet<TinkerableColdBreather>();
            }
        }

        [HarmonyPatch(typeof(ColdBreather), "OnReplanted")]
        internal static class ColdBreather_OnReplanted
        {
            private static void Postfix(ColdBreather __instance)
            {
                __instance.GetComponent<TinkerableColdBreather>()?.ApplyModifier();
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
                __result.AddOrGet<TinkerableForestTree>();
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
                TinkerableForestTree.ApplyModifierToBranch(__instance, ___buddingTrunk.Get());
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
                        // todo: не нужно убобрять дерево если все ветки выросли
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
                // todo: сдесь должны быть дополнительные проверки. в частности - опция убобрения декоративки
                var plants = entries
                    .Select((e) => e.obj as KMonoBehaviour)
                    .Where((kmb) => kmb.GetMyWorldId() == myWorldId)
                    .ToList();
                entries.Recycle();
                return plants;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var IsGrown = PPatchTools.GetMethodSafe(typeof(Growing), "IsGrown", false, PPatchTools.AnyArguments);
                var GetWorldItems = PPatchTools.GetMethodSafe(typeof(Components.Cmps<Crop>), "GetWorldItems", false, PPatchTools.AnyArguments);
                var findPlants = PPatchTools.GetMethodSafe(typeof(CropTendingStates_FindCrop), nameof(FindPlants), true, PPatchTools.AnyArguments);
                var isNotNeedTending = PPatchTools.GetMethodSafe(typeof(CropTendingStates_FindCrop), nameof(IsNotNeedTending), true, PPatchTools.AnyArguments);

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
                    if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (MethodInfo)instruction.operand == GetWorldItems)
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
                    if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (MethodInfo)instruction.operand == IsGrown)
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
                Debug.Log("selected plant: " + x.GetProperName());
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

        // todo: сделать спавн доп семян при убобрении декоративных растений

        // todo: научить белочек делать экстракцию декоративных семян

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
