using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using STRINGS;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace AnyIceMachine
{
    internal class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
        }

        internal static Type PipedEverythingConsumer;
        internal static Type PipedEverythingDispenser;
        internal static Type PipedEverythingDispenserS;
        internal static Func<Component, bool> IsConnected;

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
            PipedEverythingConsumer = PPatchTools.GetTypeSafe("PipedEverything.ConduitConsumerOptional", "PipedEverything");
            PipedEverythingDispenser = PPatchTools.GetTypeSafe("PipedEverything.ConduitDispenserOptional", "PipedEverything");
            PipedEverythingDispenserS = PPatchTools.GetTypeSafe("PipedEverything.ConduitDispenserOptionalSolid", "PipedEverything");
            var api = PPatchTools.GetTypeSafe("PipedEverything.PipedEverythingState", "PipedEverything");
            try
            {
                if (PipedEverythingConsumer != null)
                {
                    IsConnected = Unsafe.As<Func<Component, bool>>(PipedEverythingConsumer.GetMethodSafe("get_IsConnected", false)
                        ?.CreateDelegate(typeof(Func<,>).MakeGenericType(PipedEverythingConsumer, typeof(bool))));
                }
                // id, is_input, x, y, filter, color = null, storageIndex, storageCapacity
                api?.Detour<Action<string, bool, int, int, string[], Color32?, int?, float?>>("AddConfig")
                    ?.Invoke(IceMachineConfig.ID, false, 0, 1, new[] { "Liquid" }, null, 1, 0f);
            }
            catch (Exception e)
            {
                PUtil.LogExcWarn(e);
            }
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame() => ModOptions.Reload();

        public class OptionInfo
        {
            public Tag ingrigient, result, icon;
            public string title, tooltip;
            public OptionInfo(Tag ingrigient)
            {
                this.ingrigient = ingrigient;
                result = Tag.Invalid;
                icon = Tag.Invalid;
            }
            public OptionInfo(Tag ingrigient, Tag result_override, Tag icon_override)
            {
                this.ingrigient = ingrigient;
                result = result_override;
                icon = icon_override;
            }
        }

        // тэг опции должен быть уникален, однако результаты пересекаются (сахар из нектара и латекса)
        // todo: упростить когда У58 всё
        public static Dictionary<Tag, OptionInfo> ELEMENT_OPTIONS = new()
        {
            { SimHashes.Ice.CreateTag(), new(SimHashes.Water.CreateTag()) },
            { SimHashes.Snow.CreateTag(), new(SimHashes.Water.CreateTag(), SimHashes.Snow.CreateTag(), SimHashes.Snow.CreateTag()) },
            { SimHashes.DirtyIce.CreateTag(), new(SimHashes.DirtyWater.CreateTag()) },
            { SimHashes.BrineIce.CreateTag(), new(SimHashes.Brine.CreateTag()) },
            { SimHashes.Brine.CreateTag(), new(SimHashes.SaltWater.CreateTag()) },
            //{ SimHashes.MurkyBrineIce.CreateTag(), new(SimHashes.MurkyBrine.CreateTag()) },
            { "MurkyBrineIce", new("MurkyBrine") },
            { SimHashes.MilkIce.CreateTag(), new(SimHashes.Milk.CreateTag()) },
            { SimHashes.SugarWater.CreateTag(), new(SimHashes.SugarWater.CreateTag(), Tag.Invalid, SimHashes.Sucrose.CreateTag()) },
            //{ SimHashes.Latex.CreateTag(), new(SimHashes.Latex.CreateTag(), Tag.Invalid, SimHashes.Sucrose.CreateTag()) },
            { "Latex", new("Latex", Tag.Invalid, SimHashes.Sucrose.CreateTag()) },
            { SimHashes.Tallow.CreateTag(), new(SimHashes.RefinedLipid.CreateTag()) },
        };

        [HarmonyPatch(typeof(IceMachineConfig), nameof(IceMachineConfig.ConfigureBuildingTemplate))]
        private static class IceMachineConfig_ConfigureBuildingTemplate
        {
            private static void Postfix(GameObject go)
            {
                // строки тоолтипов для вариантов
                const string pattern = @"\w*";
                string text = BUILDINGS.PREFABS.ICEMACHINE.OPTION_TOOLTIPS.ICE.text;
                text = new Regex(UI.FormatAsLink(pattern, "ICE"), RegexOptions.Multiline).Replace(text, "{0}");
                text = new Regex(UI.FormatAsLink(pattern, "WATER"), RegexOptions.Multiline).Replace(text, "{1}");
                foreach (var tag in ELEMENT_OPTIONS.Keys.ToArray())
                {
                    var info = ELEMENT_OPTIONS[tag];
                    var ingrigient = ElementLoader.GetElement(info.ingrigient);
                    if (ingrigient == null)
                        continue;
                    var result = info.result.IsValid ? ElementLoader.GetElement(info.result) : ingrigient.lowTempTransition;
                    string result_name = result.name;
                    if (ingrigient.lowTempTransitionOreMassConversion > 0)
                    {
                        result_name = result_name + STRINGS.UI.GAMEOBJECTEFFECTS.REQUIREMETS_AND
                            + ElementLoader.FindElementByHash(ingrigient.lowTempTransitionOreID).name;
                    }
                    info.title = result_name;
                    info.tooltip = string.Format(text, result_name, ingrigient.name);
                    info.result = result.tag;
                    info.icon = info.icon.IsValid ? info.icon : result.tag;
                }
                IceMachineConfig.ELEMENT_OPTIONS = ELEMENT_OPTIONS.Keys.ToArray();
                go.AddOrGet<DropAllWorkable>();
                go.AddOrGet<CopyBuildingSettings>();
            }
        }

        [HarmonyPatch(typeof(IceMachine), "OnSpawn")]
        private static class IceMachine_OnSpawn
        {
            private static void Postfix(IceMachine __instance)
            {
                __instance.Subscribe((int)GameHashes.CopySettings, __instance.OnCopySettings);
                __instance.SetChosenIce(__instance.targetProductionElement.CreateTag());
                __instance.SetPipedEverythingDispenser();
            }
        }

        [HarmonyPatch(typeof(IceMachine), nameof(IceMachine.OnOptionSelected))]
        private static class IceMachine_OnOptionSelected
        {
            private static void Prefix(IceMachine __instance, FewOptionSideScreen.IFewOptionSideScreen.Option option)
            {
                if (__instance.targetProductionElement.CreateTag() != option.tag)
                    __instance.SetChosenIce(option.tag);
            }
        }

        // todo: следующие три скопипизжены. нужно поглядывать если поменяют

        [HarmonyPatch(typeof(IceMachine), nameof(IceMachine.GetOptions))]
        private static class IceMachine_GetOptions
        {
            private static bool Prefix(IceMachine __instance, ref FewOptionSideScreen.IFewOptionSideScreen.Option[] __result)
            {
                bool show_all = ModOptions.Instance.show_all || (IsConnected != null
                    && __instance.TryGetComponent(PipedEverythingConsumer, out var consumer) && IsConnected(consumer));

                var list = ListPool<FewOptionSideScreen.IFewOptionSideScreen.Option, IceMachine>.Allocate();
                for (int i = 0; i < IceMachineConfig.ELEMENT_OPTIONS.Length; i++)
                {
                    var option = IceMachineConfig.ELEMENT_OPTIONS[i];
                    var info = ELEMENT_OPTIONS[option];
                    var ingrigient = ElementLoader.GetElement(info.ingrigient);
                    if (ingrigient == null)
                        continue;
                    if (show_all || ingrigient.tag == GameTags.Water || DiscoveredResources.Instance.IsDiscovered(ingrigient.tag))
                    {
                        list.Add(new FewOptionSideScreen.IFewOptionSideScreen.Option(option, info.title,
                            Def.GetUISprite(info.icon, "ui", false), info.tooltip));
                    }
                }
                __result = list.ToArray();
                list.Recycle();
                return false;
            }
        }

        // для старта проверять любую воду
        [HarmonyPatch(typeof(IceMachine), "CanMakeIce")]
        private static class IceMachine_CanMakeIce
        {
            private static bool Prefix(IceMachine __instance, ref bool __result)
            {
                bool flag = __instance.waterStorage != null && __instance.waterStorage.GetMassAvailable(GameTags.Liquid) >= 0.1f;
                bool flag2 = __instance.iceStorage != null && __instance.iceStorage.IsFull();
                __result = flag && !flag2;
                return false;
            }
        }

        // правильное замораживание с субпродуктами
        [HarmonyPatch(typeof(IceMachine), "MakeIce")]
        private static class IceMachine_MakeIce
        {
            private static bool Prefix(IceMachine __instance, IceMachine.StatesInstance smi, float dt)
            {
                if (__instance.waterStorage.items.Count > 0)
                {
                    float delta = __instance.heatRemovalRate * dt / __instance.waterStorage.items.Count;
                    foreach (var item in __instance.waterStorage.items)
                    {
                        if (item != null && item.TryGetComponent(out PrimaryElement pe) && pe.Mass > 0f)
                            GameUtil.DeltaThermalEnergy(pe, -delta, smi.master.targetTemperature);
                    }

                    // todo: поразмыслить об оптимизации
                    var target_element = SimHashes.Vacuum;
                    var selected = __instance.GetSelectedOption();
                    if (ELEMENT_OPTIONS.TryGetValue(selected, out var info) && info.result.IsValid)
                        target_element = ElementLoader.GetElement(info.result).id;

                    for (int i = __instance.waterStorage.items.Count; i > 0; i--)
                    {
                        var item = __instance.waterStorage.items[i - 1];
                        if (item != null && item.TryGetComponent(out PrimaryElement pe) && pe.Temperature < pe.Element.lowTemp)
                        {
                            var result = target_element != SimHashes.Vacuum ? target_element : pe.Element.lowTempTransitionTarget;
                            __instance.waterStorage.AddOre(result,
                                pe.Mass * (1f - pe.Element.lowTempTransitionOreMassConversion), pe.Temperature, pe.DiseaseIdx, pe.DiseaseCount);
                            if (pe.Element.lowTempTransitionOreMassConversion > 0)
                            {
                                __instance.waterStorage.AddOre(pe.Element.lowTempTransitionOreID,
                                pe.Mass * pe.Element.lowTempTransitionOreMassConversion, pe.Temperature, pe.DiseaseIdx, pe.DiseaseCount);
                            }
                            __instance.waterStorage.ConsumeIgnoringDisease(item);
                        }
                    }
                    smi.UpdateIceState();
                }
                else
                    smi.GoTo(smi.sm.on.waiting);
                return false;
            }
        }
    }
}
