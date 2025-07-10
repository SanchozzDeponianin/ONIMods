using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using STRINGS;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;

namespace AnyIceMachine
{
    internal class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
        }

        public static Dictionary<Tag, Tag> ELEMENT_OPTIONS = new()
        {
            { SimHashes.Ice.CreateTag(), SimHashes.Water.CreateTag() },
            { SimHashes.Snow.CreateTag(), SimHashes.Water.CreateTag() },
            { SimHashes.DirtyIce.CreateTag(), SimHashes.DirtyWater.CreateTag() },
            { SimHashes.BrineIce.CreateTag(), SimHashes.Brine.CreateTag() },
            { SimHashes.Brine.CreateTag(), SimHashes.SaltWater.CreateTag() },
            { SimHashes.MilkIce.CreateTag(), SimHashes.Milk.CreateTag() },
            { SimHashes.Sucrose.CreateTag(), SimHashes.SugarWater.CreateTag() },
        };

        [HarmonyPatch(typeof(IceMachineConfig), nameof(IceMachineConfig.ConfigureBuildingTemplate))]
        private static class IceMachineConfig_ConfigureBuildingTemplate
        {
            private static void Postfix(GameObject go)
            {
                // строки тоолтипов для вариантов
                const string pattern = @"\w*";
                string text = Strings.Get("STRINGS.BUILDINGS.PREFABS.ICEMACHINE.OPTION_TOOLTIPS.ICE");
                text = new Regex(UI.FormatAsLink(pattern, "ICE"), RegexOptions.Multiline).Replace(text, "{0}");
                text = new Regex(UI.FormatAsLink(pattern, "WATER"), RegexOptions.Multiline).Replace(text, "{1}");
                foreach (var tag in ELEMENT_OPTIONS.Keys.ToArray())
                {
                    if (!IceMachineConfig.ELEMENT_OPTIONS.Contains(tag))
                    {
                        Strings.Add("STRINGS.BUILDINGS.PREFABS.ICEMACHINE.OPTION_TOOLTIPS." + tag.ToString().ToUpperInvariant(),
                            string.Format(text, ElementLoader.GetElement(tag).name, ElementLoader.GetElement(ELEMENT_OPTIONS[tag]).name));
                    }
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

        // todo: следующие две скопипизжены. нужно поглядывать если поменяют
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
                        GameUtil.DeltaThermalEnergy(item.GetComponent<PrimaryElement>(), -delta, smi.master.targetTemperature);
                    }
                    var target_element = __instance.targetProductionElement == SimHashes.Sucrose ? SimHashes.Ice : __instance.targetProductionElement;
                    for (int i = __instance.waterStorage.items.Count; i > 0; i--)
                    {
                        var item = __instance.waterStorage.items[i - 1];
                        if (item != null && item.TryGetComponent(out PrimaryElement pe) && pe.Temperature < pe.Element.lowTemp)
                        {
                            __instance.waterStorage.AddOre(target_element,
                                pe.Mass * (1f - pe.Element.lowTempTransitionOreMassConversion), pe.Temperature, pe.DiseaseIdx, pe.DiseaseCount);
                            if (pe.Element.lowTempTransitionOreID != SimHashes.Vacuum)
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
