using HarmonyLib;
using UnityEngine;
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
            // заменяем технологию для клеевской пластины
            var klei_tech_current = Db.Get().Techs.TryGetTechForTechItem(HEPBridgeTileConfig.ID);
            var klei_tech_new_id = ModOptions.Instance.research_klei.ToString();
            if (klei_tech_current != null && klei_tech_current.Id != klei_tech_new_id)
            {
                klei_tech_current.unlockedItemIDs.Remove(HEPBridgeTileConfig.ID);
                Utils.AddBuildingToTechnology(klei_tech_new_id, HEPBridgeTileConfig.ID);
            }
            var mod_tech_id = ModOptions.Instance.research_mod.ToString();
            Utils.AddBuildingToTechnology(mod_tech_id, HEPBridgeInsulationTileConfig.ID);
        }

        // чтобы работало копирование настроек между простым редиректором и нашим.
        [HarmonyPatch(typeof(HighEnergyParticleRedirectorConfig), nameof(HighEnergyParticleRedirectorConfig.ConfigureBuildingTemplate))]
        private static class HighEnergyParticleRedirectorConfig_ConfigureBuildingTemplate
        {
            private static void Postfix(GameObject go)
            {
                go.AddOrGet<CopyBuildingSettings>().copyGroupTag = HighEnergyParticleRedirectorConfig.ID;
            }
        }
    }
}
