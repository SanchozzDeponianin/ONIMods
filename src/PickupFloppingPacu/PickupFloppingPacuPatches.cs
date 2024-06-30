using HarmonyLib;
using SanchozzONIMods.Lib;

namespace PickupFloppingPacu
{
    internal sealed class PickupFloppingPacuPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
        }

        [HarmonyPatch(typeof(FlopStates), nameof(FlopStates.InitializeStates))]
        private static class FlopStates_InitializeStates
        {
            private static void Postfix(FlopStates __instance)
            {
                __instance.root
                    .ToggleTag(GameTags.Creatures.Deliverable)
                    .EventHandler(GameHashes.OnStore, ScheduleUpdateBrain);
            }
        }

        private static void ScheduleUpdateBrain(StateMachine.Instance smi)
        {
            if (smi.gameObject.TryGetComponent<CreatureBrain>(out var brain))
                GameScheduler.Instance.ScheduleNextFrame(null, ForceUpdateBrain, brain);
        }

        private static void ForceUpdateBrain(object data)
        {
            var brain = data as CreatureBrain;
            if (brain != null && brain.IsRunning())
                brain.UpdateBrain();
        }
    }
}
