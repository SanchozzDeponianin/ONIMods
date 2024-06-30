using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Klei.AI;
using TUNING;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace RoverRefueling
{
    internal sealed class RoverRefuelingPatches : KMod.UserMod2
    {
        public const string RefuelingEffectID = "ScoutBotRefueling";
        public static Effect RefuelingEffect;

        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(RoverRefuelingPatches));
            new POptions().RegisterOptions(this, typeof(RoverRefuelingOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            ModUtil.AddBuildingToPlanScreen(BUILD_CATEGORY.Utilities, RoverRefuelingStationConfig.ID, BUILD_SUBCATEGORY.automated, SweepBotStationConfig.ID);
            Utils.AddBuildingToTechnology("ArtificialFriends", RoverRefuelingStationConfig.ID);
            var db = Db.Get();
            var rate = ROBOTS.SCOUTBOT.BATTERY_CAPACITY / RoverRefuelingOptions.Instance.charge_time;
            var modifier = new AttributeModifier(
                attribute_id: db.Amounts.InternalChemicalBattery.deltaAttribute.Id,
                value: rate,
                description: STRINGS.DUPLICANTS.MODIFIERS.SCOUTBOTREFUELING.NAME);
            RefuelingEffect = new Effect(
                id: RefuelingEffectID,
                name: STRINGS.DUPLICANTS.MODIFIERS.SCOUTBOTREFUELING.NAME,
                description: STRINGS.DUPLICANTS.MODIFIERS.SCOUTBOTREFUELING.TOOLTIP,
                duration: 0,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false);
            RefuelingEffect.Add(modifier);
            db.effects.Add(RefuelingEffect);
        }

        public static readonly Tag RoverNeedRefueling = TagManager.Create(nameof(RoverNeedRefueling));

        [HarmonyPatch(typeof(RobotBatteryMonitor), nameof(RobotBatteryMonitor.InitializeStates))]
        private static class RobotBatteryMonitor_InitializeStates
        {
            private static void Postfix(RobotBatteryMonitor __instance)
            {
                __instance.drainingStates.lowBattery.ToggleTag(RoverNeedRefueling);
                __instance.needsRechargeStates.lowBattery.ToggleTag(RoverNeedRefueling);
            }
        }

        // так как батарея ровера теперь заряжается, нужно показывать другой статусытем
        // но мы не будем трогать RobotBatteryMonitor.Def.canCharge чтобы не поломать его другую логику
        // вместо этого найдём и пропатчим сгенерированые делегатовые методы
        // благо конструкция встречается пару раз и с одинаковой начинкой
        // .ToggleStatusItem(smi => smi.def.canCharge ? Db.Get().RobotStatusItems.LowBattery : Db.Get().RobotStatusItems.LowBatteryNoCharge
        [HarmonyPatch]
        private static class RobotBatteryMonitor_InitializeStates_ToggleStatusItem
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (var type in typeof(RobotBatteryMonitor).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (type.IsDefined(typeof(CompilerGeneratedAttribute)))
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            var parameters = method.GetParameters();
                            if (method.ReturnType == typeof(StatusItem)
                                && parameters.Length == 1 && parameters[0].ParameterType == typeof(RobotBatteryMonitor.Instance))
                            {
                                yield return method;
                            }
                        }
                    }
                }
            }

            private static void Postfix(RobotBatteryMonitor.Instance smi, ref StatusItem __result)
            {
                if (smi.gameObject.PrefabID() == ScoutRoverConfig.ID)
                    __result = Db.Get().RobotStatusItems.LowBattery;
            }
        }

        // всять запас топлива и сбросить вместе с ровером
        [HarmonyPatch(typeof(ScoutModuleConfig), nameof(ScoutModuleConfig.DoPostConfigureComplete))]
        private static class ScoutModuleConfig_DoPostConfigureComplete
        {
            private static bool Prepare() => RoverRefuelingOptions.Instance.fuel_cargo_bay_enable;
            private static void Postfix(GameObject go)
            {
                float capacity = RoverRefuelingStationConfig.NUM_USES * RoverRefuelingOptions.Instance.fuel_mass_per_charge;
                var storage = go.AddOrGet<Storage>();
                storage.capacityKg = capacity;
                var fuels = Assets.GetPrefabsWithTag(RoverRefuelingStationConfig.fuelTag).Select(prefab => prefab.PrefabID());
                var filterable = go.AddOrGet<FlatTagFilterable>();
                filterable.headerText = STRINGS.BUILDINGS.PREFABS.ROVERREFUELINGSTATION.UI_FILTER_CATEGORY;
                filterable.tagOptions.AddRange(fuels);
                filterable.selectedTags.AddRange(fuels);
                var treeFilterable = go.AddOrGet<TreeFilterable>();
                treeFilterable.dropIncorrectOnFilterChange = false;
                treeFilterable.autoSelectStoredOnLoad = false;
                treeFilterable.uiHeight = TreeFilterable.UISideScreenHeight.Short;
                var cargoBay = go.AddOrGet<RoverFuelCargoBay>();
                cargoBay.fuelTag = RoverRefuelingStationConfig.fuelTag;
                cargoBay.discoverResourcesOnSpawn = fuels.ToList();
                cargoBay.storage = storage;
                cargoBay.storageType = CargoBay.CargoType.Liquids;
                go.AddOrGet<CopyBuildingSettings>();
                go.AddOrGet<DropToUserCapacity>();
            }
        }

        // при изменении максимума запаса топлива - выбрасывать только топливо
        // не выбрасывать руду, ровера и посадочный модуль
        [HarmonyPatch(typeof(DropToUserCapacity), "OnCompleteWork")]
        private static class DropToUserCapacity_OnCompleteWork
        {
            private static bool Prepare() => RoverRefuelingOptions.Instance.fuel_cargo_bay_enable;
            private static bool Prefix(DropToUserCapacity __instance, ref Chore ___chore)
            {
                if (__instance.TryGetComponent<RoverFuelCargoBay>(out var cargoBay))
                {
                    cargoBay.DropExcess();
                    ___chore = null;
                    return false;
                }
                else return true;
            }
        }

        // заправка из ракетных погрузчиков
        // скопипащено и причесано из LaunchPadMaterialDistributor.Instance.FillRocket
        private static IDetouredField<LaunchPadMaterialDistributor, LaunchPadMaterialDistributor.TargetParameter> attachedRocket
            = PDetours.DetourFieldLazy<LaunchPadMaterialDistributor, LaunchPadMaterialDistributor.TargetParameter>("attachedRocket");

        private static IDetouredField<LaunchPadMaterialDistributor, LaunchPadMaterialDistributor.BoolParameter> fillComplete = PDetours.DetourFieldLazy<LaunchPadMaterialDistributor, LaunchPadMaterialDistributor.BoolParameter>("fillComplete");

        [HarmonyPatch(typeof(LaunchPadMaterialDistributor.Instance), nameof(LaunchPadMaterialDistributor.Instance.FillRocket))]
        private static class LaunchPadMaterialDistributor_Instance_FillRocket
        {
            private static bool Prepare() => RoverRefuelingOptions.Instance.fuel_cargo_bay_enable;
            private static void Postfix(LaunchPadMaterialDistributor.Instance __instance)
            {
                var craftInterface = attachedRocket.Get(__instance.sm).Get<RocketModuleCluster>(__instance).CraftInterface;
                var cargos = ListPool<RoverFuelCargoBay, LaunchPadMaterialDistributor>.Allocate();
                foreach (var @ref in craftInterface.ClusterModules)
                {
                    if (@ref.Get().TryGetComponent<RoverFuelCargoBay>(out var cargo) && cargo.RemainingCapacity > 0)
                        cargos.Add(cargo);
                }
                var chainedBuildings = HashSetPool<ChainedBuilding.StatesInstance, ChainedBuilding.StatesInstance>.Allocate();
                __instance.GetSMI<ChainedBuilding.StatesInstance>().GetLinkedBuildings(ref chainedBuildings);
                bool filling = false;
                foreach (var building in chainedBuildings)
                {
                    var smi = building.GetSMI<ModularConduitPortController.Instance>();
                    if (smi != null)
                    {
                        bool loading = false;
                        var consumer = building.GetComponent<IConduitConsumer>();
                        if (consumer != null && (
                            smi.SelectedMode == ModularConduitPortController.Mode.Load ||
                            smi.SelectedMode == ModularConduitPortController.Mode.Both))
                        {
                            smi.SetRocket(true);
                            foreach (var cargo in cargos)
                            {
                                if (!cargo.FillEnable ||
                                    CargoBayConduit.ElementToCargoMap[consumer.ConduitType] != cargo.storageType)
                                    continue;
                                float remaining = cargo.RemainingCapacity;
                                if (remaining <= 0f)
                                    continue;
                                for (int i = consumer.Storage.items.Count - 1; i >= 0; i--)
                                {
                                    var go = consumer.Storage.items[i];
                                    float mass = consumer.Storage.MassStored();
                                    if (remaining > 0f && mass > 0f && cargo.TreeFilterable.AcceptedTags.Contains(go.PrefabID()))
                                    {
                                        loading = true;
                                        filling = true;
                                        var pickupable = go.GetComponent<Pickupable>().Take(remaining);
                                        if (pickupable != null)
                                        {
                                            cargo.storage.Store(pickupable.gameObject);
                                            remaining -= pickupable.PrimaryElement.Mass;
                                        }
                                    }
                                }
                            }
                            if (loading)
                                smi.SetLoading(loading);
                        }
                    }
                }
                chainedBuildings.Recycle();
                cargos.Recycle();
                if (filling)
                    fillComplete.Get(__instance.sm).Set(!filling, __instance);
            }
        }
    }
}
