using Harmony;

namespace OldLiquidReservoir
{
    internal static class OldLiquidReservoirPatches
    {
        [HarmonyPatch(typeof(LiquidReservoirConfig), "CreateBuildingDef")]
        internal static class LiquidReservoirConfig_CreateBuildingDef
        {
            private static void Postfix(ref BuildingDef __result)
            {
                __result.AnimFiles = new KAnimFile[]
                {
                    Assets.GetAnim("old_liquidreservoir_kanim")
                };
            }
        }
    }
}
