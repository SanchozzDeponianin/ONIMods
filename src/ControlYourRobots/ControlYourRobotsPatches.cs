using System.Collections.Generic;
using Klei.AI;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
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
            private static void Postfix(RobotAi __instance)
            {
                // создаём новый стат
                var suspended = new GameStateMachine<RobotAi, RobotAi.Instance, IStateMachineTarget, object>.State();
                const string name = nameof(suspended);
                __instance.CreateStates(suspended);
                __instance.BindState(__instance.alive, suspended, name);

                __instance.alive.normal
                    .ToggleStateMachine(smi => new MoveToLocationMonitor.Instance(smi.master)) // иди туды
                    .TagTransition(RobotSuspend, suspended, false);

                suspended
                    .TagTransition(RobotSuspend, __instance.alive.normal, true)
                    .ToggleStatusItem(NAME, TOOLTIP)
                    .ToggleAttributeModifier("save battery",
                        smi => SuspendedBatteryModifiers[smi.PrefabID()],
                        smi => SuspendedBatteryModifiers.ContainsKey(smi.PrefabID()))
                    .ToggleBrain(name)
                    .Enter(smi =>
                    {
                        smi.GetComponent<Navigator>().Pause(name);
                        smi.GetComponent<Storage>().DropAll();
                    })
                    .ScheduleActionNextFrame("Clean StatusItem", CleanStatusItem)
                    .ToggleStateMachine(smi => new RobotSleepStates.Instance(smi.master))
                    .ToggleStateMachine(smi => new FallWhenDeadMonitor.Instance(smi.master))
                    .Exit(smi => smi.GetComponent<Navigator>().Unpause(name));
            }

            // очищаем лишний StatusItem который может появиться при прерывании выполнения FetchAreaChore
            private static void CleanStatusItem(StateMachine.Instance smi)
            {
                if (!smi.IsNullOrStopped() && smi.gameObject.TryGetComponent<KSelectable>(out var selectable))
                    selectable.SetStatusItem(Db.Get().StatusItemCategories.Main, null, null);
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
    }
}
