using HarmonyLib;

namespace OldLiquidReservoir
{
    internal sealed class OldLiquidReservoirPatches : KMod.UserMod2
    {
        [HarmonyPatch(typeof(LiquidReservoirConfig), nameof(LiquidReservoirConfig.CreateBuildingDef))]
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
