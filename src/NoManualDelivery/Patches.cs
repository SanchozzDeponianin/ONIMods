using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using TUNING;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;

namespace NoManualDelivery
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            ModOptions.Reload();

            // разрешить руке хватать бутылки
            if (ModOptions.Instance.AllowTransferArmPickupGasLiquid)
            {
                STORAGEFILTERS.SOLID_TRANSFER_ARM_CONVEYABLE = STORAGEFILTERS.SOLID_TRANSFER_ARM_CONVEYABLE
                    .Append(STORAGEFILTERS.GASES).Append(STORAGEFILTERS.LIQUIDS);
                BuildingToMakeAutomatable.AddRange(BuildingToMakeAutomatableWithTransferArmPickupGasLiquid);
            }

            // подготовка, чтобы разрешить дупликам забирать жеготных из инкубатора и всегда хватать еду
            AlwaysCouldBePickedUpByMinionTags = new Tag[] { GameTags.Creatures.Deliverable };
            if (ModOptions.Instance.AllowAlwaysPickupEdible)
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
        private static List<string> BuildingToMakeAutomatable = new()
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
            // Ronivan's Legacy https://steamcommunity.com/sharedfiles/filedetails/?id=3557584850
            "SimpleFridge", "FridgeRed", "FridgeYellow", "FridgeBlue", "FridgeAdvanced",
            "FridgePod", "SpaceBox", "HightechSmallFridge", "HightechBigFridge", "AIO_FridgeLarge",
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

        private static List<string> BuildingToMakeAutomatableWithTransferArmPickupGasLiquid = new()
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

        private static List<string> BuildingWithoutHoldMode = new()
        {
            SweepBotStationConfig.ID,
            LiquidPumpingStationConfig.ID,
            LiquidBottlerConfig.ID,
            GasBottlerConfig.ID,
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
                        if (go.TryGetComponent(out Automatable automatable))
                            UnityEngine.Object.DestroyImmediate(automatable);
                        var automatable2 = go.AddOrGet<Automatable2>();
                        automatable2.allowHold = ModOptions.Instance.HoldMode.Chores && !BuildingWithoutHoldMode.Contains(def.PrefabID);
                        automatable2.SetAutomation(false, ModOptions.Instance.HoldMode.ByDefault);
                    }
                }
            }
            AutomatableHolder.LongTimeout = ModOptions.Instance.HoldMode.Timeout;
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            if (ModOptions.Instance.HoldMode.Enabled)
                Game.Instance.FindOrAdd<TransferArmGroupProber>();
        }

        [PLibMethod(RunAt.OnEndGame)]
        private static void OnEndGame()
        {
            TransferArmGroupProber.DestroyInstance();
        }

        [PLibMethod(RunAt.OnDetailsScreenInit)]
        private static void OnDetailsScreenInit()
        {
            PUIUtils.AddSideScreenContent<Automatable2SideScreen>();
        }

        [HarmonyPatch(typeof(AutomatableSideScreen), nameof(AutomatableSideScreen.IsValidForTarget))]
        private static class AutomatableSideScreen_IsValidForTarget
        {
            private static void Postfix(GameObject target, ref bool __result)
            {
                if (__result && target.TryGetComponent<Automatable>(out var automatable) && automatable is Automatable2)
                    __result = false;
            }
        }


        private static Tag[] AlwaysCouldBePickedUpByMinionTags = new Tag[0];

        // разрешить дупликам забирать жеготных из инкубатора, всегда хватать еду, всегда брать воду из чайника
        // запретить дупликам подбирать вещи если они "зарезервированы" "умным режимом"
        [HarmonyPatch(typeof(Pickupable), nameof(Pickupable.CouldBePickedUpByMinion), new Type[] { typeof(int) })]
        private static class Pickupable_CouldBePickedUpByMinion
        {
            private static bool Prefix(Pickupable __instance, int carrierID, ref bool __result, ref bool __state)
            {
                if (__instance.KPrefabID.HasAnyTags(AlwaysCouldBePickedUpByMinionTags)
                    || (ModOptions.Instance.AllowAlwaysPickupKettle && __instance.targetWorkable is IceKettleWorkable))
                {
                    __result = __instance.CouldBePickedUpCommon(carrierID);
                    __state = false;
                    return false;
                }
                __state = true;
                return true;
            }

            private static void Postfix(Pickupable __instance, ref bool __result, bool __state)
            {
                if (__state && __result && PickupableHolder != null && TryGetHolder(__instance, out var holder))
                {
                    __result = holder.IsTimeOut();
                }
            }
        }

        #region twiks
        // разрешить дупликам доставку для Tinkerable объектов, типа генераторов
        [HarmonyPatch(typeof(Tinkerable), nameof(Tinkerable.UpdateChore))]
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
                return instructions.Transpile(method, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var CouldBePickedUpByMinion = typeof(Pickupable).GetMethodSafe(nameof(Pickupable.CouldBePickedUpByMinion), false, typeof(int));
                var CouldBePickedUpCommon = typeof(Pickupable).GetMethodSafe(nameof(Pickupable.CouldBePickedUpCommon), false, typeof(int));
                if (CouldBePickedUpByMinion != null && CouldBePickedUpCommon != null)
                {
                    instructions = PPatchTools.ReplaceMethodCallSafe(instructions, CouldBePickedUpByMinion, CouldBePickedUpCommon).ToList();
                    return true;
                }
                return false;
            }
        }

        // станция бота-подметашки
        // при изменении хранилища станция принудительно помечает все ресурсы "для переноски"
        // изза этого дуплы всегда будут игнорировать установленную галку (другая задача переноски, не учитывает галку вообще)
        // поэтому нужно при установленной галке - отменить пометки "для переноски"
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

        internal static void UpdateSweepBotStationStorage(KMonoBehaviour kMonoBehaviour)
        {
            if (kMonoBehaviour.TryGetComponent<SweepBotStation>(out var sweepBotStation)
                && sweepBotStation.sweepStorage != null)
            {
                FixSweepBotStationStorage(sweepBotStation.sweepStorage);
            }
        }

        [HarmonyPatch(typeof(SweepBotStation), nameof(SweepBotStation.OnStorageChanged))]
        private static class SweepBotStation_OnStorageChanged
        {
            private static void Postfix(Storage ___sweepStorage)
            {
                FixSweepBotStationStorage(___sweepStorage);
            }
        }

        [HarmonyPatch(typeof(SweepBotStation), nameof(SweepBotStation.OnSpawn))]
        private static class SweepBotStation_OnSpawn
        {
            private static void Postfix(Storage ___sweepStorage)
            {
                FixSweepBotStationStorage(___sweepStorage);
            }
        }

        // домег бомжега. галку нужно не показывать до поры. кнопку копировать настройки тоже.
        [HarmonyPatch(typeof(CopyBuildingSettings), nameof(CopyBuildingSettings.OnRefreshUserMenu))]
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

        // чайник, боттлеры, ручной насос
        // убираем AnimOverrides если воду берёт рука
        [HarmonyPatch(typeof(Workable), nameof(Workable.GetAnim))]
        private static class Workable_GetAnim
        {
            private static bool Prepare() => ModOptions.Instance.AllowTransferArmPickupGasLiquid;
            private static void Postfix(WorkerBase worker, ref Workable.AnimInfo __result)
            {
                if (!worker.UsesMultiTool())
                    __result.overrideAnims = null;
            }
        }

        // ящег-збрасыватель
        // пофиксим ручное переключение вкл/выкл
        [HarmonyPatch(typeof(ObjectDispenser), nameof(ObjectDispenser.Toggle))]
        internal static class AutomaticDispenser_Toggle
        {
            private static void Postfix(ObjectDispenser.Instance ___smi, bool ___switchedOn)
            {
                ___smi.SetSwitchState(___switchedOn);
            }
        }
        #endregion

        #region smartmode
        // "умный режим" - разрешить доставку дупликам только если рука не может это сделать за некоторый таймаут
        // "резервируем" чоры доставки в зоне досягаемости руки
        // todo: гипотетически это можно переписать через пачти к существующим прекондициям. нужно ли ?
        [HarmonyPatch]
        private static class FetchChore_Constructor
        {
            private static bool Prepare() => ModOptions.Instance.HoldMode.Chores;

            // если свежесозданная чора в зоне досягаемости руки - зарезервируем на короткое время
            private static MethodBase TargetMethod() => typeof(FetchChore).GetConstructors()[0];

            [HarmonyPriority(Priority.Low)]
            private static void Postfix(FetchChore __instance)
            {
                if (__instance.automatable is Automatable2)
                {
                    var holder = new AutomatableHolder();
                    if (__instance.destination != null && TransferArmGroupProber.Get().IsReachable(Grid.PosToCell(__instance.destination)))
                        holder.SetShortTimeout();
                    __instance.AddPrecondition(IsAllowedByAutomationHoldEarly, holder);
                    __instance.AddPrecondition(IsAllowedByAutomationHoldLater, holder);
                }
            }

            // если чора в зоне досягаемости руки но рука выполняет другую чору - зарезервируем
            // должно запускаться до ChorePreconditions.IsMoreSatisfyingEarly
            private static Chore.Precondition IsAllowedByAutomationHoldEarly = new()
            {
                id = nameof(IsAllowedByAutomationHoldEarly),
                description = global::STRINGS.DUPLICANTS.CHORES.PRECONDITIONS.IS_ALLOWED_BY_AUTOMATION,
                sortOrder = -3,
                canExecuteOnAnyThread = true,
                fn = delegate (ref Chore.Precondition.Context context, object data)
                {
                    if (context.consumerState.hasSolidTransferArm && context.consumerState.choreDriver.HasChore())
                    {
                        var automatable = ((FetchChore)context.chore).automatable as Automatable2;
                        if (automatable != null)
                            ((AutomatableHolder)data).SetLongTimeout();
                    }
                    return true;
                }
            };

            // если кусок для переноски найден и:
            //  - это рука - рука может это сделать, продлим таймаут
            //  - это не рука - запрещаем дупликам если таймаут не истёк
            // должно запускаться после FetchChore.IsFetchTargetAvailable
            private static Chore.Precondition IsAllowedByAutomationHoldLater = new()
            {
                id = nameof(IsAllowedByAutomationHoldLater),
                description = global::STRINGS.DUPLICANTS.CHORES.PRECONDITIONS.IS_ALLOWED_BY_AUTOMATION,
                sortOrder = 2,
                canExecuteOnAnyThread = false,
                fn = delegate (ref Chore.Precondition.Context context, object data)
                {
                    var automatable = ((FetchChore)context.chore).automatable as Automatable2;
                    if (automatable == null)
                        return true;
                    if (context.consumerState.hasSolidTransferArm)
                    {
                        if (context.data as Pickupable != null)
                            ((AutomatableHolder)data).RefreshTimestamp();
                        return true;
                    }
                    else
                        return !automatable.GetAutomationHold() || ((AutomatableHolder)data).IsTimeOut();
                }
            };
        }

        // "резервируем" предметы на полу в зоне досягаемости руки, если она может их доставить
        // если до было !HasChore а после стало HasChore, значит произошел полный поиск чоры без скипа через IsMoreSatisfying
        // и мы можем полагаться на GetSuceededPreconditionContexts как список возможных чор
        [HarmonyPatch(typeof(SolidTransferArm), nameof(SolidTransferArm.Sim))]
        private static class SolidTransferArm_Sim
        {
            private static bool Prepare() => ModOptions.Instance.HoldMode.Items;

            private static void Prefix(SolidTransferArm __instance, ref bool __state)
            {
                __state = !__instance.choreDriver.HasChore();
            }

            private static void Postfix(SolidTransferArm __instance, bool __state)
            {
                if (__state && __instance.choreDriver.HasChore())
                {
                    // todo: цыклы можно оптимизировать
                    var contexts = __instance.choreConsumer.GetSuceededPreconditionContexts();
                    for (int i = 0; i < contexts.Count; i++)
                    {
                        var chore = contexts[i].chore as FetchChore;
                        if (chore != null && chore.destination != null)
                        {
                            for (int j = 0; j < __instance.pickupables.Count; j++)
                            {
                                var pickupable = __instance.pickupables[j];
                                if (pickupable != null && pickupable.storage == null
                                    && FetchManager.IsFetchablePickup(pickupable, chore, chore.destination)
                                    && TryGetHolder(pickupable, out var holder))
                                {
                                    holder.RefreshTimestamp();
                                }
                            }
                        }
                    }
                }
            }
        }

        // для отслеживания областей досягаемости рук
        [HarmonyPatch(typeof(SolidTransferArm), nameof(SolidTransferArm.AsyncUpdate))]
        private static class SolidTransferArm_AsyncUpdate
        {
            private static bool Prepare() => ModOptions.Instance.HoldMode.Enabled;
            /*
                MinionGroupProber.Get().Occupy(list1);
            +++ TransferArmGroupProber.Get().Occupy(list1);
                MinionGroupProber.Get().Vacate(list2);
            +++ TransferArmGroupProber.Get().Vacate(list2);
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                return instructions.Transpile(method, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var occupy = typeof(MinionGroupProber).GetMethodSafe(nameof(MinionGroupProber.Occupy), false, typeof(List<int>));
                var vacate = typeof(MinionGroupProber).GetMethodSafe(nameof(MinionGroupProber.Vacate), false, typeof(List<int>));
                var get = typeof(TransferArmGroupProber).GetMethodSafe(nameof(TransferArmGroupProber.Get), true);
                if (occupy != null && vacate != null && get != null)
                {
                    int i = instructions.FindIndex(inst => inst.Calls(occupy));
                    int j = instructions.FindIndex(inst => inst.Calls(vacate));
                    if (i == -1 || j == -1)
                        return false;
                    // в обратном порядке
                    instructions.Insert(j + 1, instructions[j].Clone());
                    instructions.Insert(j + 1, instructions[j - 1].Clone());
                    instructions.Insert(j + 1, new CodeInstruction(OpCodes.Call, get));
                    instructions.Insert(i + 1, instructions[i].Clone());
                    instructions.Insert(i + 1, instructions[i - 1].Clone());
                    instructions.Insert(i + 1, new CodeInstruction(OpCodes.Call, get));
                    return true;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(SolidTransferArm), nameof(SolidTransferArm.OnCleanUp))]
        private static class SolidTransferArm_OnCleanUp
        {
            private static bool Prepare() => ModOptions.Instance.HoldMode.Enabled;
            private static void Prefix(HashSet<int> ___reachableCells)
            {
                TransferArmGroupProber.Get().Vacate(___reachableCells.ToList());
            }
        }

        // присоседимся к FetchableMonitor чтобы дёргать из Pickupable без GetComponent
        private static FetchableMonitor.ObjectParameter<AutomatableHolder> PickupableHolder;

        [HarmonyPatch(typeof(FetchableMonitor), nameof(FetchableMonitor.InitializeStates))]
        private static class FetchableMonitor_InitializeStates
        {
            private static bool Prepare() => ModOptions.Instance.HoldMode.Enabled;
            private static void Postfix(FetchableMonitor __instance)
            {
                PickupableHolder = __instance.AddParameter(nameof(AutomatableHolder), new FetchableMonitor.ObjectParameter<AutomatableHolder>());
                __instance.root.EventHandler(GameHashes.OnStore, OnStore);
            }
            private static void OnStore(FetchableMonitor.Instance smi)
            {
                if (smi.pickupable.storage != null)
                    PickupableHolder.Get(smi)?.SetZeroTimeout();
            }
        }

        private static bool TryGetHolder(Pickupable pickupable, out AutomatableHolder holder)
        {
            if (pickupable != null && pickupable.fetchable_monitor != null)
            {
                holder = PickupableHolder.Get(pickupable.fetchable_monitor);
                if (holder == null)
                {
                    holder = new();
                    PickupableHolder.Set(holder, pickupable.fetchable_monitor);
                    // при первом обращении, а это скорее всего сразу после спавна
                    // если кусок в зоне досягаемости руки - зарезервируем ненадолго
                    if (pickupable.storage == null && TransferArmGroupProber.Get().IsReachable(pickupable.cachedCell))
                        holder.SetShortTimeout();
                }
                return true;
            }
            else
            {
                holder = null;
                return false;
            }
        }
        #endregion
    }
}
