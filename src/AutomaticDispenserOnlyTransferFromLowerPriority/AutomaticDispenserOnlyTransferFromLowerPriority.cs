using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace AutomaticDispenserOnlyTransferFromLowerPriority
{
    internal sealed class AutomaticDispenserOnlyTransferFromLowerPriorityPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            this.LogModVersion();
            base.OnLoad(harmony);
        }

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
