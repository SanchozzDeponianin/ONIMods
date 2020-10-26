using Harmony;
using UnityEngine;

namespace TweakedBiologicalCargoBay
{
    [HarmonyPatch(typeof(SpecialCargoBayConfig), "DoPostConfigureComplete")]
    internal static class SpecialCargoBayConfig_DoPostConfigureComplete
    {
        private static void Postfix(GameObject go)
        {
            go.GetComponent<Storage>().allowItemRemoval = true;
        }
    }
}
