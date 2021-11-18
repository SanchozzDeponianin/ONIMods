using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;

namespace SuitRecharger
{
    internal sealed class SuitRechargerPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(SuitRechargerPatches));
        }
        /*
        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
            LocString.CreateLocStringKeys(typeof(STRINGS.BUILDINGS));
        }*/

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            Utils.AddBuildingToPlanScreen("Equipment", SuitRechargerConfig.ID, SuitFabricatorConfig.ID);
            Utils.AddBuildingToTechnology("ImprovedGasPiping", SuitRechargerConfig.ID);
            PGameUtils.CopySoundsToAnim("suitrecharger_kanim", "suit_maker_kanim");
        }

        // при наполнении пустого костюма - восстанавливаем дыхательный компонент в нормальное состояние
        // чтобы дупель не задохнулся на ровном месте
        [HarmonyPatch(typeof(SuitSuffocationMonitor), nameof(SuitSuffocationMonitor.InitializeStates))]
        internal static class SuitSuffocationMonitor_InitializeStates
        {
            private static void Postfix(SuitSuffocationMonitor __instance)
            {
                __instance.nooxygen.Transition(__instance.satisfied, smi => !smi.IsTankEmpty(), UpdateRate.SIM_200ms);
            }
        }
    }
}
