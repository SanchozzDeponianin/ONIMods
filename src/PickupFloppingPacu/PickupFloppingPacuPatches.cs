using HarmonyLib;
using SanchozzONIMods.Lib;

namespace PickupFloppingPacu
{
    internal sealed class PickupFloppingPacuPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
        }

        [HarmonyPatch(typeof(FlopStates), nameof(FlopStates.InitializeStates))]
        private static class FlopStates_InitializeStates
        {
            private static void Postfix(FlopStates __instance)
            {
                __instance.root
                    .ToggleTag(GameTags.Creatures.Deliverable)
                    .EventHandler(GameHashes.OnStore, ForceUpdateBrain);
            }
        }

        private static void ForceUpdateBrain(StateMachine.Instance smi)
        {
            if (smi.gameObject.TryGetComponent<CreatureBrain>(out var brain))
                Game.BrainScheduler.PrioritizeBrain(brain);
        }
    }
}
