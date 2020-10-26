using Harmony;
using UnityEngine;

namespace StackableLumber
{
    internal static class StackableLumberPatches
    {
        [HarmonyPatch(typeof(EntitySplitter), "OnPrefabInit")]
        internal static class EntitySplitter_OnPrefabInit
        {
            private static void Postfix(EntitySplitter __instance)
            {
                if (__instance.maxStackSize == float.MaxValue)
                    __instance.maxStackSize = PrimaryElement.MAX_MASS;
            }
        }

        private static void SetMaxStackSize (ref GameObject gameObject, float maxStackSize = 1000f)
        {
            EntitySplitter entitySplitter = gameObject.GetComponent<EntitySplitter>();
            if (entitySplitter != null)
            {
                entitySplitter.maxStackSize = maxStackSize;
            }
        }

        [HarmonyPatch(typeof(EntityTemplates), "ExtendEntityToFood")]
        internal static class EntityTemplates_ExtendEntityToFood
        {
            private static void Postfix(ref GameObject __result)
            {
                SetMaxStackSize(ref __result);
            }
        }

        [HarmonyPatch(typeof(EntityTemplates), "CreateAndRegisterSeedForPlant")]
        internal static class EntityTemplates_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                SetMaxStackSize(ref __result);
            }
        }

        [HarmonyPatch(typeof(BabyCrabShellConfig), "CreatePrefab")]
        internal static class BabyCrabShellConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                SetMaxStackSize(ref __result);
            }
        }

        [HarmonyPatch(typeof(CrabShellConfig), "CreatePrefab")]
        internal static class CrabShellConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                SetMaxStackSize(ref __result);
            }
        }

        [HarmonyPatch(typeof(BasicFabricConfig), "CreatePrefab")]
        internal static class BasicFabricConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                SetMaxStackSize(ref __result);
            }
        }

        [HarmonyPatch(typeof(EggShellConfig), "CreatePrefab")]
        internal static class EggShellConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                SetMaxStackSize(ref __result);
            }
        }

        [HarmonyPatch(typeof(GasGrassHarvestedConfig), "CreatePrefab")]
        internal static class GasGrassHarvestedConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                SetMaxStackSize(ref __result);
            }
        }

        [HarmonyPatch(typeof(RotPileConfig), "CreatePrefab")]
        internal static class RotPileConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                SetMaxStackSize(ref __result);
            }
        }

        [HarmonyPatch(typeof(SwampLilyFlowerConfig), "CreatePrefab")]
        internal static class SwampLilyFlowerConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                SetMaxStackSize(ref __result);
            }
        }

        [HarmonyPatch(typeof(TableSaltConfig), "CreatePrefab")]
        internal static class TableSaltConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                SetMaxStackSize(ref __result);
            }
        }

        [HarmonyPatch(typeof(WoodLogConfig), "CreatePrefab")]
        internal static class WoodLogConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                SetMaxStackSize(ref __result, 25000f);
            }
        }
    }
}
