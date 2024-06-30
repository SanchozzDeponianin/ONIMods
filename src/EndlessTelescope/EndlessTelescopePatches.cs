using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SanchozzONIMods.Lib;

namespace EndlessTelescope
{
    internal sealed class EndlessTelescopePatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
        }

        [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
        private static class Db_Initialize
        {
            private static void Prefix()
            {
                Utils.InitLocalization(typeof(STRINGS));
            }
        }

        [HarmonyPatch(typeof(ClusterTelescope.Instance), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(IStateMachineTarget), typeof(ClusterTelescope.Def) })]
        private static class ClusterTelescope_Instance_Constructor
        {
            private static void Postfix(ClusterTelescope.Instance __instance)
            {
                __instance.gameObject.AddOrGet<DeepSpaceTelescope>();
            }
        }

        [HarmonyPatch(typeof(ClusterTelescope.Instance), nameof(ClusterTelescope.Instance.CheckHasAnalyzeTarget))]
        private static class ClusterTelescope_Instance_CheckHasAnalyzeTarget
        {
            private static void Postfix(ClusterTelescope.Instance __instance, bool __result)
            {
                if (__instance.gameObject.TryGetComponent<DeepSpaceTelescope>(out var dst))
                    dst.UpdateEfficiencyMultiplier(__result);
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var radius = typeof(ClusterTelescope.Def).GetField(nameof(ClusterTelescope.Def.analyzeClusterRadius));
                if (radius != null)
                {
                    int i = instructions.FindIndex(instr => instr.LoadsField(radius));
                    if (i != -1)
                    {
                        instructions.Insert(++i, new CodeInstruction(OpCodes.Pop));
                        instructions.Insert(++i, new CodeInstruction(OpCodes.Ldc_I4_S, 100));
                        return true;
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(ClusterTelescope.ClusterTelescopeWorkable), nameof(ClusterTelescope.ClusterTelescopeWorkable.GetEfficiencyMultiplier))]
        private static class ClusterTelescope_ClusterTelescopeWorkable_GetEfficiencyMultiplier
        {
            private static void Postfix(ClusterTelescope.ClusterTelescopeWorkable __instance, ref float __result)
            {
                if (__instance.TryGetComponent<DeepSpaceTelescope>(out var dst))
                    __result *= dst.EfficiencyMultiplier;
            }
        }
    }
}
