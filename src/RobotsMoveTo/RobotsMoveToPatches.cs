using HarmonyLib;
using SanchozzONIMods.Lib;

namespace RobotsMoveTo
{
    internal sealed class RobotsMoveToPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Utils.LogModVersion();
            base.OnLoad(harmony);
        }

        [HarmonyPatch(typeof(RobotAi), nameof(RobotAi.InitializeStates))]
        private static class RobotAi_InitializeStates
        {
            private static void Postfix(RobotAi __instance)
            {
                __instance.alive.normal.ToggleStateMachine(smi => new MoveToLocationMonitor.Instance(smi.master));
            }
        }
    }
}
