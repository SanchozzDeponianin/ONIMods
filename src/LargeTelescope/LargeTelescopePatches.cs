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
            LargeTelescopeOptions.Reload();
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
            Utils.AddBuildingToPlanScreen("Rocketry", ClusterLargeTelescopeConfig.ID, ClusterTelescopeConfig.ID);
            Utils.AddBuildingToTechnology("CrashPlan", ClusterLargeTelescopeConfig.ID);
        }

        [HarmonyPatch(typeof(ClusterTelescope.ClusterTelescopeWorkable), "OnWorkableEvent")]
        private static class ClusterTelescope_ClusterTelescopeWorkable_OnWorkableEvent
        {
            private static bool Prepare() => LargeTelescopeOptions.Instance.FixNoConsumePowerBug;

            private static void Postfix(ClusterTelescope.ClusterTelescopeWorkable __instance, Workable.WorkableEvent ev)
            {
                switch (ev)
                {
                    case Workable.WorkableEvent.WorkStarted:
                        __instance.GetComponent<Operational>()?.SetActive(true);
                        break;
                    case Workable.WorkableEvent.WorkStopped:
                        __instance.GetComponent<Operational>()?.SetActive(false);
                        break;
                }
            }
        }
    }
}
