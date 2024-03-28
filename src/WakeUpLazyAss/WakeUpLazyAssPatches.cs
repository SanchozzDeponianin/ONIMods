using HarmonyLib;
using SanchozzONIMods.Lib;

namespace WakeUpLazyAss
{
    internal sealed class WakeUpLazyAssPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Utils.LogModVersion();
            base.OnLoad(harmony);
        }

        public static GameHashes SleepDisturbedByKick = (GameHashes)Hash.SDBMLower(nameof(SleepDisturbedByKick));

        [HarmonyPatch(typeof(SleepChore.States), nameof(SleepChore.States.InitializeStates))]
        private static class SleepChore_States_InitializeStates
        {
            private static void Postfix(SleepChore.States __instance)
            {
                var not_so_uninterruptable = __instance.CreateState("not_so_uninterruptable", __instance.sleep.uninterruptable);
                var interrupt_kick = __instance.CreateState("interrupt_kick", __instance.sleep.uninterruptable);
                var interrupt_kick_and_sleep_again = __instance.CreateState("interrupt_kick_and_sleep_again", __instance.sleep.uninterruptable);

                __instance.sleep.uninterruptable
                    .DefaultState(not_so_uninterruptable)
                    .ToggleReactable(smi => new KickMinionReactable(smi.gameObject));

                not_so_uninterruptable
                    .QueueAnim("working_loop", true)
                    .EventHandler(SleepDisturbedByKick, smi =>
                    {
                        if (smi.gameObject.TryGetComponent(out Schedulable schedulable)
                            && schedulable.IsAllowed(Db.Get().ScheduleBlockTypes.Sleep))
                        {
                            smi.GoTo(interrupt_kick_and_sleep_again);
                            return;
                        }
                        var stamina = Db.Get().Amounts.Stamina.Lookup(smi.gameObject);
                        if (stamina != null && stamina.value < stamina.GetMax() * 0.2f)
                        {
                            smi.GoTo(interrupt_kick_and_sleep_again);
                            return;
                        }
                        smi.GoTo(interrupt_kick);
                    });

                interrupt_kick
                    .QueueAnim("working_pst")
                    .OnAnimQueueComplete(__instance.success);

                interrupt_kick_and_sleep_again
                    .QueueAnim("interrupt_light")
                    .OnAnimQueueComplete(not_so_uninterruptable);
            }
        }
    }
}
