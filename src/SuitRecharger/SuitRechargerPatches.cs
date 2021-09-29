using HarmonyLib;
using SanchozzONIMods.Lib;
/*
using PeterHan.PLib;
using PeterHan.PLib.Options;
*/
namespace SuitRecharger
{
    internal sealed class SuitRechargerPatches : KMod.UserMod2
    {
        [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
        internal static class Db_Initialize
        {
            private static void Postfix()
            {
                Utils.AddBuildingToPlanScreen("Equipment", SuitRechargerConfig.ID, SuitFabricatorConfig.ID);
                var tech = DlcManager.IsExpansion1Active() ? "PortableGasses" : "Suits";
                Utils.AddBuildingToTechnology(tech, SuitRechargerConfig.ID);
            }
        }

        /*
        [HarmonyPatch(typeof(Localization), nameof(Localization.Initialize))]
        internal static class Localization_Initialize
        {
            private static void Postfix()
            {
                Utils.InitLocalization(typeof(STRINGS));
                LocString.CreateLocStringKeys(typeof(STRINGS.BUILDINGS));
            }
        }
        */

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
