using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;

namespace AquaticFarm
{
    internal sealed class AquaticFarmPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary();
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(AquaticFarmPatches));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
            LocString.CreateLocStringKeys(typeof(STRINGS.BUILDINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            ModUtil.AddBuildingToPlanScreen(BUILD_CATEGORY.Food, AquaticFarmConfig.ID, BUILD_SUBCATEGORY.farming, FarmTileConfig.ID);
            Utils.AddBuildingToTechnology("FineDining", AquaticFarmConfig.ID);
        }
    }
}
