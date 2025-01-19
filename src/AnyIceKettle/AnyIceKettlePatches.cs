using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
    internal class AnyIceKettlePatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(AnyIceKettlePatches));
            if (DlcManager.IsContentSubscribed(DlcManager.EXPANSION1_ID) || DlcManager.IsContentSubscribed(DlcManager.DLC3_ID))
                new POptions().RegisterOptions(this, typeof(AnyIceKettleOptions));
            AnyIceKettleOptions.Reload();
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
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
                var ice = AnyIceKettle.ElementToMelt.Get((IceKettle.Instance)data);
                str = str.Replace("{1}", ice.tag.ProperName()).Replace("{2}", ice.highTempTransition.tag.ProperName());
                return originCB(str, data);
            };
        }

        [HarmonyPatch(typeof(IceKettleConfig), nameof(IceKettleConfig.ConfigureBuildingTemplate))]
        private static class IceKettleConfig_ConfigureBuildingTemplate
        {
            private static void Postfix(GameObject go)
            {
                go.AddOrGet<AnyIceKettle>();
            }
        }

        [HarmonyPatch(typeof(IceKettle), nameof(IceKettle.InitializeStates))]
        private static class IceKettle_InitializeStates
        {
            private static void Postfix(IceKettle __instance)
            {
                // проверка перед плавлением, на случай если лёд был сброшен
                __instance.operational.melting.working
                    .EnterTransition(__instance.operational.melting.exit, smi => !IceKettle.HasEnoughSolidsToMelt(smi));
                // сбросим воду, если выбор сменился во время наливания
                __instance.inUse.Exit(smi => smi.Get<AnyIceKettle>().DropExcessLiquid());
            }
        }

        // заменить все smi.def.targetElementTag на smi.elementToMelt.tag
        [HarmonyPatch(typeof(IceKettle.Instance), nameof(IceKettle.Instance.MeltNextBatch))]
        private static class IceKettle_Instance_MeltNextBatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var def = typeof(IceKettle.GenericInstance).GetPropertySafe<IceKettle.Def>("def", false)?.GetGetMethod();
                var target_tag = typeof(IceKettle.Def).GetFieldSafe(nameof(IceKettle.Def.targetElementTag), false);
                var element_to_melt = typeof(IceKettle.Instance).GetFieldSafe("elementToMelt", false);
                var element_tag = typeof(Element).GetFieldSafe(nameof(Element.tag), false);
                bool found = false;
                if (def != null && target_tag != null && element_to_melt != null && element_tag != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].LoadsField(target_tag) && instructions[i - 1].Calls(def))
                        {
                            instructions[i - 1].opcode = OpCodes.Ldfld;
                            instructions[i - 1].operand = element_to_melt;
                            instructions[i].operand = element_tag;
                            found = true;
                        }
                    }
                }
                return found;
            }
        }
    }
}
