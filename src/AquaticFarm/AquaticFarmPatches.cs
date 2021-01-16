using Harmony;
using SanchozzONIMods.Lib;

namespace AquaticFarm
{
    internal static class AquaticFarmPatches
    {
        [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
        internal static class Db_Initialize
        {
            private static void Postfix()
            {
                Utils.AddBuildingToPlanScreen("Food", AquaticFarmConfig.ID, FarmTileConfig.ID);
                Utils.AddBuildingToTechnology("FineDining", AquaticFarmConfig.ID);
            }
        }

        [HarmonyPatch(typeof(Localization), nameof(Localization.Initialize))]
        internal static class Localization_Initialize
        {
            private static void Postfix()
            {
                Utils.InitLocalization(typeof(STRINGS));
                LocString.CreateLocStringKeys(typeof(STRINGS.BUILDINGS));
            }
        }
    }
}
