using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;

namespace HEPWallBridge
{
    internal sealed class HEPWallBridgePatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(HEPWallBridgePatches));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            Utils.AddBuildingToPlanScreen("HEP", HighEnergyParticleWallBridgeRedirectorConfig.ID);
            Utils.AddBuildingToTechnology("NuclearResearch", HighEnergyParticleWallBridgeRedirectorConfig.ID);
            PGameUtils.CopySoundsToAnim("wallbridge_orb_transporter_kanim", "orb_transporter_kanim");
        }

        // включаем и выключаем потребление искричества
        [HarmonyPatch(typeof(HighEnergyParticleRedirector.States), nameof(HighEnergyParticleRedirector.States.InitializeStates))]
        internal static class HighEnergyParticleRedirector_States_InitializeStates
        {
            private static void Postfix(HighEnergyParticleRedirector.States __instance)
            {
                __instance.redirect
                    .Enter(smi => smi.GetComponent<Operational>().SetActive(true, false))
                    .Exit(smi => smi.GetComponent<Operational>().SetActive(false, false));
            }
        }
    }
}
