using HarmonyLib;
using UnityEngine;

namespace AutomaticDispenserOnlyTransferFromLowerPriority
{
    internal sealed class AutomaticDispenserOnlyTransferFromLowerPriorityPatches : KMod.UserMod2
    {
        [HarmonyPatch(typeof(ObjectDispenserConfig), nameof(ObjectDispenserConfig.DoPostConfigureComplete))]
        internal static class AutomaticDispenser_DoPostConfigureComplete
        {
            private static void Postfix(GameObject go)
            {
                go.GetComponent<Storage>().onlyTransferFromLowerPriority = true;
            }
        }
    }
}
