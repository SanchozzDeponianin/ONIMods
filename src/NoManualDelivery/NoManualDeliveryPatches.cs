using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;
using TUNING;
using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;

namespace NoManualDelivery
{
    internal static class NoManualDeliveryPatches
    {
        public static void OnLoad()
        {
            PUtil.InitLibrary();
            PUtil.RegisterPatchClass(typeof(NoManualDeliveryPatches));
            POptions.RegisterOptions(typeof(NoManualDeliveryOptions));

            NoManualDeliveryOptions.Reload();

            // хак для того чтобы разрешить руке хватать бутылки
            if (NoManualDeliveryOptions.Instance.AllowTransferArmPickupGasLiquid)
            {
                SolidTransferArm.tagBits = new TagBits(STORAGEFILTERS.NOT_EDIBLE_SOLIDS.Concat(STORAGEFILTERS.FOOD).Concat(STORAGEFILTERS.GASES).Concat(STORAGEFILTERS.LIQUIDS).ToArray());

                BuildingToMakeAutomatable.AddRange(BuildingToMakeAutomatableWithTransferArmPickupGasLiquid);
            }

            // подготовка хака, чтобы разрешить дупликам забирать жеготных из инкубатора и всегда хватать еду
            AlwaysCouldBePickedUpByMinionTags = new Tag[] { GameTags.Creatures.Deliverable };
            if (NoManualDeliveryOptions.Instance.AllowAlwaysPickupEdible)
            {
                AlwaysCouldBePickedUpByMinionTags = AlwaysCouldBePickedUpByMinionTags.Concat(STORAGEFILTERS.FOOD).Add(GameTags.MedicalSupplies).ToArray();
            }
        }

        [PLibMethod(RunAt.AfterModsLoad)]
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
            RationBoxConfig.ID,
            RefrigeratorConfig.ID,
            FarmTileConfig.ID,
            HydroponicFarmConfig.ID,
            PlanterBoxConfig.ID,
            CreatureFeederConfig.ID,
            FishFeederConfig.ID,
            EggIncubatorConfig.ID,
            KilnConfig.ID,
            IceCooledFanConfig.ID,
            SolidBoosterConfig.ID,
            ResearchCenterConfig.ID,
            SweepBotStationConfig.ID,
            // из ДЛЦ:
            "UraniumCentrifuge",
            "NuclearReactor",

            // из модов:
            // Aquatic Farm https://steamcommunity.com/sharedfiles/filedetails/?id=1910961538
            "AquaticFarm",
            // Advanced Refrigeration https://steamcommunity.com/sharedfiles/filedetails/?id=2021324045
            "SimpleFridge", "FridgeRed", "FridgeYellow", "FridgeBlue", "FridgeAdvanced",
            // Storage Pod  https://steamcommunity.com/sharedfiles/filedetails/?id=1873476551
            "StoragePodConfig",
            // Big Storage  https://steamcommunity.com/sharedfiles/filedetails/?id=1913589787
            "BigSolidStorage",
            "BigBeautifulStorage",
            // Trashcans    https://steamcommunity.com/sharedfiles/filedetails/?id=2037089892
            "SolidTrashcan",
        };

