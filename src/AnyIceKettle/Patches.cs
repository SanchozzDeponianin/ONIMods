using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using STRINGS;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace AnyIceKettle
{
    internal class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            ModOptions.Reload();
        }

        internal static Type PipedEverythingConsumerS;
        internal static Type PipedEverythingDispenser;

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
            PipedEverythingConsumerS = PPatchTools.GetTypeSafe("PipedEverything.ConduitConsumerOptionalSolid", "PipedEverything");
            PipedEverythingDispenser = PPatchTools.GetTypeSafe("PipedEverything.ConduitDispenserOptional", "PipedEverything");
        }

        // заменяем тоолтип
        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            var status = Db.Get().BuildingStatusItems.KettleMelting;
            const string pattern = @"\w*";
            var text = new Regex(UI.FormatAsLink(pattern, "ICE"), RegexOptions.Multiline).Replace(status.tooltipText, "{1}");
            text = new Regex(UI.FormatAsLink(pattern, "WATER"), RegexOptions.Multiline).Replace(text, "{2}");
            status.tooltipText = text;
            var originCB = status.resolveTooltipCallback ?? status.resolveStringCallback;
            status.resolveTooltipCallback = (str, data) =>
            {
                var ice = ((IceKettle.Instance)data).elementToMelt;
                str = str.Replace("{1}", ice.tag.ProperName()).Replace("{2}", ice.highTempTransition.tag.ProperName());
                return originCB(str, data);
            };
        }

        private static Dictionary<Tag, float> FuelWeights = new();

        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            var go = Assets.GetBuildingDef(IceKettleConfig.ID).BuildingComplete;
            go.AddOrGet<AnyIceKettle>();
            if (FuelWeights.Count == 0)
            {
                foreach (var tag in GameTags.BasicWoods)
                    FuelWeights[tag] = 1f;
            }
            var filterable = go.AddOrGet<TreeFilterable>();
            filterable.dropIncorrectOnFilterChange = true;
            filterable.preventAutoAddOnDiscovery = true;
            filterable.filterByStorageCategoriesOnSpawn = false;
            filterable.autoSelectStoredOnLoad = false;
            filterable.copySettingsEnabled = false;
            filterable.uiHeight = TreeFilterable.UISideScreenHeight.Short;
            var flat = go.AddOrGet<FlatTagFilterable>();
            flat.headerText = UI.METERS.FUEL.TOOLTIP;
            flat.tagOptions.AddRange(FuelWeights.Keys);
            flat.selectedTags.AddRange(GameTags.BasicWoods);
            go.AddOrGet<AnyFuelKettle>().discoverResourcesOnSpawn = GameTags.BasicWoods.ToList();
            go.AddOrGetDef<IceKettle.Def>().fuelElementTag = GameTags.Solid;
            go.AddOrGet<CopyBuildingSettings>();
        }

        // вытаскиваем все что можно в уголь в печке
        [HarmonyPatch(typeof(KilnConfig), nameof(KilnConfig.ConfigureRecipes))]
        private static class KilnConfig_ConfigureRecipes
        {
            [HarmonyPriority(Priority.High)]
            private static void Postfix()
            {
                var carbon = SimHashes.RefinedCarbon.CreateTag();
                var recipe = ComplexRecipeManager.Get().preProcessRecipes.FirstOrDefault(recipe => recipe.fabricators.Contains(KilnConfig.ID)
                    && recipe.results.Length > 0 && recipe.results[0].material == carbon && recipe.ingredients.Length > 0
                    && recipe.ingredients[0].possibleMaterials != null && recipe.ingredients[0].possibleMaterialAmounts != null
                    && recipe.ingredients[0].possibleMaterials.Length == recipe.ingredients[0].possibleMaterialAmounts.Length);
                if (recipe != null)
                {
                    int w = Array.IndexOf(recipe.ingredients[0].possibleMaterials, WoodLogConfig.TAG);
                    if (w != -1)
                    {
                        float wood = recipe.ingredients[0].possibleMaterialAmounts[w];
                        FuelWeights[carbon] = recipe.results[0].amount / wood;
                        for (int i = 0; i < recipe.ingredients[0].possibleMaterials.Length; i++)
                            FuelWeights[recipe.ingredients[0].possibleMaterials[i]] = recipe.ingredients[0].possibleMaterialAmounts[i] / wood;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(IceKettle), nameof(IceKettle.InitializeStates))]
        private static class IceKettle_InitializeStates
        {
            private static void Postfix(IceKettle __instance)
            {
                // проверка перед плавлением, на случай если лёд или топливо были сброшены
                __instance.operational.melting.working
                    .EnterTransition(__instance.operational.melting.exit, smi => !IceKettle.CanMeltNextBatch(smi));
                // сбросим воду, если выбор сменился во время наливания
                __instance.inUse.Exit(smi => smi.Get<AnyIceKettle>().DropExcessLiquid());
            }
        }

        // доступное топливо с учётом его веса
        [HarmonyPatch(typeof(IceKettle.Instance), nameof(IceKettle.Instance.FuelUnitsAvailable), MethodType.Getter)]
        private static class IceKettle_Instance_FuelUnitsAvailable
        {
            private static bool Prefix(IceKettle.Instance __instance, ref float __result)
            {
                __result = 0f;
                for (int i = 0; i < __instance.fuelStorage.Count; i++)
                {
                    var go = __instance.fuelStorage[i];
                    if (go != null && go.TryGetComponent(out PrimaryElement pe) && FuelWeights.ContainsKey(pe.Element.tag))
                        __result += pe.Mass / FuelWeights[pe.Element.tag];
                }
                __result = Mathf.RoundToInt(__result * 1000f) / 1000f; // like Storage.MassStored()
                return false;
            }
        }

        // проще всё переписать
        [HarmonyPatch(typeof(IceKettle.Instance), nameof(IceKettle.Instance.MeltNextBatch))]
        private static class IceKettle_Instance_MeltNextBatch
        {
            private static bool Prefix(IceKettle.Instance __instance)
            {
                MeltNextBatch(__instance);
                return false;
            }

            private static void MeltNextBatch(IceKettle.Instance smi)
            {
                if (!IceKettle.CanMeltNextBatch(smi))
                    return;
                smi.kettleStorage.FindFirst(smi.elementToMelt.tag).TryGetComponent(out PrimaryElement ice_pe);
                float fuel_amount = Mathf.Min(smi.FuelUnitsAvailable,
                    smi.GetUnitsOfFuelRequiredToMelt(smi.elementToMelt, smi.def.KGToMeltPerBatch, ice_pe.Temperature));
                smi.kettleStorage.ConsumeAndGetDisease(smi.elementToMelt.id.CreateTag(), smi.def.KGToMeltPerBatch,
                    out float ice_mass, out var disease_info, out _);
                smi.outputStorage.AddElement(smi.elementToMelt.highTempTransitionTarget, ice_mass, smi.def.TargetTemperature,
                    disease_info.idx, disease_info.count);
                ConsumeFuelWithWeight(smi.fuelStorage, fuel_amount, out float fuel_mass_consumed, out float temperature);
                float exhaust_mass = fuel_mass_consumed * smi.def.ExhaustMassPerUnitOfLumber;
                var exhaust = ElementLoader.FindElementByHash(smi.def.exhaust_tag);
                SimMessages.AddRemoveSubstance(Grid.PosToCell(smi), exhaust.id, null, exhaust_mass, temperature, byte.MaxValue, 0);
            }

            // поглощение топлива с учётом веса
            private static void ConsumeFuelWithWeight(Storage storage, float fuel_amount, out float mass_consumed, out float aggregate_temperature)
            {
                mass_consumed = 0f;
                aggregate_temperature = 0f;
                int i = 0;
                while (i < storage.Count && fuel_amount > 0f)
                {
                    var go = storage[i];
                    if (go != null && go.TryGetComponent(out PrimaryElement pe) && FuelWeights.ContainsKey(pe.Element.tag))
                    {
                        float weight = FuelWeights[pe.Element.tag];
                        storage.ConsumeAndGetDisease(pe.Element.tag, fuel_amount * weight, out float consumed, out _, out float temperature);
                        aggregate_temperature = Klei.SimUtil.CalculateFinalTemperature(mass_consumed, aggregate_temperature, consumed, temperature);
                        mass_consumed += consumed;
                        fuel_amount -= consumed / weight;
                    }
                    i++;
                }
            }
        }

        // корректируем прибитый гвоздями IsFunctional
        [HarmonyPatch(typeof(FilteredStorage), nameof(FilteredStorage.IsFunctional))]
        private static class FilteredStorage_IsFunctional
        {
            private static bool Prefix(FilteredStorage __instance, ref bool __result)
            {
                if (__instance.root is AnyFuelKettle kettle && !kettle.IsNullOrDestroyed())
                {
                    __result = kettle.IsOperational;
                    return false;
                }
                return true;
            }
        }

        // корректируем прибитый гвоздями SideScreen
        [HarmonyPatch(typeof(FlatTagFilterSideScreen), nameof(FlatTagFilterSideScreen.GetSideScreenSortOrder))]
        private static class FlatTagFilterSideScreen_GetSideScreenSortOrder
        {
            private static void Postfix(FlatTagFilterSideScreen __instance, ref int __result)
            {
                if (!__instance.tagFilterable.IsNullOrDestroyed() && __instance.tagFilterable.IsPrefabID(IceKettleConfig.ID))
                    __result = -5;
            }
        }
    }
}
