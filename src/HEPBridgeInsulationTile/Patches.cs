using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;

namespace HEPBridgeInsulationTile
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
            LoadSprite();
        }

        private static void LoadSprite()
        {
            const string name = "ui_extrudable";
            var sprite = PUIUtils.LoadSprite($"sprites/{name}.png");
            sprite.name = name;
            Assets.Sprites.Add(name, sprite);
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            Utils.AddBuildingToPlanScreen(BUILD_CATEGORY.HEP, HEPBridgeInsulationTileConfig.ID, BUILD_SUBCATEGORY.transmissions, HEPBridgeTileConfig.ID);
            var mod_tech_id = ModOptions.Instance.research_mod.ToString();
            Utils.AddBuildingToTechnology(mod_tech_id, HEPBridgeInsulationTileConfig.ID);
        }
    }
}
