using HarmonyLib;
using UnityEngine;

namespace TweakedBiologicalCargoBay
{
    internal sealed class TweakedBiologicalCargoBayPatches : KMod.UserMod2
    {
        [HarmonyPatch(typeof(SpecialCargoBayConfig), nameof(SpecialCargoBayConfig.DoPostConfigureComplete))]
        internal static class SpecialCargoBayConfig_DoPostConfigureComplete
        {
            private static void Postfix(GameObject go)
            {
                go.GetComponent<Storage>().allowItemRemoval = true;
            }
        }
    }
}
