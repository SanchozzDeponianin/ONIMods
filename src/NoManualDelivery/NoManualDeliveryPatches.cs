﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using TUNING;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace NoManualDelivery
{
    internal sealed class NoManualDeliveryPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(NoManualDeliveryPatches));
            new POptions().RegisterOptions(this, typeof(NoManualDeliveryOptions));
            NoManualDeliveryOptions.Reload();

            // разрешить руке хватать бутылки
            if (NoManualDeliveryOptions.Instance.AllowTransferArmPickupGasLiquid)
            {
                STORAGEFILTERS.SOLID_TRANSFER_ARM_CONVEYABLE = STORAGEFILTERS.SOLID_TRANSFER_ARM_CONVEYABLE
                    .Append(STORAGEFILTERS.GASES).Append(STORAGEFILTERS.LIQUIDS);
                BuildingToMakeAutomatable.AddRange(BuildingToMakeAutomatableWithTransferArmPickupGasLiquid);
            }

            // подготовка, чтобы разрешить дупликам забирать жеготных из инкубатора и всегда хватать еду
            AlwaysCouldBePickedUpByMinionTags = new Tag[] { GameTags.Creatures.Deliverable };
            if (NoManualDeliveryOptions.Instance.AllowAlwaysPickupEdible)
            {
                AlwaysCouldBePickedUpByMinionTags = AlwaysCouldBePickedUpByMinionTags.Append(STORAGEFILTERS.FOOD).Append(GameTags.MedicalSupplies);
            }
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        // ручной список добавленных построек
        private static List<string> BuildingToMakeAutomatable = new List<string>()
        {
            StorageLockerConfig.ID,
            StorageLockerSmartConfig.ID,
            ObjectDispenserConfig.ID,
            StorageTileConfig.ID,
            RationBoxConfig.ID,
            RefrigeratorConfig.ID,
            DiningTableConfig.ID,
            OuthouseConfig.ID,
            FarmTileConfig.ID,
            HydroponicFarmConfig.ID,
            PlanterBoxConfig.ID,
            CreatureFeederConfig.ID,
            FishFeederConfig.ID,
            EggIncubatorConfig.ID,
            IceCooledFanConfig.ID,
            SolidBoosterConfig.ID,
            ResearchCenterConfig.ID,
            SweepBotStationConfig.ID,
            SpiceGrinderConfig.ID,
            GeoTunerConfig.ID,
            MissileLauncherConfig.ID,
            // из ДЛЦ:
            NuclearReactorConfig.ID,
            NoseconeHarvestConfig.ID,
            RailGunPayloadOpenerConfig.ID,
            // из ДЛЦ2:
            IceKettleConfig.ID,
            // из ДЛЦ3:
            ElectrobankChargerConfig.ID,
            LargeElectrobankDischargerConfig.ID,
            SmallElectrobankDischargerConfig.ID,

            // из модов:
            // Aquatic Farm https://steamcommunity.com/sharedfiles/filedetails/?id=1910961538
            "AquaticFarm",
            // Advanced Refrigeration https://steamcommunity.com/sharedfiles/filedetails/?id=2021324045
            "SimpleFridge", "FridgeRed", "FridgeYellow", "FridgeBlue", "FridgeAdvanced",
            "FridgePod", "SpaceBox", "HightechSmallFridge", "HightechBigFridge",
            // Storage Pod  https://steamcommunity.com/sharedfiles/filedetails/?id=1873476551
            "StoragePodConfig",
            // Big Storage  https://steamcommunity.com/sharedfiles/filedetails/?id=1913589787
            "BigSolidStorage",
            "BigBeautifulStorage",
            // Trashcans    https://steamcommunity.com/sharedfiles/filedetails/?id=2037089892
            "SolidTrashcan",
            // Insulated Farm Tiles https://steamcommunity.com/sharedfiles/filedetails/?id=1850356486
            "InsulatedFarmTile", "InsulatedHydroponicFarm",
            // Freezer https://steamcommunity.com/sharedfiles/filedetails/?id=2618339179
            "Freezer",
            // Modified Storage https://steamcommunity.com/sharedfiles/filedetails/?id=1900617368
            "Ktoblin.ModifiedRefrigerator", "Ktoblin.ModifiedStorageLockerSmart",
            // Gravitas Shipping Container https://steamcommunity.com/sharedfiles/filedetails/?id=2942005501
            "GravitasBigStorage_Container",
            // Big Storage Restoraged https://steamcommunity.com/sharedfiles/filedetails/?id=3059711743
            "BigStorageLocker", "BigBeautifulStorageLocker", "BigSmartStorageLocker", "BigStorageTile",
        };

        private static List<string> BuildingToMakeAutomatableWithTransferArmPickupGasLiquid = new List<string>()
        {
            AdvancedResearchCenterConfig.ID,
            LiquidPumpingStationConfig.ID,
            LiquidBottlerConfig.ID,
            GasBottlerConfig.ID,
            BottleEmptierConduitGasConfig.ID,
            BottleEmptierConduitLiquidConfig.ID,
            BottleEmptierConfig.ID,
            BottleEmptierGasConfig.ID,
            // из модов:
            // Fluid Shipping https://steamcommunity.com/sharedfiles/filedetails/?id=1794548334
            "StormShark.BottleInserter",
            "StormShark.CanisterInserter",
            // Automated Canisters https://steamcommunity.com/sharedfiles/filedetails/?id=1824410623
            "asquared31415.PipedLiquidBottler",
        };

        // добавляем компонент к постройкам
        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            foreach (var def in Assets.BuildingDefs)
            {
                var go = def?.BuildingComplete;
                if (go != null)
                {
                    if (
                        BuildingToMakeAutomatable.Contains(def.PrefabID)
                        || (go.TryGetComponent<ManualDeliveryKG>(out _) && (go.TryGetComponent<ElementConverter>(out _) || go.TryGetComponent<EnergyGenerator>(out _)) && !go.TryGetComponent<ResearchCenter>(out _))
                        || go.TryGetComponent<ComplexFabricator>(out _)
                        || go.TryGetComponent<TinkerStation>(out _)
                        )
                    {
                        go.AddOrGet<Automatable2>();
                    }
                }
            }
        }

        // разрешить дупликам доставку для Tinkerable объектов, типа генераторов
        [HarmonyPatch(typeof(Tinkerable), "UpdateChore")]
        private static class Tinkerable_UpdateChore
        {
            private static void Postfix(Chore ___chore)
            {
                if (___chore != null && ___chore is FetchChore chore)
                {
                    string id = ChorePreconditions.instance.IsAllowedByAutomation.id;
                    chore.GetPreconditions().RemoveAll(x => x.condition.id == id);
                    chore.automatable = null;
                }
            }
        }

        private static Tag[] AlwaysCouldBePickedUpByMinionTags = new Tag[0];

        // разрешить дупликам забирать жеготных из инкубатора, всегда хватать еду, всегда брать воду из чайника
        [HarmonyPatch(typeof(Pickupable), nameof(Pickupable.CouldBePickedUpByMinion), new Type[] { typeof(int) })]
        private static class Pickupable_CouldBePickedUpByMinion
        {
            private static Func<Pickupable, int, bool> CouldBePickedUpCommon =
                typeof(Pickupable).Detour<Func<Pickupable, int, bool>>("CouldBePickedUpCommon");

            private static bool Prefix(Pickupable __instance, int carrierID, ref bool __result)
            {
                if (__instance.KPrefabID.HasAnyTags(AlwaysCouldBePickedUpByMinionTags)
                    || (NoManualDeliveryOptions.Instance.AllowAlwaysPickupKettle && __instance.targetWorkable is IceKettleWorkable))
                {
                    __result = CouldBePickedUpCommon(__instance, carrierID);
                    return false;
                }
                return true;
            }
        }

        // дуплы обжирающиеся от стресса, будут игнорировать установленную галку
        [HarmonyPatch(typeof(BingeEatChore.StatesInstance), nameof(BingeEatChore.StatesInstance.FindFood))]
        private static class BingeEatChore_StatesInstance_FindFood
        {
            /*
            if ( блаблабла && 
                item.GetComponent<Pickupable>()
        ---         .CouldBePickedUpByMinion(base.GetComponent<KPrefabID>().InstanceID)
        +++         .CouldBePickedUpCommon(base.GetComponent<KPrefabID>().InstanceID)
               )
            {
                блаблабла
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                return TranspilerUtils.Transpile(instructions, method, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var CouldBePickedUpByMinion = typeof(Pickupable).GetMethodSafe(nameof(Pickupable.CouldBePickedUpByMinion), false, typeof(int));
                var CouldBePickedUpCommon = typeof(Pickupable).GetMethodSafe("CouldBePickedUpCommon", false, typeof(int));
                if (CouldBePickedUpByMinion != null && CouldBePickedUpCommon != null)
                {
                    instructions = PPatchTools.ReplaceMethodCallSafe(instructions, CouldBePickedUpByMinion, CouldBePickedUpCommon).ToList();
                    return true;
                }
                return false;
            }
        }

        // чтобы не испортить заголовок окна - сместить галку вниз
        // похоже второго патча достаточно
#if false
        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        private static class DetailsScreen_OnPrefabInit
        {
            private static void Prefix(List<DetailsScreen.SideScreenRef> ___sideScreens)
            {
                DetailsScreen.SideScreenRef sideScreen;
                for (int i = 0; i < ___sideScreens.Count; i++)
                {

                    if (___sideScreens[i].name == "Automatable Side Screen")
                    {
                        sideScreen = ___sideScreens[i];
                        ___sideScreens.RemoveAt(i);
                        ___sideScreens.Insert(0, sideScreen);
                        break;
                    }
                }
            }
        }
#endif

        [HarmonyPatch(typeof(SideScreenContent), nameof(SideScreenContent.GetSideScreenSortOrder))]
        private static class SideScreenContent_GetSideScreenSortOrder
        {
            // ранее в прошлом вылетало на линухах. todo: мониторить ситуацию
            //private static bool Prepare() => Environment.OSVersion.Platform.Equals(PlatformID.Win32NT);
            private static void Postfix(SideScreenContent __instance, ref int __result)
            {
                if (__instance is AutomatableSideScreen)
                    __result = -10;
            }
        }

        // станция бота-подметашки
        // при изменении хранилища станция принудительно помечает все ресурсы "для переноски"
        // изза этого дуплы всегда будут игнорировать установленную галку (другая задача переноски, не учитывает галку вообще)
        // поэтому нужно при установленной галке - отменить пометки "для переноски"

        private static readonly IDetouredField<SweepBotStation, Storage> SWEEP_STORAGE = PDetours.DetourField<SweepBotStation, Storage>("sweepStorage");

        private static void FixSweepBotStationStorage(Storage sweepStorage)
        {
            bool automationOnly = (sweepStorage.automatable?.GetAutomationOnly()) ?? false;
            for (int i = 0; i < sweepStorage.Count; i++)
            {
                if (sweepStorage[i].TryGetComponent<Clearable>(out var clearable))
                {
                    if (automationOnly)
                        clearable.CancelClearing();
                    else
                        clearable.MarkForClear(false, true);
                }
            }
        }

        private static void UpdateSweepBotStationStorage(KMonoBehaviour kMonoBehaviour)
        {
            if (kMonoBehaviour.TryGetComponent<SweepBotStation>(out var sweepBotStation))
            {
                var storage = SWEEP_STORAGE.Get(sweepBotStation);
                if (storage != null)
                    FixSweepBotStationStorage(storage);
            }
        }

        [HarmonyPatch(typeof(SweepBotStation), "OnStorageChanged")]
        private static class SweepBotStation_OnStorageChanged
        {
            private static void Postfix(Storage ___sweepStorage)
            {
                FixSweepBotStationStorage(___sweepStorage);
            }
        }

        // нужно обновить хранилище при загрузке, и при изменении галки
        [HarmonyPatch(typeof(SweepBotStation), "OnSpawn")]
        private static class SweepBotStation_OnSpawn
        {
            private static void Postfix(Storage ___sweepStorage)
            {
                FixSweepBotStationStorage(___sweepStorage);
            }
        }

        [HarmonyPatch(typeof(Automatable), "OnCopySettings")]
        private static class Automatable_OnCopySettings
        {
            private static void Postfix(Automatable __instance)
            {
                UpdateSweepBotStationStorage(__instance);
            }
        }

        [HarmonyPatch(typeof(AutomatableSideScreen), "OnAllowManualChanged")]
        private static class AutomatableSideScreen_OnAllowManualChanged
        {
            private static void Postfix(Automatable ___targetAutomatable)
            {
                UpdateSweepBotStationStorage(___targetAutomatable);
            }
        }

        // домег бомжега. галку нужно не показывать до поры. кнопку копировать настройки тоже.
        [HarmonyPatch(typeof(AutomatableSideScreen), nameof(AutomatableSideScreen.IsValidForTarget))]
        private static class AutomatableSideScreen_IsValidForTarget
        {
            private static void Postfix(GameObject target, ref bool __result)
            {
                if (__result && target.TryGetComponent<Automatable2>(out var automatable))
                    __result = automatable.showInUI;
            }
        }

        [HarmonyPatch(typeof(CopyBuildingSettings), "OnRefreshUserMenu")]
        private static class CopyBuildingSettings_OnRefreshUserMenu
        {
            private static bool Prefix(CopyBuildingSettings __instance)
            {
                return __instance.enabled;
            }
        }

        [HarmonyPatch(typeof(LonelyMinionHouse.Instance), nameof(LonelyMinionHouse.Instance.StartSM))]
        private static class LonelyMinionHouse_Instance_StartSM
        {
            private static void Postfix(LonelyMinionHouse.Instance __instance)
            {
                bool StoryComplete = StoryManager.Instance.IsStoryComplete(Db.Get().Stories.LonelyMinion);
                if (__instance.gameObject.TryGetComponent<Automatable2>(out var automatable))
                    automatable.showInUI = StoryComplete;
                if (__instance.gameObject.TryGetComponent<CopyBuildingSettings>(out var settings))
                    settings.enabled = StoryComplete;
            }
        }

        [HarmonyPatch(typeof(LonelyMinionHouse.Instance), nameof(LonelyMinionHouse.Instance.OnCompleteStorySequence))]
        private static class LonelyMinionHouse_Instance_OnCompleteStorySequence
        {
            private static void Postfix(LonelyMinionHouse.Instance __instance)
            {
                if (__instance.gameObject.TryGetComponent<CopyBuildingSettings>(out var settings))
                    settings.enabled = true;
                if (__instance.gameObject.TryGetComponent<Automatable2>(out var automatable))
                    automatable.showInUI = true;
            }
        }

        [HarmonyPatch(typeof(LonelyMinionHouseConfig), nameof(LonelyMinionHouseConfig.ConfigureBuildingTemplate))]
        private static class LonelyMinionHouseConfig_ConfigureBuildingTemplate
        {
            private static void Postfix(GameObject go)
            {
                go.AddOrGet<Automatable2>().showInUI = false;
                go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.StorageLocker;
            }
        }

        // древний окаменелостъ. галку нужно не показывать до окончания раскопок
        [HarmonyPatch(typeof(FossilMine), nameof(FossilMine.SetActiveState))]
        private static class FossilMine_SetActiveState
        {
            private static void Postfix(FossilMine __instance, bool active)
            {
                if (__instance.TryGetComponent<Automatable2>(out var automatable))
                    automatable.showInUI = active;
            }
        }

        // чайник. 
        // убираем AnimOverrides если воду из чайника берёт рука
        [HarmonyPatch(typeof(Workable), nameof(Workable.GetAnim))]
        private static class Workable_GetAnim
        {
            private static bool Prepare() => NoManualDeliveryOptions.Instance.AllowTransferArmPickupGasLiquid;
            private static void Postfix(Workable __instance, WorkerBase worker, ref Workable.AnimInfo __result)
            {
                if (__instance is IceKettleWorkable && !worker.UsesMultiTool())
                    __result.overrideAnims = null;
            }
        }

        // ящег-збрасыватель
        // пофиксим ручное переключение вкл/выкл
        [HarmonyPatch(typeof(ObjectDispenser), "Toggle")]
        internal static class AutomaticDispenser_Toggle
        {
            private static void Postfix(ObjectDispenser.Instance ___smi, bool ___switchedOn)
            {
                ___smi.SetSwitchState(___switchedOn);
            }
        }
    }

    // нужно, чтобы установить галку по умолчанию
    public class Automatable2 : Automatable
    {
        [SerializeField]
        public bool showInUI = true;
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            SetAutomationOnly(false);
        }
    }
}
