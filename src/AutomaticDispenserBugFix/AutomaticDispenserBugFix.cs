using HarmonyLib;

namespace AutomaticDispenserBugFix
{
    internal sealed class AutomaticDispenserBugFixPatches : KMod.UserMod2
    {
        [HarmonyPatch(typeof(ObjectDispenser), "Toggle")]
        internal static class AutomaticDispenser_Toggle
        {
            private static void Postfix(ObjectDispenser.Instance ___smi, bool ___switchedOn)
            {
                ___smi.SetSwitchState(___switchedOn);
            }
        }
    }
}
