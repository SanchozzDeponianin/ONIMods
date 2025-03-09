using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Klei.AI;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;

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
                && ControlYourRobotsOptions.Instance.flydo_can_pass_door;

            private static void Postfix(GameNavGrids __instance, Pathfinding pathfinding)
            {
                var flyer = __instance.FlyerGrid1x1;
                /*
                Debug.Log("transition\t[void solid valid invalid] critter");
                var transitions = new NavGrid.Transition[flyer.transitions.Length];
                for (int i = 0; i < flyer.transitions.Length; i++)
                {
                    var transition = flyer.transitions[i];
                    Debug.LogFormat("{0}\t[{1} {2} {3} {4}] {5}", transition.ToString(), transition.voidOffsets.Length, transition.solidOffsets.Length, transition.validNavOffsets.Length, transition.invalidNavOffsets.Length, transition.isCritter);
                    transition.isCritter = false;
                    transitions[i] = transition;
                }*/
                var flydo = new NavGrid(FlydoGrid, flyer.transitions, flyer.navTypeData, new CellOffset[] { CellOffset.none },
                    new NavTableValidator[] {
                        new GameNavGrids.FlyingValidator(false, false, true),
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

            private static bool ShouldSleep(RobotElectroBankMonitor.Instance smi, bool hasElectrobank)
            {
                return !hasElectrobank || smi.HasTag(RobotSuspend);
            }

            private static bool ShouldWakeUp(RobotElectroBankMonitor.Instance smi, bool hasElectrobank) => !ShouldSleep(smi, hasElectrobank);

            /*
            --- .powered.ParamTransition(hasElectrobank, powerdown.pre, IsFalse);
            +++ .powered.ParamTransition(hasElectrobank, powerdown.pre, ShouldSleep);
            
            --- .powerdown.dead.ParamTransition(hasElectrobank, powerup.grounded, IsTrue);
            +++ .powerdown.dead.ParamTransition(hasElectrobank, powerup.grounded, ShouldWakeUp);
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Transpile(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                // todo: довольно грубо. благо что IsFalse и IsTrue ровно по разу там где нам надо
                // в идеале нужно больше проверок
                var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
                var is_false = typeof(RobotElectroBankMonitor).GetField("IsFalse", flags);
                var is_true = typeof(RobotElectroBankMonitor).GetField("IsTrue", flags);
                var callback = typeof(RobotElectroBankMonitor.Parameter<bool>.Callback).GetConstructors()[0];
                var sleep = typeof(RobotElectroBankMonitor_InitializeStates).GetMethodSafe(nameof(ShouldSleep), true, PPatchTools.AnyArguments);
                var wakeup = typeof(RobotElectroBankMonitor_InitializeStates).GetMethodSafe(nameof(ShouldWakeUp), true, PPatchTools.AnyArguments);

                if (is_false != null && is_true != null && callback != null && sleep != null && wakeup != null)
                {
                    int i = instructions.FindIndex(inst => inst.opcode == OpCodes.Ldsfld
                        && inst.operand is FieldInfo info && info.FieldHandle == is_false.FieldHandle);
                    if (i == -1)
                        return false;
                    instructions.RemoveAt(i);
                    instructions.Insert(i++, new CodeInstruction(OpCodes.Ldnull));
                    instructions.Insert(i++, new CodeInstruction(OpCodes.Ldftn, sleep));
                    instructions.Insert(i++, new CodeInstruction(OpCodes.Newobj, callback));

                    i = instructions.FindIndex(inst => inst.opcode == OpCodes.Ldsfld
                        && inst.operand is FieldInfo info && info.FieldHandle == is_true.FieldHandle);
                    if (i == -1)
                        return false;
                    instructions.RemoveAt(i);
                    instructions.Insert(i++, new CodeInstruction(OpCodes.Ldnull));
                    instructions.Insert(i++, new CodeInstruction(OpCodes.Ldftn, wakeup));
                    instructions.Insert(i++, new CodeInstruction(OpCodes.Newobj, callback));
                    return true;
                }
                return false;
            }

            private static bool OnTagsGotoSleep(RobotElectroBankMonitor.Instance smi, object data)
            {
                return data is TagChangedEventData @event && @event.tag == RobotSuspend && @event.added;
            }

            private static bool OnTagsGotoWakeUp(RobotElectroBankMonitor.Instance smi, object data)
            {
                return data is TagChangedEventData @event && @event.tag == RobotSuspend && !@event.added
                    && smi.sm.hasElectrobank.Get(smi);
            }

            private static void Postfix(RobotElectroBankMonitor __instance)
            {
                __instance.powered.EventHandlerTransition(GameHashes.TagsChanged, __instance.powerdown.pre, OnTagsGotoSleep);
                __instance.powerdown.dead.EventHandlerTransition(GameHashes.TagsChanged, __instance.powerup.grounded, OnTagsGotoWakeUp);
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
