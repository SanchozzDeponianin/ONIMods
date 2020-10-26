using Harmony;
using SanchozzONIMods.Lib;

namespace AquaticFarm
{
    internal static class AquaticFarmPatches
    {
        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        internal static class GeneratedBuildings_LoadGeneratedBuildings
        {
            private static void Prefix()
            {
                Utils.AddBuildingToPlanScreen("Food", AquaticFarmConfig.ID, "FarmTile");
            }
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        internal static class Db_Initialize
        {
            private static void Prefix()
            {
                Utils.AddBuildingToTechnology("FineDining", AquaticFarmConfig.ID);
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
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
