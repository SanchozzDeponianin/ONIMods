using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Klei.AI;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace ControlYourRobots
{
    using static STRINGS.ROBOTS.STATUSITEMS.SLEEP_MODE;

    internal sealed class ControlYourRobotsPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Utils.LogModVersion();
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(ControlYourRobotsPatches));
            new POptions().RegisterOptions(this, typeof(ControlYourRobotsOptions));
            ControlYourRobotsOptions.Reload();
        }

        public static Tag RobotSuspend = TagManager.Create(nameof(RobotSuspend));
        private static Dictionary<Tag, AttributeModifier> SuspendedBatteryModifiers = new Dictionary<Tag, AttributeModifier>();
        private static Dictionary<Tag, AttributeModifier> IdleBatteryModifiers = new Dictionary<Tag, AttributeModifier>();

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [HarmonyPatch(typeof(BaseRoverConfig), nameof(BaseRoverConfig.BaseRover))]
        private static class BaseRoverConfig_BaseRover
        {
            private static void Postfix(GameObject __result, string id, float batteryDepletionRate, Amount batteryType)
            {
                __result.AddOrGet<RobotTurnOffOn>();
                __result.AddOrGet<Movable>(); // переносить спящего
                SuspendedBatteryModifiers[id] = new AttributeModifier(batteryType.deltaAttribute.Id, batteryDepletionRate, NAME);
                if (ControlYourRobotsOptions.Instance.low_power_mode_enable)
                {
                    float rate = batteryDepletionRate * (1f - ControlYourRobotsOptions.Instance.low_power_mode_value / 100f);
                    IdleBatteryModifiers[id] = new AttributeModifier(batteryType.deltaAttribute.Id, rate,
                        global::STRINGS.CREATURES.STATUSITEMS.IDLE.NAME);
                }
            }
        }

        // иди туды и вкл выкл
        [HarmonyPatch(typeof(RobotAi), nameof(RobotAi.InitializeStates))]
        private static class RobotAi_InitializeStates
        {
            public class SuspendedStates : RobotAi.State
            {
#pragma warning disable CS0649
                public RobotAi.State satisfied;
                public RobotAi.State moved;
#pragma warning restore CS0649
            }

            private static void Postfix(RobotAi __instance)
            {
                const string name = "suspended";
                var suspended = (SuspendedStates)__instance.CreateState(name, __instance.alive, new SuspendedStates());

                __instance.alive.normal
                    .ToggleStateMachine(smi => new MoveToLocationMonitor.Instance(smi.master)) // иди туды
                    .TagTransition(RobotSuspend, suspended, false);

                suspended
                    .TagTransition(RobotSuspend, __instance.alive.normal, true)
                    .DefaultState(suspended.satisfied)
                    .ToggleStatusItem(NAME, TOOLTIP)
                    .ToggleAttributeModifier("save battery",
                        smi => SuspendedBatteryModifiers[smi.PrefabID()],
                        smi => SuspendedBatteryModifiers.ContainsKey(smi.PrefabID()))
                    .ToggleBrain(name)
                    .Enter(smi =>
                    {
                        smi.GetComponent<Navigator>().Pause(name);
                        smi.GetComponent<Storage>().DropAll();
                        smi.RefreshUserMenu();
                    })
                    .ScheduleActionNextFrame("Clean StatusItem", CleanStatusItem)
                    .Exit(smi =>
                    {
                        // отменить перемещение при просыпании
                        var movable = smi.GetComponent<Movable>();
                        if (movable != null && movable.StorageProxy != null)
                            movable.StorageProxy.GetComponent<CancellableMove>().OnCancel(movable);
                        smi.GetComponent<Navigator>().Unpause(name);
                        smi.RefreshUserMenu();
                    });

                suspended.satisfied
                    .PlayAnim("in_storage")
                    .TagTransition(GameTags.Stored, suspended.moved, false)
                    .ToggleStateMachine(smi => new RobotSleepFX.Instance(smi.master))
                    .ToggleStateMachine(smi => new FallWhenDeadMonitor.Instance(smi.master))
                    .Enter(smi =>
                    {
                        // принудительно "роняем" робота чтобы он не зависал в воздухе после перемещения
                        var fall_smi = smi.GetSMI<FallWhenDeadMonitor.Instance>();
                        if (!fall_smi.IsNullOrStopped())
                            fall_smi.GoTo(fall_smi.sm.falling);
                    });

                suspended.moved
                    .PlayAnim("in_storage")
                    .TagTransition(GameTags.Stored, suspended.satisfied, true);
            }

            // очищаем лишний StatusItem который может появиться при прерывании выполнения FetchAreaChore
            private static void CleanStatusItem(StateMachine.Instance smi)
            {
                if (!smi.IsNullOrStopped() && smi.gameObject.TryGetComponent<KSelectable>(out var selectable))
                    selectable.SetStatusItem(Db.Get().StatusItemCategories.Main, null, null);
            }
        }

        // скрываем кнопку MoveTo для перемещения объектов если это робот и он не выключен
        [HarmonyPatch(typeof(Movable), "OnRefreshUserMenu")]
        private static class Movable_OnRefreshUserMenu
        {
            private static bool Prefix(Movable __instance)
            {
                return !(__instance.HasTag(GameTags.Robot) && !__instance.HasTag(RobotSuspend));
            }
        }

        // если команда MoveTo применена к выключенному роботу
        // патчим чору, чтобы его переносили как кусок ресурса, а не как жеготное, так как у роботов нет некоторых компонентов
        [HarmonyPatch(typeof(MovePickupableChore), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(IStateMachineTarget), typeof(GameObject), typeof(Action<Chore>) })]
        private static class MovePickupableChore_Constructor
        {
            /*
        --- if (pickupable.GetComponent<CreatureBrain>())
        +++ if (pickupable.GetComponent<CreatureBrain>() && !pickupable.HasTag(GameTags.Robot))
            {
                AddPrecondition(blabla);
                AddPrecondition(blabla);
            }
            else
            {
                AddPrecondition(blabla);
            }
            */
            private static bool IsNotRobot(bool condition, GameObject go)
            {
                return condition && go != null && !go.HasTag(GameTags.Robot);
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions, MethodBase original)
            {
                var pickupable = original?.GetParameters().FirstOrDefault(p =>
                    p.ParameterType == typeof(GameObject) && p.Name == "pickupable");
                var gc = typeof(GameObject).GetMethodSafe(nameof(GameObject.GetComponent), false)
                    ?.MakeGenericMethod(typeof(CreatureBrain));
                var opi = typeof(UnityEngine.Object).GetMethodSafe("op_Implicit", true, typeof(UnityEngine.Object));
                var inject = typeof(MovePickupableChore_Constructor)
                    .GetMethodSafe(nameof(IsNotRobot), true, typeof(bool), typeof(GameObject));
                if (pickupable != null && gc != null && opi != null && inject != null)
                {
                    int i = instructions.FindIndex(ins => ins.Calls(gc));
                    if (i != -1)
                    {
                        i++;
                        if (instructions[i].Calls(opi))
                        {
                            instructions.Insert(++i, TranspilerUtils.GetLoadArgInstruction(pickupable));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, inject));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // низкая мощность при безделии
        [HarmonyPatch(typeof(IdleStates), nameof(IdleStates.InitializeStates))]
        private static class IdleStates_InitializeStates
        {
            private static bool Prepare() => ControlYourRobotsOptions.Instance.low_power_mode_enable;

            private static void Postfix(GameStateMachine<IdleStates, IdleStates.Instance, IStateMachineTarget, IdleStates.Def>.State ___loop)
            {
                ___loop.ToggleAttributeModifier("low power mode",
                        smi => IdleBatteryModifiers[smi.PrefabID()],
                        smi => IdleBatteryModifiers.ContainsKey(smi.PrefabID()));
            }
        }

        // поиск пути с учётом разрешения дверей
        [HarmonyPatch(typeof(CreatureBrain), "OnPrefabInit")]
        private static class CreatureBrain_OnPrefabInit
        {
            private static Tag[] Robot_AI_Tags = { GameTags.Robot, GameTags.DupeBrain };
            private static void Postfix(CreatureBrain __instance)
            {
                if (__instance.HasAllTags(Robot_AI_Tags) && __instance.TryGetComponent<Navigator>(out var navigator))
                    navigator.SetAbilities(new RobotPathFinderAbilities(navigator));
            }
        }

        // внедряем роботов в экран доступов двери
        [HarmonyPatch(typeof(AccessControlSideScreen), nameof(AccessControlSideScreen.SetTarget))]
        private static class AccessControlSideScreen_SetTarget
        {
            private static List<MinionAssignablesProxy> Inject(List<MinionAssignablesProxy> list)
            {
                list.AddRange(RobotAssignablesProxy.Cmps.Items);
                return list;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var list = typeof(AccessControlSideScreen).GetFieldSafe("identityList", false);
                var inject = typeof(AccessControlSideScreen_SetTarget).GetMethodSafe(nameof(Inject), true, PPatchTools.AnyArguments);
                if (list != null && inject != null)
                {
                    int i = instructions.FindIndex(ins => ins.StoresField(list));
                    if (i != -1)
                    {
                        instructions.Insert(i, new CodeInstruction(OpCodes.Call, inject));
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
