using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
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
            new POptions().RegisterOptions(this, typeof(HEPBridgeInsulationTileOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            Utils.AddBuildingToPlanScreen("HEP", HEPBridgeInsulationTileConfig.ID, "HEP", HEPBridgeTileConfig.ID);
            PGameUtils.CopySoundsToAnim("wallbridge_orb_transporter_kanim", "orb_transporter_kanim");
            // заменяем технологию для клеевской пластины
            var klei_tech_current = Db.Get().Techs.TryGetTechForTechItem(HEPBridgeTileConfig.ID);
            var klei_tech_new_id = HEPBridgeInsulationTileOptions.Instance.research_klei.ToString();
            if (klei_tech_current != null && klei_tech_current.Id != klei_tech_new_id)
            {
                klei_tech_current.unlockedItemIDs.Remove(HEPBridgeTileConfig.ID);
                Utils.AddBuildingToTechnology(klei_tech_new_id, HEPBridgeTileConfig.ID);
            }
            var mod_tech_id = HEPBridgeInsulationTileOptions.Instance.research_mod.ToString();
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
