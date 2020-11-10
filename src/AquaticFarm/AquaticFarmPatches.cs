using Harmony;
using SanchozzONIMods.Lib;

namespace AquaticFarm
{
    internal static class AquaticFarmPatches
    {
        [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
        internal static class GeneratedBuildings_LoadGeneratedBuildings
        {
            private static void Prefix()
            {
                Utils.AddBuildingToPlanScreen("Food", AquaticFarmConfig.ID, FarmTileConfig.ID);
            }
        }

        [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
        internal static class Db_Initialize
        {
            private static void Prefix()
            {
                Utils.AddBuildingToTechnology("FineDining", AquaticFarmConfig.ID);
            }
        }

        [HarmonyPatch(typeof(Localization), nameof(Localization.Initialize))]
        internal static class Localization_Initialize
        {
            private static void Postfix(Localization.Locale ___sLocale)
            {
                Utils.InitLocalization(typeof(STRINGS), ___sLocale);
                LocString.CreateLocStringKeys(typeof(STRINGS.BUILDINGS));
            }
        }
    }
}
