using Harmony;
using UnityEngine;

namespace AutomaticDispenserOnlyTransferFromLowerPriority
{
    [HarmonyPatch(typeof(ObjectDispenserConfig), "DoPostConfigureComplete")]
    public class AutomaticDispenser_DoPostConfigureComplete
    {
        public static void Postfix(GameObject go)
        {
            go.GetComponent<Storage>().onlyTransferFromLowerPriority = true;
        }
    }
}
