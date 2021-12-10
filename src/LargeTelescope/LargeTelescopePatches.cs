using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TUNING;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace LargeTelescope
{
    internal sealed class LargeTelescopePatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            LargeTelescopeOptions.Reload();
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(LargeTelescopePatches));
            new POptions().RegisterOptions(this, typeof(LargeTelescopeOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        // тюнинг искричества и стоимости
        [HarmonyPatch(typeof(ClusterTelescopeEnclosedConfig), nameof(ClusterTelescopeEnclosedConfig.CreateBuildingDef))]
        private static class ClusterTelescopeEnclosedConfig_CreateBuildingDef
        {
            private static void Postfix(BuildingDef __result)
            {
                __result.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER4;
                if (LargeTelescopeOptions.Instance.add_glass)
                {
                    __result.MaterialCategory = __result.MaterialCategory.AddItem(MATERIALS.GLASSES[0]).ToArray();
                    __result.Mass = __result.Mass.AddItem(BUILDINGS.CONSTRUCTION_MASS_KG.TIER3[0]).ToArray();
                }
            }
        }

        // тюнинг стройки внутри ракет, радиуса и скорости скана.
        [HarmonyPatch(typeof(ClusterTelescopeEnclosedConfig), nameof(ClusterTelescopeEnclosedConfig.ConfigureBuildingTemplate))]
        private static class ClusterTelescopeEnclosedConfig_ConfigureBuildingTemplate
        {
            private static void Prefix(GameObject go)
            {
                if (LargeTelescopeOptions.Instance.prohibit_inside_rocket)
                    go.AddOrGet<KPrefabID>().AddTag(GameTags.NotRocketInteriorBuilding);
                go.AddOrGet<ClusterLargeTelescopeWorkable>().efficiencyMultiplier = 1f + (LargeTelescopeOptions.Instance.efficiency_multiplier / 100f);
            }

            private static void Postfix(GameObject go)
            {
                go.AddOrGetDef<ClusterTelescope.Def>().analyzeClusterRadius = LargeTelescopeOptions.Instance.analyze_cluster_radius;
            }
        }

        // убираем требование к трубе
        [HarmonyPatch(typeof(ClusterTelescopeEnclosedConfig), nameof(ClusterTelescopeEnclosedConfig.DoPostConfigureComplete))]
        private static class ClusterTelescopeEnclosedConfig_DoPostConfigureComplete
        {
            private static bool Prepare() => LargeTelescopeOptions.Instance.not_require_gas_pipe;
            private static void Postfix(GameObject go)
            {
                go.GetComponent<RequireInputs>().SetRequirements(true, false);
            }
        }

        // убираем требование чоры к наличию кислорода
        [HarmonyPatch(typeof(ClusterTelescope.Instance), nameof(ClusterTelescope.Instance.CreateChore))]
        private static class ClusterTelescope_Instance_CreateChore
        {
            private static readonly IDetouredField<Chore, List<Chore.PreconditionInstance>> preconditions = PDetours.DetourField<Chore, List<Chore.PreconditionInstance>>("preconditions");
            private static bool Prepare() => LargeTelescopeOptions.Instance.not_require_gas_pipe;
            private static void Postfix(Chore __result)
            {
                preconditions.Get(__result).RemoveAll(precondition => precondition.id == Telescope.ContainsOxygen.id);
            }
        }

        // ConsumeGas напрямую меняет массу PrimaryElement, это не триггерит событие OnStorageChange, нужно перепроверять
        [HarmonyPatch(typeof(ClusterTelescope.ClusterTelescopeWorkable), nameof(ClusterTelescope.ClusterTelescopeWorkable.ConsumeGas))]
        private static class ClusterTelescope_ClusterTelescopeWorkable_ConsumeGas
        {
            private static void Postfix(ClusterTelescope.ClusterTelescopeWorkable __instance)
            {
                (__instance as ClusterLargeTelescopeWorkable)?.CheckStorageIsEmpty();
            }
        }

        // отключаем манипуляции клеев с GasProvider, так как у нас свои есть
        [HarmonyPatch(typeof(ClusterTelescope.ClusterTelescopeWorkable), "OnWorkableEvent")]
        private static class ClusterTelescope_ClusterTelescopeWorkable_OnWorkableEvent_Oxygen
        {
            private static void StubSetGasProvider(OxygenBreather breather, OxygenBreather.IGasProvider gas_provider) { }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator IL)
            {
                string methodName = method.DeclaringType.FullName + "." + method.Name;
                var SetGasProvider = typeof(OxygenBreather).GetMethodSafe(nameof(OxygenBreather.SetGasProvider), false, PPatchTools.AnyArguments);
                var Stub = typeof(ClusterTelescope_ClusterTelescopeWorkable_OnWorkableEvent_Oxygen).GetMethodSafe(nameof(StubSetGasProvider), true, PPatchTools.AnyArguments);

                bool result = false;
                if (SetGasProvider != null && Stub != null)
                {
                    instructions = PPatchTools.ReplaceMethodCall(instructions, SetGasProvider, Stub);
                    result = true;
                }
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
#if DEBUG
                else
                    PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                return instructions;
            }
        }
    }
}
