using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Klei.AI;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;

namespace ControlYourRobots
{
    using static ControlYourRobotsPatches;
    using static STRINGS.ROBOTS.STATUSITEMS.SLEEP_MODE;

    internal static class FlydoPatches
    {
        // кастомная нафигация для флудо с возможностью через двери
        // на основе стандартной нафигации летунов 1х1

        public const string FlydoGrid = "FludooGrid1x1";

        [HarmonyPatch(typeof(GameNavGrids), MethodType.Constructor)]
        [HarmonyPatch(new System.Type[] { typeof(Pathfinding) })]
        private static class GameNavGrids_Constructor
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID)
                && (ControlYourRobotsOptions.Instance.flydo_can_pass_door || ControlYourRobotsOptions.Instance.flydo_prefers_straight);

            private static void Postfix(GameNavGrids __instance, Pathfinding pathfinding)
            {
                var flyer = __instance.FlyerGrid1x1;
                NavGrid.Transition[] transitions;
                if (ControlYourRobotsOptions.Instance.flydo_prefers_straight)
                {
                    transitions = new NavGrid.Transition[flyer.transitions.Length];
                    for (int i = 0; i < flyer.transitions.Length; i++)
                    {
                        var transition = flyer.transitions[i];
                        if (transition.start == NavType.Hover && transition.end == NavType.Hover && transition.y == 0)
                            transition.cost -= 1;
                        transitions[i] = transition;
                    }
                }
                else
                    transitions = flyer.transitions;
                var flydo = new NavGrid(FlydoGrid, transitions, flyer.navTypeData, new CellOffset[] { CellOffset.none },
                    new NavTableValidator[] {
                        new GameNavGrids.FlyingValidator(false, false, ControlYourRobotsOptions.Instance.flydo_can_pass_door),
                        new GameNavGrids.SwimValidator() },
                    flyer.updateRangeX, flyer.updateRangeY, flyer.maxLinksPerCell);
                pathfinding.AddNavGrid(flydo);
            }
        }

        [HarmonyPatch(typeof(FetchDroneConfig), nameof(FetchDroneConfig.CreatePrefab))]
        private static class FetchDroneConfig_CreatePrefab
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID);
            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<RobotTurnOffOn>();
                if (ControlYourRobotsOptions.Instance.flydo_can_pass_door)
                    __result.GetComponent<Navigator>().NavGridName = FlydoGrid;
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
                            break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FetchDroneConfig), nameof(FetchDroneConfig.OnSpawn))]
        private static class FetchDroneConfig_OnSpawn
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID)
                && ControlYourRobotsOptions.Instance.flydo_can_pass_door;
            private static void Postfix(GameObject inst)
            {
                if (inst.TryGetComponent(out Navigator navigator))
                    navigator.transitionDriver.overrideLayers.Add(new Door1x1TransitionLayer(navigator));
            }
        }

        // запрещаем летуну двери по умолчанию
        public static readonly int FirstFludoWasAppeared = Hash.SDBMLower(nameof(FirstFludoWasAppeared));

        private static void Restrict(this AccessControl door, object data)
        {
            if (door != null && data is RobotAssignablesProxy proxy && door.IsDefaultPermission(proxy))
                door.SetPermission(proxy, AccessControl.Permission.Neither);
        }

        // запретить на существующих дверях когда первый флудо появился
        [HarmonyPatch(typeof(AccessControl), "OnPrefabInit")]
        private static class AccessControl_OnPrefabInit
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID)
                && ControlYourRobotsOptions.Instance.flydo_can_pass_door
                && ControlYourRobotsOptions.Instance.restrict_flydo_by_default;
            private static void Postfix(AccessControl __instance)
            {
                Game.Instance.Subscribe(FirstFludoWasAppeared, __instance.Restrict);
            }
        }

        // запретить на вновь построенных дверях если флуды уже есть
        [HarmonyPatch(typeof(AccessControl), "OnSpawn")]
        private static class AccessControl_OnSpawn
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID)
                && ControlYourRobotsOptions.Instance.flydo_can_pass_door
                && ControlYourRobotsOptions.Instance.restrict_flydo_by_default;
            private static void Postfix(AccessControl __instance)
            {
                foreach (var proxy in RobotAssignablesProxy.Cmps.Items)
                {
                    if (proxy.PrefabID == FetchDroneConfig.ID)
                    {
                        __instance.Restrict(proxy);
                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AccessControl), "OnCleanUp")]
        private static class AccessControl_OnCleanUp
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID)
                && ControlYourRobotsOptions.Instance.flydo_can_pass_door
                && ControlYourRobotsOptions.Instance.restrict_flydo_by_default;
            private static void Prefix(AccessControl __instance)
            {
                Game.Instance.Unsubscribe(FirstFludoWasAppeared, __instance.Restrict);
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
                return data is TagChangedEventData @event && @event.tag == RobotSuspend && !@event.added
                    && IsElectrobankDelivered.Invoke(smi);
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

        [HarmonyPatch]
        private static class FetchChore_CanFetchDroneComplete
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID)
                && ControlYourRobotsOptions.Instance.flydo_can_for_itself;

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
                return TranspilerUtils.Transpile(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
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
    }
}
