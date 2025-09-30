using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SanchozzONIMods.Lib;

namespace DeadPlantsNotifier
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            Utils.RegisterLocalization(typeof(STRINGS));
            base.OnLoad(harmony);
        }

        const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [HarmonyPatch]
        private static class Component_CreateDeathNotification
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new MethodBase[] {
                    typeof(StandardCropPlant).GetMethod(nameof(StandardCropPlant.CreateDeathNotification), flags),
                    typeof(CritterTrapPlant).GetMethod(nameof(CritterTrapPlant.CreateDeathNotification), flags),
                };
            }
            private static void Postfix(StateMachineComponent __instance, Notification __result)
            {
                __result.customClickCallback = __result.CustomClick;
                __result.customClickData = Grid.PosToCell(__instance);
            }
        }

        [HarmonyPatch]
        private static class SMI_CreateDeathNotification
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new MethodBase[] {
                    typeof(SpaceTreePlant).GetMethod(nameof(SpaceTreePlant.CreateDeathNotification), flags),
                    typeof(VineBranch).GetMethod(nameof(VineMother.CreateDeathNotification), flags),
                    typeof(VineMother).GetMethod(nameof(VineMother.CreateDeathNotification), flags),
                };
            }
            private static void Postfix(StateMachine.Instance smi, Notification __result)
            {
                __result.customClickCallback = __result.CustomClick;
                __result.customClickData = Grid.PosToCell(smi);
            }
        }
    }

    internal static class OnClick
    {
        public static void CustomClick(this Notification notification, object data)
        {
            int cell = (int)data;
            if (Grid.IsValidCell(cell) && Grid.WorldIdx[cell] != 255)
            {
                var position = Grid.CellToPosCBC(cell, Grid.SceneLayer.Building);
                position.z = -40f;
                GameUtil.FocusCameraOnWorld(Grid.WorldIdx[cell], position);
            }
        }
    }
}
