using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace LargeTelescope
{
    internal sealed class LargeTelescopePatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(LargeTelescopePatches));
            new POptions().RegisterOptions(this, typeof(LargeTelescopeOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            // todo: уточнить техи и мюню
            Utils.AddBuildingToPlanScreen("Furniture", ClusterLargeTelescopeConfig.ID, EspressoMachineConfig.ID);
            Utils.AddBuildingToTechnology("SpaceProgram", ClusterLargeTelescopeConfig.ID);
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            // todo: а нужно ли ?
            LargeTelescopeOptions.Reload();
        }
    }
}
