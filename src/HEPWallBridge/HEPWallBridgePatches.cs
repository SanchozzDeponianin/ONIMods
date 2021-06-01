using Harmony;
using SanchozzONIMods.Lib;
using PeterHan.PLib;

namespace HEPWallBridge
{
    internal static class HEPWallBridgePatches
    {
        public static void OnLoad()
        {
            PUtil.InitLibrary();
            PUtil.RegisterPatchClass(typeof(HEPWallBridgePatches));
        }

        [PLibMethod(RunAt.AfterModsLoad)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            Utils.AddBuildingToPlanScreen("HEP", HighEnergyParticleWallBridgeRedirectorConfig.ID);
            Utils.AddBuildingToTechnology("NuclearResearch", HighEnergyParticleWallBridgeRedirectorConfig.ID);
            PUtil.CopySoundsToAnim("wallbridge_orb_transporter_kanim", "orb_transporter_kanim");
        }

        // ой мудакии!
        // позиция входного рад.порта вычисляется в двух разных формулах.
        // одна их которых не учитывает вращение постройки. и при ненулевом смещении порта получается котовасия.
        [HarmonyPatch(typeof(HighEnergyParticlePort), "OnSpawn")]
        internal static class HighEnergyParticlePort_OnSpawn
        {
            private static void Postfix(HighEnergyParticlePort __instance)
            {
                var building = __instance.GetComponent<Building>();
                if (building != null)
                {
                    __instance.particleInputOffset = building.GetRotatedOffset(building.Def.HighEnergyParticleInputOffset);
                }
            }
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
