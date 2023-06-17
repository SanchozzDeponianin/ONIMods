using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace TweakedBiologicalCargoBay
{
    internal sealed class TweakedBiologicalCargoBayPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Utils.LogModVersion();
            base.OnLoad(harmony);
        }

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
