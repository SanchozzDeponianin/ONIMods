using HarmonyLib;
using SanchozzONIMods.Lib;

namespace AquaticFarm
{
    internal sealed class AquaticFarmPatches : KMod.UserMod2
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
