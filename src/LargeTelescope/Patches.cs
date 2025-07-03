using System;
using System.Collections.Generic;
using System.Reflection;
using TUNING;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace LargeTelescope
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            ModOptions.Reload();
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
            harmony.PatchAll();
        }

        // тюнинг искричества и стоимости
        [HarmonyPatch(typeof(ClusterTelescopeEnclosedConfig), nameof(ClusterTelescopeEnclosedConfig.CreateBuildingDef))]
        private static class ClusterTelescopeEnclosedConfig_CreateBuildingDef
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active();
            private static void Postfix(BuildingDef __result)
            {
                __result.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER4;
                if (ModOptions.Instance.add_glass)
                {
                    __result.MaterialCategory = __result.MaterialCategory.Append(MATERIALS.GLASSES[0]);
                    __result.Mass = __result.Mass.Append(BUILDINGS.CONSTRUCTION_MASS_KG.TIER3[0]);
                }
            }
        }

        [HarmonyPatch(typeof(TelescopeConfig), nameof(TelescopeConfig.CreateBuildingDef))]
        private static class TelescopeConfig_CreateBuildingDef
        {
            private static bool Prepare() => DlcManager.IsPureVanilla();
            private static void Postfix(BuildingDef __result)
            {
                if (ModOptions.Instance.add_glass)
                {
                    __result.MaterialCategory = __result.MaterialCategory.Append(MATERIALS.GLASSES[0]);
                    __result.Mass = __result.Mass.Append(BUILDINGS.CONSTRUCTION_MASS_KG.TIER3[0]);
                }
            }
        }

        // тюнинг стройки внутри ракет, радиуса скана.
        [HarmonyPatch(typeof(ClusterTelescopeEnclosedConfig), nameof(ClusterTelescopeEnclosedConfig.ConfigureBuildingTemplate))]
        private static class ClusterTelescopeEnclosedConfig_ConfigureBuildingTemplate
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active();
            private static void Postfix(GameObject go)
            {
                if (ModOptions.Instance.prohibit_inside_rocket)
                    go.AddOrGet<KPrefabID>().AddTag(GameTags.NotRocketInteriorBuilding);
                go.AddOrGetDef<ClusterTelescope.Def>().analyzeClusterRadius = ModOptions.Instance.analyze_cluster_radius;
            }
        }

        // убираем требование чоры к наличию кислорода
        [HarmonyPatch]
        private static class Telescopes_CreateChore
        {
            private static bool Prepare() => ModOptions.Instance.not_require_gas_pipe;
            private static IEnumerable<MethodBase> TargetMethods()
            {
                if (DlcManager.IsExpansion1Active())
                {
                    return new MethodBase[] {
                        typeof(ClusterTelescope.Instance).GetMethodSafe(nameof(ClusterTelescope.Instance.CreateRevealTileChore), false),
                        typeof(ClusterTelescope.Instance).GetMethodSafe(nameof(ClusterTelescope.Instance.CreateIdentifyMeteorChore), false),
                    };
                }
                else
                {
                    return new MethodBase[] { typeof(Telescope).GetMethodSafe("CreateChore", false) };
                }
            }
            private static void Postfix(Chore __result)
            {
                __result.GetPreconditions().RemoveAll(precondition => precondition.condition.id == Telescope.ContainsOxygen.id);
            }
        }

        // убираем требование наличия трубы
        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            if (ModOptions.Instance.not_require_gas_pipe)
            {
                var id = DlcManager.IsExpansion1Active() ? ClusterTelescopeEnclosedConfig.ID : TelescopeConfig.ID;
                Assets.GetBuildingDef(id).BuildingComplete.GetComponent<RequireInputs>().SetRequirements(true, false);
            }
        }

        // ускоряем скорость работы
        [HarmonyPatch]
        private static class Telescopes_OnWorkTick
        {
            private static float efficiencyMultiplier = 1f;
            private static bool Prepare()
            {
                efficiencyMultiplier = 1f + (ModOptions.Instance.efficiency_multiplier / 100f);
                return DlcManager.IsExpansion1Active();
            }

            private static IEnumerable<MethodBase> TargetMethods()
            {
                const string method = "OnWorkTick";
                var args = new Type[] { typeof(WorkerBase), typeof(float) };
                return new MethodBase[] {
                    typeof(ClusterTelescope.ClusterTelescopeWorkable).GetMethodSafe(method, false, args),
                    typeof(ClusterTelescope.ClusterTelescopeIdentifyMeteorWorkable).GetMethodSafe(method, false, args),
                };
            }

            private static void Prefix(ClusterTelescope.Instance ___m_telescope, ref float dt)
            {
                if (___m_telescope.providesOxygen)
                    dt *= efficiencyMultiplier;
            }
        }
    }
}
