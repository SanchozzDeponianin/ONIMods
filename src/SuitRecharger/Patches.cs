﻿using System.Collections.Generic;
using KMod;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using SanchozzONIMods.Shared;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;

namespace SuitRecharger
{
    using static SuitRecharger;
    internal sealed class Patches : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
        }

        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods)
        {
            base.OnAllModsLoaded(harmony, mods);
            ManualDeliveryKGPatch.Patch(harmony);
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            Utils.AddBuildingToPlanScreen(BUILD_CATEGORY.Equipment, SuitRechargerConfig.ID, BUILD_SUBCATEGORY.equipment, SuitFabricatorConfig.ID);
            Utils.AddBuildingToTechnology("ImprovedGasPiping", SuitRechargerConfig.ID);
            PGameUtils.CopySoundsToAnim("suitrecharger_kanim", "suit_maker_kanim");
            Init();
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            DurabilityMode = DurabilitySetting.Unknown;
            ValidateRepairMaterials();
        }

        [PLibMethod(RunAt.OnEndGame)]
        private static void OnEndGame()
        {
            DurabilityMode = DurabilitySetting.Unknown;
        }

        [PLibMethod(RunAt.OnDetailsScreenInit)]
        private static void OnDetailsScreenInit()
        {
            PUIUtils.AddSideScreenContent<SuitRechargerSideScreen>();
        }

        // задача "подышать" не учитывает цену пути до дыхабельной клетки
        // добавим эту проверку, чтобы задыхающийся дупель мог "подышать" или использовать зарядник 
        // в зависимости от того что ближе
        [HarmonyPatch(typeof(BreathMonitor), "CreateRecoverBreathChore")]
        private static class BreathMonitor_CreateRecoverBreathChore
        {
            private static void Postfix(Chore __result, BreathMonitor.Instance smi)
            {
                System.Func<int> GetCell = () => smi.GetRecoverCell();
                __result.AddPrecondition(ChorePreconditions.instance.CanMoveToDynamicCell, GetCell);
            }
        }

        // задыхающийся дупель после использования зарядника всеравно хочет "подышать"
        // подменим целевую клетку, чтобы он не бежал далеко, а "отдышался" возле зарядника
        [HarmonyPatch(typeof(RecoverBreathChore.StatesInstance), nameof(RecoverBreathChore.StatesInstance.UpdateLocator))]
        private static class RecoverBreathChore_StatesInstance_UpdateLocator
        {
            private static bool Prefix(RecoverBreathChore.StatesInstance __instance)
            {
                var prefabID = __instance.Get<KPrefabID>();
                if (prefabID.HasTag(GameTags.HasSuitTank) && !prefabID.HasTag(GameTags.NoOxygen))
                {
                    int num = Grid.PosToCell(__instance.sm.recoverer.Get<Transform>(__instance).GetPosition());
                    Vector3 position = Grid.CellToPosCBC(num, Grid.SceneLayer.Move);
                    __instance.sm.locator.Get<Transform>(__instance).SetPosition(position);
                    return false;
                }
                else
                    return true;
            }
        }

        // косметика. если дупель "отдышывается" после зарядника, подавляем исчезновение шлема
        [HarmonyPatch(typeof(HelmetController), "OnBeginRecoverBreath")]
        private static class HelmetController_OnBeginRecoverBreath
        {
            private static bool Prefix(Navigator ___owner_navigator)
            {
                return ___owner_navigator == null || ___owner_navigator.HasTag(GameTags.NoOxygen);
            }
        }

        // если дупель уже принял задачу зарядить костюм, но снял его по дороге
        // отменяем ему эту задачу
        [HarmonyPatch(typeof(RationalAi), nameof(RationalAi.InitializeStates))]
        private static class RationalAi_InitializeStates
        {
            private static void Postfix(RationalAi __instance)
            {
                __instance.alive.EventHandler(GameHashes.TagsChanged, (smi, obj) =>
                {
                    var tag = (TagChangedEventData)obj;
                    if (tag.added == false && tag.tag == GameTags.HasSuitTank && GameScheduler.Instance != null)
                    {
                        GameScheduler.Instance.Schedule("StopRecharge", 2 * UpdateManager.SecondsPerSimTick, (o) =>
                        {
                            var driver = smi?.GetComponent<ChoreDriver>();
                            if (driver != null)
                            {
                                var chore = driver.GetCurrentChore();
                                if (chore != null && (chore.choreType == Db.Get().ChoreTypes.Recharge || chore.choreType == RecoverBreathRecharge || chore.choreType == Db.Get().ChoreTypes.RecoverBreath))
                                {
                                    driver.StopChore();
                                }
                            }
                        }, null, null);
                    }
                });
            }
        }

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
