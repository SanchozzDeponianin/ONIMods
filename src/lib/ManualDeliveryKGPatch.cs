using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using STRINGS;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;

namespace SanchozzONIMods.Shared
{
    // пачти для ManualDeliveryKG
    // копирование настроек - вкл/выкл ручную доставку
    // исправление тоолтипа для этой кнопки, ушоб было видно доставку чего отключаем.
    public static class ManualDeliveryKGPatch
    {
        private static readonly Action<object, object> OnCopySettingsDelegate = (cmp, data) => ((ManualDeliveryKG)cmp).OnCopySettings(data);

        private const string PATCH_KEY = "Patch.ManualDeliveryKG.OnCopySettings";

        public static readonly IDetouredField<ManualDeliveryKG, bool> userPaused =
            PDetours.DetourField<ManualDeliveryKG, bool>("userPaused");

        public static void Patch(Harmony harmony)
        {
            if (!PRegistry.GetData<bool>(PATCH_KEY))
            {
                harmony.Patch(typeof(ManualDeliveryKG), nameof(OnSpawn),
                    postfix: new HarmonyMethod(typeof(ManualDeliveryKGPatch), nameof(OnSpawn)));
                harmony.PatchTranspile(typeof(ManualDeliveryKG), "OnRefreshUserMenu",
                    transpiler: new HarmonyMethod(typeof(ManualDeliveryKGPatch), nameof(Transpiler)));
                PRegistry.PutData(PATCH_KEY, true);
            }
        }

        private static void OnSpawn(ManualDeliveryKG __instance)
        {
            if (__instance.allowPause)
                __instance.Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate, __instance);
        }

        private static void OnCopySettings(this ManualDeliveryKG @this, object data)
        {
            if (@this.allowPause)
            {
                // правильное копирование, если компонентов несколько
                int index = Array.IndexOf(@this.GetComponents<ManualDeliveryKG>(), @this);
                var others = ((GameObject)data).GetComponents<ManualDeliveryKG>();
                if (others != null && index >= 0 && index < others.Length && others[index] != null)
                {
                    bool paused = userPaused.Get(others[index]);
                    userPaused.Set(@this, paused);
                    @this.Pause(paused, "OnCopySettings");
                }
            }
        }

        private static string ResolveTooltip(string tooltip, ManualDeliveryKG manualDelivery)
        {
            return $"{tooltip}\n{string.Format(BUILDING.STATUSITEMS.WAITINGFORMATERIALS.LINE_ITEM_UNITS, manualDelivery.RequestedItemTag.ProperName())}";
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return instructions.Transpile(original, transpiler);
        }

        private static bool transpiler(ref List<CodeInstruction> instructions)
        {
            var Tooltip1 = typeof(UI.USERMENUACTIONS.MANUAL_DELIVERY)
                .GetFieldSafe(nameof(UI.USERMENUACTIONS.MANUAL_DELIVERY.TOOLTIP), true);
            var Tooltip2 = typeof(UI.USERMENUACTIONS.MANUAL_DELIVERY)
                .GetFieldSafe(nameof(UI.USERMENUACTIONS.MANUAL_DELIVERY.TOOLTIP_OFF), true);
            var Resolver = typeof(ManualDeliveryKGPatch).GetMethodSafe(nameof(ResolveTooltip), true, PPatchTools.AnyArguments);

            bool result = false;
            if (Tooltip1 != null && Tooltip2 != null && Resolver != null)
            {
                for (int i = 0; i < instructions.Count(); i++)
                {
                    if (instructions[i].LoadsField(Tooltip1) || instructions[i].LoadsField(Tooltip2))
                    {
                        i++;
                        instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                        instructions.Insert(++i, new CodeInstruction(OpCodes.Call, Resolver));
                        result = true;
                    }
                }
            }
            return result;
        }
    }
}
