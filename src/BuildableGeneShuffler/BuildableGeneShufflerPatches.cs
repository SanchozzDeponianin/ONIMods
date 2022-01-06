using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace BuildableGeneShuffler
{
    internal sealed class BuildableGeneShufflerPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(BuildableGeneShufflerPatches));
            new POptions().RegisterOptions(this, typeof(BuildableGeneShufflerOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            // todo: определиться с технологией и местом в мюню // "Equipment", "ResetSkillsStation", "AdvancedResearch"
            Utils.AddBuildingToPlanScreen("Equipment", BuildableGeneShufflerConfig.ID, ResetSkillsStationConfig.ID);
            Utils.AddBuildingToTechnology("AdvancedResearch", BuildableGeneShufflerConfig.ID);
            PGameUtils.CopySoundsToAnim(BuildableGeneShufflerConfig.anim, "geneshuffler_kanim");
        }

        [HarmonyPatch(typeof(GeneShufflerConfig), nameof(GeneShufflerConfig.CreatePrefab))]
        private static class GeneShufflerConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<BuildedGeneShuffler>();
            }
        }
    }
}
