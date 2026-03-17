using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Klei.AI;
using STRINGS;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;

namespace ControlYourRobots
{
    using static Patches;
    using static STRINGS.ROBOTS.STATUSITEMS.SLEEP_MODE;

    internal static class FlydoPatches
    {
        [HarmonyPatch(typeof(FetchDroneConfig), nameof(FetchDroneConfig.CreatePrefab))]
        private static class FetchDroneConfig_CreatePrefab
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID);
            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<RobotTurnOffOn>();
                if (ModOptions.Instance.low_power_mode_flydo_landed)
                    __result.AddOrGetDef<RobotLandedIdleMonitor.Def>().timeout = ModOptions.Instance.low_power_mode_flydo_timeout;
                // для правильного отображения величины заряда при сне
                var delta_id = Db.Get().Amounts.InternalElectroBank.deltaAttribute.Id;
                var trait = Db.Get().traits.TryGet("FetchDroneBaseTrait");
                if (trait != null)
                {
                    foreach (var modifier in trait.SelfModifiers)
                    {
                        if (modifier.AttributeId == delta_id)
                        {
                            SuspendedBatteryModifiers[__result.PrefabID()] = new AttributeModifier(delta_id, -modifier.Value, NAME);
                            float rate = -modifier.Value * (1f - ModOptions.Instance.low_power_mode_value / 100f);
                            LandedIdleBatteryModifiers[__result.PrefabID()] = new AttributeModifier(delta_id, rate, CREATURES.STATUSITEMS.IDLE.NAME);
                            break;
                        }
                    }
                }
            }
        }

        // вкл выкл
        [HarmonyPatch(typeof(RobotElectroBankMonitor), nameof(RobotElectroBankMonitor.InitializeStates))]
        private static class RobotElectroBankMonitor_InitializeStates
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID);

            private static void Postfix(RobotElectroBankMonitor __instance)
            {
                __instance.powered.TagTransition(RobotSuspend, __instance.powerdown, false);
            }
        }

        // при включении проверим а есть ли у него батарейка
        [HarmonyPatch(typeof(RobotElectroBankDeadStates), nameof(RobotElectroBankDeadStates.InitializeStates))]
        private static class RobotElectroBankDeadStates_InitializeStates
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID);

            private delegate bool ElectrobankDelivered(RobotElectroBankDeadStates.Instance smi);

            private static DetouredMethod<ElectrobankDelivered> IsElectrobankDelivered
                = typeof(RobotElectroBankDeadStates).DetourLazy<ElectrobankDelivered>(nameof(ElectrobankDelivered));

            private static bool OnWakeUp(RobotElectroBankDeadStates.Instance smi, object data)
            {
                var @event = ((Boxed<TagChangedEventData>)data).value;
                return @event.tag == RobotSuspend && !@event.added && IsElectrobankDelivered.Invoke(smi);
            }

            private static void Postfix(RobotElectroBankDeadStates __instance)
            {
                __instance.powerdown.EventHandlerTransition(GameHashes.TagsChanged, __instance.powerup.grounded, OnWakeUp);
            }
        }

        // а это на случай если батарейку всунули в выключенного
        [HarmonyPatch(typeof(RobotElectroBankDeadStates), "ElectrobankDelivered")]
        private static class RobotElectroBankDeadStates_ElectrobankDelivered
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID);

            private static void Postfix(RobotElectroBankDeadStates.Instance smi, ref bool __result)
            {
                __result = __result && !smi.HasTag(RobotSuspend);
            }
        }

        // приземляццо и низкая мощность при безделии
        [HarmonyPatch(typeof(FetchDroneConfig), nameof(FetchDroneConfig.CreatePrefab))]
        private static class FetchDroneConfig_CreatePrefab_2
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID)
                && ModOptions.Instance.low_power_mode_flydo_landed;
            /*
                var chore_table = new ChoreTable.Builder()
                    .Add(блаблабла)
            +++     .Add(new RobotLandedIdleStates.Def())
                    .Add(new IdleStates.Def(блаблабла));
            */
            private static ChoreTable.Builder Inject(ChoreTable.Builder builder)
            {
                // приоритет чуть выше чем Idle
                return builder.Add(new RobotLandedIdleStates.Def(), true, Db.Get().ChoreTypes.ReturnSuitIdle.priority);
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var idle_states = typeof(IdleStates.Def).GetConstructors()[0];
                var inject = typeof(FetchDroneConfig_CreatePrefab_2).GetMethodSafe(nameof(Inject), true, typeof(ChoreTable.Builder));
                if (idle_states != null && inject != null)
                {
                    int i = instructions.FindIndex(inst => inst.opcode == OpCodes.Newobj && inst.operand as MethodBase == idle_states);
                    if (i != -1)
                    {
                        instructions.Insert(i, new CodeInstruction(OpCodes.Call, inject));
                        return true;
                    }
                }
                return false;
            }
        }

        // флудо может брать батарейки для себя
        [HarmonyPatch]
        private static class FetchChore_CanFetchDroneComplete
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID)
                && ModOptions.Instance.flydo_can_for_itself;

            private static IEnumerable<MethodBase> TargetMethods()
            {
                var list = new List<MethodBase>();
                var method = FetchChore.CanFetchDroneComplete.fn.Method;
                if (method != null)
                    list.Add(method);
                return list;
            }
            /*
                if (blablabla
            ---     && !(kmonoBehaviour.gameObject == context.consumerState.gameObject)
                    && blablabla)
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var gameObject = typeof(ChoreConsumerState).GetFieldSafe(nameof(ChoreConsumerState.gameObject), false);
                var op_Equality = typeof(Object).GetMethodSafe("op_Equality", true, typeof(Object), typeof(Object));
                if (gameObject != null && op_Equality != null)
                {
                    int i = instructions.FindIndex(inst => inst.LoadsField(gameObject));
                    if (i != -1 && instructions[++i].Calls(op_Equality))
                    {
                        instructions.Insert(++i, new CodeInstruction(OpCodes.Pop));
                        instructions.Insert(++i, new CodeInstruction(OpCodes.Ldc_I4_0));
                        return true;
                    }
                }
                return false;
            }
        }

        // флудо может брать бутылки из наливайки, чайника, набутыливателей и т.п.
        [HarmonyPatch]
        private static class FetchChore_IsFetchTargetAvailable
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID)
                && ModOptions.Instance.flydo_can_liquid_source;

            private static IEnumerable<MethodBase> TargetMethods()
            {
                var list = new List<MethodBase>
                {
                    FetchChore.IsFetchTargetAvailable.fn.Method,
                    FetchChore.CanFetchDroneComplete.fn.Method
                };
                list.RemoveAll(m => m == null);
                return list;
            }
            /*
                if (blablabla
            ---     && (pickupable.targetWorkable == null || pickupable.targetWorkable as Pickupable != null)
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions, MethodBase method)
            {
                var targetWorkable = typeof(Pickupable).GetFieldSafe(nameof(Pickupable.targetWorkable), false);
                if (targetWorkable != null)
                    return instructions.RemoveAll(inst => inst.LoadsField(targetWorkable)) > 0;
                return false;
            }
        }

        // прямолинейность
        [HarmonyPatch(typeof(GameNavGrids), MethodType.Constructor)]
        [HarmonyPatch(new System.Type[] { typeof(Pathfinding) })]
        private static class GameNavGrids_Constructor
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID)
                && ModOptions.Instance.flydo_prefers_straight;

            private static void Postfix(GameNavGrids __instance)
            {
                var flyer = __instance.RobotFlyerGrid1x1;
                for (int i = 0; i < flyer.transitions.Length; i++)
                {
                    var transition = flyer.transitions[i];
                    if (transition.start == NavType.Hover && transition.end == NavType.Hover && transition.y != 0)
                    {
                        if (transition.x == 0)
                            transition.cost -= 1;
                        else
                            transition.cost += 1;
                        flyer.transitions[i] = transition;
                    }
                }
            }
        }
    }
}
