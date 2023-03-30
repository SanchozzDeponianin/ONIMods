using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            private static bool Prepare() => DlcManager.IsExpansion1Active();
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

        [HarmonyPatch(typeof(TelescopeConfig), nameof(TelescopeConfig.CreateBuildingDef))]
        private static class TelescopeConfig_CreateBuildingDef
        {
            private static bool Prepare() => DlcManager.IsPureVanilla();
            private static void Postfix(BuildingDef __result)
            {
                if (LargeTelescopeOptions.Instance.add_glass)
                {
                    __result.MaterialCategory = __result.MaterialCategory.AddItem(MATERIALS.GLASSES[0]).ToArray();
                    __result.Mass = __result.Mass.AddItem(BUILDINGS.CONSTRUCTION_MASS_KG.TIER3[0]).ToArray();
                }
            }
        }

        // тюнинг стройки внутри ракет, радиуса и скорости скана. исправленный газпрофидер
        [HarmonyPatch(typeof(ClusterTelescopeEnclosedConfig), nameof(ClusterTelescopeEnclosedConfig.ConfigureBuildingTemplate))]
        private static class ClusterTelescopeEnclosedConfig_ConfigureBuildingTemplate
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active();
            private static void Postfix(GameObject go)
            {
                if (LargeTelescopeOptions.Instance.prohibit_inside_rocket)
                    go.AddOrGet<KPrefabID>().AddTag(GameTags.NotRocketInteriorBuilding);
                go.AddOrGetDef<ClusterTelescope.Def>().analyzeClusterRadius = LargeTelescopeOptions.Instance.analyze_cluster_radius;
                go.AddOrGet<TelescopeGasProvider>().efficiencyMultiplier = 1f + (LargeTelescopeOptions.Instance.efficiency_multiplier / 100f);
            }
        }

        // исправленный газпрофидер для ванильного телескопа
        [HarmonyPatch(typeof(TelescopeConfig), nameof(TelescopeConfig.ConfigureBuildingTemplate))]
        private static class TelescopeConfig_ConfigureBuildingTemplate
        {
            private static bool Prepare() => DlcManager.IsPureVanilla();
            private static void Postfix(GameObject go)
            {
                go.AddOrGet<TelescopeGasProvider>().efficiencyMultiplier = 1f;
            }
        }

        // убираем требование чоры к наличию кислорода
        [HarmonyPatch]
        private static class Telescopes_CreateChore
        {
            private static readonly IDetouredField<Chore, List<Chore.PreconditionInstance>> preconditions = PDetours.DetourField<Chore, List<Chore.PreconditionInstance>>("preconditions");
            private static bool Prepare() => LargeTelescopeOptions.Instance.not_require_gas_pipe;
            private static IEnumerable<MethodBase> TargetMethods()
            {
                if (DlcManager.IsExpansion1Active())
                {
                    yield return typeof(ClusterTelescope.Instance).GetMethodSafe(nameof(ClusterTelescope.Instance.CreateRevealTileChore), false);
                    yield return typeof(ClusterTelescope.Instance).GetMethodSafe(nameof(ClusterTelescope.Instance.CreateIdentifyMeteorChore), false);
                }
                else
                {
                    yield return typeof(Telescope).GetMethodSafe("CreateChore", false);
                }
            }
            private static void Postfix(Chore __result)
            {
                preconditions.Get(__result).RemoveAll(precondition => precondition.id == Telescope.ContainsOxygen.id);
            }
        }

        // убираем требование наличия трубы
        [HarmonyPatch(typeof(BuildingConfigManager), nameof(BuildingConfigManager.ConfigurePost))]
        private static class BuildingConfigManager_ConfigurePost
        {
            private static bool Prepare() => LargeTelescopeOptions.Instance.not_require_gas_pipe;
            private static void Postfix()
            {
                var id = DlcManager.IsExpansion1Active() ? ClusterTelescopeEnclosedConfig.ID : TelescopeConfig.ID;
                Assets.GetBuildingDef(id).BuildingComplete.GetComponent<RequireInputs>().SetRequirements(true, false);
            }
        }

        // отключаем манипуляции клеев с GasProvider
        [HarmonyPatch]
        private static class Telescopes_OnWorkableEvent
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                const string method = "OnWorkableEvent";
                var args = new Type[] { typeof(Workable), typeof(Workable.WorkableEvent) };
                if (DlcManager.IsExpansion1Active())
                {
                    yield return typeof(ClusterTelescope.ClusterTelescopeWorkable).GetMethodSafe(method, false, args);
                    yield return typeof(ClusterTelescope.ClusterTelescopeIdentifyMeteorWorkable).GetMethodSafe(method, false, args);
                }
                else
                {
                    yield return typeof(Telescope).GetMethodSafe(method, false, args);
                }
            }

            private static void StubSetGasProvider(OxygenBreather breather, OxygenBreather.IGasProvider gas_provider) { }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var SetGasProvider = typeof(OxygenBreather).GetMethodSafe(nameof(OxygenBreather.SetGasProvider), false, typeof(OxygenBreather.IGasProvider));
                var Stub = typeof(Telescopes_OnWorkableEvent).GetMethodSafe(nameof(StubSetGasProvider), true, PPatchTools.AnyArguments);
                if (SetGasProvider != null && Stub != null)
                {
                    instructions = PPatchTools.ReplaceMethodCallSafe(instructions, SetGasProvider, Stub).ToList();
                    return true;
                }
                return false;
            }

            // и добавляем свой исправленный газпрофидер
            private static void Postfix(Workable workable, Workable.WorkableEvent ev)
            {
                if (workable != null && workable.TryGetComponent<TelescopeGasProvider>(out var gasProvider))
                {
                    if (ev == Workable.WorkableEvent.WorkStarted && !gasProvider.IsEmpty)
                    {
                        gasProvider.OverrideGasProvider(workable.worker);
                    }
                    else if (ev == Workable.WorkableEvent.WorkStopped)
                    {
                        gasProvider.RestoreGasProvider();
                    }
                }
            }
        }

        // ускоряем скорость работы
        // переключаем газпрофидеры если в процессе работы закончился/снова появился кислород
        [HarmonyPatch]
        private static class Telescopes_OnWorkTick
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                const string method = "OnWorkTick";
                var args = new Type[] { typeof(Worker), typeof(float) };
                if (DlcManager.IsExpansion1Active())
                {
                    yield return typeof(ClusterTelescope.ClusterTelescopeWorkable).GetMethodSafe(method, false, args);
                    yield return typeof(ClusterTelescope.ClusterTelescopeIdentifyMeteorWorkable).GetMethodSafe(method, false, args);
                }
                else
                {
                    yield return typeof(Telescope).GetMethodSafe(method, false, args);
                }
            }

            private static void Prefix(Workable __instance, Worker worker, ref float dt)
            {
                if (__instance != null && __instance.TryGetComponent<TelescopeGasProvider>(out var gasProvider))
                {
                    if (gasProvider.IsEmpty)
                        gasProvider.RestoreGasProvider();
                    else if (!gasProvider.HasBreather)
                        gasProvider.OverrideGasProvider(worker);
                    dt *= gasProvider.efficiencyMultiplier;
                }
            }
        }
    }
}
