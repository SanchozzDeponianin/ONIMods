using UnityEngine;
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

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            SuitRecharger.CheckDifficultySetting();
        }

        // при наполнении пустого костюма - восстанавливаем дыхательный компонент в нормальное состояние
        // чтобы дупель не задохнулся на ровном месте
        [HarmonyPatch(typeof(SuitSuffocationMonitor), nameof(SuitSuffocationMonitor.InitializeStates))]
        private static class SuitSuffocationMonitor_InitializeStates
        {
            private static void Postfix(SuitSuffocationMonitor __instance)
            {
                __instance.nooxygen.Transition(__instance.satisfied, smi => !smi.IsTankEmpty(), UpdateRate.SIM_200ms);
            }
        }

        // скрываем боковой экран если износ костюмов отключен в настройке сложности
        // todo: временно отключено для тестирования
        /*
        [HarmonyPatch(typeof(SingleSliderSideScreen), nameof(SingleSliderSideScreen.IsValidForTarget))]
        private static class SingleSliderSideScreen_IsValidForTarget
        {
            private static void Postfix(GameObject target, ref bool __result)
            {
                if (__result && target.HasTag(SuitRechargerConfig.ID.ToTag()))
                    __result = SuitRecharger.durabilityEnabled;
            }
        }*/

        // исправляем косяк клеев, что все четыре компонента типа Solid/Conduit/Consumer/Dispenser
        // неправильно рассчитывают точку подключения трубы при использовании вторичного порта
        // не учитывая возможное вращение постройки
        [HarmonyPatch(typeof(ConduitConsumer), "GetInputCell")]
        private static class ConduitConsumer_GetInputCell
        {
            private static bool Prefix(ConduitConsumer __instance, ref int __result, ConduitType inputConduitType, Building ___building)
            {
                if (__instance.useSecondaryInput)
                {
                    var secondaryInputs = __instance.GetComponents<ISecondaryInput>();
                    foreach (var secondaryInput in secondaryInputs)
                    {
                        if (secondaryInput.HasSecondaryConduitType(inputConduitType))
                        {
                            __result = Grid.OffsetCell(___building.NaturalBuildingCell(),
                                ___building.GetRotatedOffset(secondaryInput.GetSecondaryConduitOffset(inputConduitType)));
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ConduitDispenser), "GetOutputCell")]
        private static class ConduitDispenser_GetOutputCell
        {
            private static bool Prefix(ConduitDispenser __instance, ref int __result, ConduitType outputConduitType, Building ___building)
            {
                if (__instance.useSecondaryOutput)
                {
                    var secondaryOutputs = __instance.GetComponents<ISecondaryOutput>();
                    foreach (var secondaryOutput in secondaryOutputs)
                    {
                        if (secondaryOutput.HasSecondaryConduitType(outputConduitType))
                        {
                            __result = Grid.OffsetCell(___building.NaturalBuildingCell(),
                                ___building.GetRotatedOffset(secondaryOutput.GetSecondaryConduitOffset(outputConduitType)));
                            return false;
                        }
                    }
                }
                return true;
            }
        }
    }
}