        private static List<string> BuildingToMakeAutomatableWithTransferArmPickupGasLiquid = new List<string>()
        {
            LiquidPumpingStationConfig.ID,
            GasBottlerConfig.ID,
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
        [HarmonyPatch(typeof(Assets), nameof(Assets.AddBuildingDef))]
        internal static class Assets_AddBuildingDef
        {
            private static void Prefix(BuildingDef def)
            {
                GameObject go = def.BuildingComplete;
                if (go != null)
                {
                    if (
                        BuildingToMakeAutomatable.Contains(def.PrefabID)
                        || (go.GetComponent<ManualDeliveryKG>() != null && (go.GetComponent<ElementConverter>() != null || go.GetComponent<EnergyGenerator>() != null) && go.GetComponent<ResearchCenter>() == null)
                        || (go.GetComponent<ComplexFabricatorWorkable>() != null)
                        || (go.GetComponent<TinkerStation>() != null)
                        )
                    {
                        go.AddOrGet<Automatable2>();
                    }
                }
            }
        }

        // хак для того чтобы разрешить дупликам доставку для Tinkerable объектов, типа генераторов
        [HarmonyPatch(typeof(Tinkerable), "UpdateChore")]
        internal static class Tinkerable_UpdateChore
        {
            private static readonly IDetouredField<Chore, bool> arePreconditionsDirty = PDetours.DetourField<Chore, bool>("arePreconditionsDirty");
            private static readonly IDetouredField<Chore, List<Chore.PreconditionInstance>> preconditions = PDetours.DetourField<Chore, List<Chore.PreconditionInstance>>("preconditions");

            private static void Postfix(Chore ___chore)
            {
                if (___chore != null && ___chore is FetchChore)
                {
                    string id = ChorePreconditions.instance.IsAllowedByAutomation.id;
                    /*
                    Traverse traverse = Traverse.Create(___chore);
                    traverse.Field<bool>("arePreconditionsDirty").Value = true;
                    traverse.Field<List<Chore.PreconditionInstance>>("preconditions").Value.RemoveAll((Chore.PreconditionInstance x) => x.id == id);
                    */
                    arePreconditionsDirty.Set(___chore, true);
                    preconditions.Get(___chore).RemoveAll((Chore.PreconditionInstance x) => x.id == id);
                    ((FetchChore)___chore).automatable = null;
                }
            }
        }

        private static Tag[] AlwaysCouldBePickedUpByMinionTags = new Tag[0];

        // хак для того чтобы разрешить дупликам забирать жеготных из инкубатора и всегда хватать еду
        [HarmonyPatch(typeof(Pickupable), nameof(Pickupable.CouldBePickedUpByMinion))]
        internal static class Pickupable_CouldBePickedUpByMinion
        {
            private static bool Prefix(Pickupable __instance, GameObject carrier, ref bool __result)
            {
                if (__instance.KPrefabID.HasAnyTags(AlwaysCouldBePickedUpByMinionTags))
                {
                    __result = __instance.CouldBePickedUpByTransferArm(carrier);
                    return false;
                }
                return true;
            }
        }

        // хак - дуплы обжирающиеся от стресса, будут игнорировать установленную галку
        [HarmonyPatch(typeof(BingeEatChore.StatesInstance), nameof(BingeEatChore.StatesInstance.FindFood))]
        internal static class BingeEatChore_StatesInstance_FindFood
        {
            /*
            if ( блаблабла && 
                item.GetComponent<Pickupable>()
        ---         .CouldBePickedUpByMinion(base.gameObject)
        +++         .CouldBePickedUpByTransferArm(base.gameObject)
               )
            {
                блаблабла
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;
                var CouldBePickedUpByMinion = typeof(Pickupable).GetMethod(nameof(Pickupable.CouldBePickedUpByMinion), new Type[] { typeof(GameObject) });
                var CouldBePickedUpByTransferArm = typeof(Pickupable).GetMethod(nameof(Pickupable.CouldBePickedUpByTransferArm), new Type[] { typeof(GameObject) });
                bool result = false;
                if (CouldBePickedUpByMinion != null && CouldBePickedUpByTransferArm != null)
                {
                    for (int i = 0; i < instructionsList.Count; i++)
                    {
                        var instruction = instructionsList[i];
                        if (instruction.opcode == OpCodes.Callvirt && (instruction.operand is MethodInfo info) && info == CouldBePickedUpByMinion)
                        {
                            instructionsList[i] = new CodeInstruction(OpCodes.Callvirt, CouldBePickedUpByTransferArm);
                            result = true;
#if DEBUG
                        PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                            break;
                        }
                    }
                }
                if (!result)
                {
                    PUtil.LogWarning($"{ Utils.modInfo.assemblyName}: Could not apply apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }
        }

        // хак для того чтобы не испортить заголовок окна - сместить галку вниз
        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        internal static class DetailsScreen_OnPrefabInit
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

        // хаки для станции бота.
        // при изменении хранилища станция принудительно помечает все ресурсы "для переноски"
        // изза этого дуплы всегда будут игнорировать установленную галку (другая задача переноски, не учитывает галку вообще)
        // поэтому нужно при установленной галке - отменить пометки "для переноски"

        private static readonly IDetouredField<SweepBotStation, Storage> SWEEP_STORAGE = PDetours.DetourField<SweepBotStation, Storage>("sweepStorage");

        private static void FixSweepBotStationStorage(Storage sweepStorage)
        {
            bool automationOnly = (sweepStorage?.automatable?.GetAutomationOnly()) ?? false;
            for (int i = 0; i < sweepStorage.Count; i++)
            {
                var clearable = sweepStorage[i].GetComponent<Clearable>();
                if (automationOnly)
                    clearable.CancelClearing();
                else
                    clearable.MarkForClear(false, true);
            }
        }

        private static void UpdateSweepBotStationStorage(KMonoBehaviour kMonoBehaviour)
        {
            var sweepBotStation = kMonoBehaviour.GetComponent<SweepBotStation>();
            if (sweepBotStation != null)
            {
                var storage = SWEEP_STORAGE.Get(sweepBotStation);
                if (storage != null)
                    FixSweepBotStationStorage(storage);
            }
        }

        [HarmonyPatch(typeof(SweepBotStation), "OnStorageChanged")]
        internal static class SweepBotStation_OnStorageChanged
        {
            private static void Postfix(Storage ___sweepStorage)
            {
                FixSweepBotStationStorage(___sweepStorage);
            }
        }

        // нужно обновить хранилище при загрузке, и при изменении галки
        [HarmonyPatch(typeof(SweepBotStation), "OnSpawn")]
        internal static class SweepBotStation_OnSpawn
        {
            private static void Postfix(Storage ___sweepStorage)
            {
                FixSweepBotStationStorage(___sweepStorage);
            }
        }

        [HarmonyPatch(typeof(Automatable), "OnCopySettings")]
        internal static class Automatable_OnCopySettings
        {
            private static void Postfix(Automatable __instance)
            {
                UpdateSweepBotStationStorage(__instance);
            }
        }

        [HarmonyPatch(typeof(AutomatableSideScreen), "OnAllowManualChanged")]
        internal static class AutomatableSideScreen_OnAllowManualChanged
        {
            private static void Postfix(Automatable ___targetAutomatable)
            {
                UpdateSweepBotStationStorage(___targetAutomatable);
            }
        }
    }

    // нужно, чтобы установить галку по умолчанию
    public class Automatable2 : Automatable
    {
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            SetAutomationOnly(false);
        }
    }
}
