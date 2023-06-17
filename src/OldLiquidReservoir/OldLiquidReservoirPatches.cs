using HarmonyLib;
using SanchozzONIMods.Lib;

namespace OldLiquidReservoir
{
    internal sealed class OldLiquidReservoirPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Utils.LogModVersion();
            base.OnLoad(harmony);
        }

        [HarmonyPatch(typeof(LiquidReservoirConfig), nameof(LiquidReservoirConfig.CreateBuildingDef))]
        internal static class LiquidReservoirConfig_CreateBuildingDef
        {
            private static void Postfix(BuildingDef __result)
            {
                __result.AnimFiles = new KAnimFile[]
                {
                    Assets.GetAnim("old_liquidreservoir_kanim")
                };
            }
        }
    }
}
