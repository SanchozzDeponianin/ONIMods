using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;

namespace HEPBridgeInsulationTile
{
    internal sealed class HEPBridgeInsulationTilePatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(HEPBridgeInsulationTilePatches));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            Utils.AddBuildingToPlanScreen("HEP", HEPBridgeInsulationTileConfig.ID, HEPBridgeTileConfig.ID);
            var KleiHEPBridgeTileTech = Db.Get().Techs.TryGetTechForTechItem(HEPBridgeTileConfig.ID)?.Id ?? "NuclearRefinement";
            Utils.AddBuildingToTechnology(KleiHEPBridgeTileTech, HEPBridgeInsulationTileConfig.ID);
            PGameUtils.CopySoundsToAnim("wallbridge_orb_transporter_kanim", "orb_transporter_kanim");
        }
    }
}
