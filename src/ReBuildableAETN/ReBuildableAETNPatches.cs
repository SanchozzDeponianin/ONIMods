using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Database;
using TUNING;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using static ReBuildableAETN.MassiveHeatSinkCoreConfig;

namespace ReBuildableAETN
{
    public sealed class ReBuildableAETNPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            ReBuildableAETNOptions.Reload();
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(GetType());
            if (DlcManager.IsExpansion1Active())
                new POptions().RegisterOptions(this, typeof(ReBuildableAETNSpaceOutOptions));
            else
                new POptions().RegisterOptions(this, typeof(ReBuildableAETNVanillaOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            Utils.AddBuildingToPlanScreen("Utilities", MassiveHeatSinkConfig.ID);
            Utils.AddBuildingToTechnology("Catalytics", MassiveHeatSinkConfig.ID);
            GameTags.MaterialBuildingElements.Add(ID);
            // добавляем ядра -выдры- в космос
            var chances = ReBuildableAETNOptions.Instance.VanillaPlanet;
            if (DlcManager.IsPureVanilla() && chances.Enabled)
            {
                var sdp = Db.Get().SpaceDestinationTypes;
                CloneArtifactDropRateTable(sdp.IcyDwarf, TIER_CORE, chances.IcyDwarfChance / 100f);
                CloneArtifactDropRateTable(sdp.IceGiant, TIER_CORE, chances.IceGiantChance / 100f);
            }
        }

        private static void CloneArtifactDropRateTable(SpaceDestinationType destination, ArtifactTier tier, float weight_percent)
        {
            var result = new ArtifactDropRate();
            float weight = destination.artifactDropTable.totalWeight * weight_percent;
            foreach (var rate in destination.artifactDropTable.rates)
            {
                if (rate.first == DECOR.SPACEARTIFACT.TIER_NONE)
                    result.AddItem(rate.first, rate.second - weight);
                else
                    result.AddItem(rate.first, rate.second);
            }
            result.AddItem(tier, weight);
            destination.artifactDropTable = result;
        }

        // todo: если возможно - добавлять его в теху и мюню только после изучения первого дикого аетна
        // todo: дополнительные способы получения ядер
        // todo: проверить и сделать - на длц, чтобы нельзя было строить из замурованной версии.
        // todo: проверить на длц - все места которые теоритически могут вызвать краш после отключения мода. найти обходные пути

        [HarmonyPatch(typeof(MassiveHeatSinkConfig), nameof(MassiveHeatSinkConfig.CreateBuildingDef))]
        internal static class MassiveHeatSinkConfig_CreateBuildingDef
        {
            private static void Postfix(ref BuildingDef __result)
            {
                __result.ViewMode = OverlayModes.GasConduits.ID;
                __result.MaterialCategory = MATERIALS.REFINED_METALS.AddItem(ID).ToArray();
                __result.Mass = __result.Mass.AddItem(2).ToArray();
                if (ReBuildableAETNOptions.Instance.AddLogicPort)
                    __result.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(1, 0));
            }
        }

        [HarmonyPatch(typeof(MassiveHeatSinkConfig), nameof(MassiveHeatSinkConfig.DoPostConfigureComplete))]
        internal static class MassiveHeatSinkConfig_DoPostConfigureComplete
        {
            private static void Postfix(GameObject go)
            {
                // требование навыка для постройки
                var constructable = go.GetComponent<Building>().Def.BuildingUnderConstruction.GetComponent<Constructable>();
                constructable.requiredSkillPerk = Db.Get().SkillPerks.CanDemolish.Id;
                // требование навыка для разрушения
                var deconstructable = go.GetComponent<Deconstructable>();
                deconstructable.requiredSkillPerk = Db.Get().SkillPerks.CanDemolish.Id;
                deconstructable.allowDeconstruction = false;
                go.AddOrGet<MassiveHeatSinkRebuildable>();
                if (ReBuildableAETNOptions.Instance.AddLogicPort)
                    go.AddOrGet<LogicOperationalController>();
            }
        }

        // скрываем требование навыка пока разрушение не назначено
        [HarmonyPatch(typeof(Deconstructable), "OnSpawn")]
        internal static class Deconstructable_OnSpawn
        {
            private static void Prefix(ref bool ___shouldShowSkillPerkStatusItem)
            {
                ___shouldShowSkillPerkStatusItem = false;
            }
        }

        [HarmonyPatch]
        internal static class Deconstructable_Queue_Cancel_Deconstruction
        {
            private static readonly DetouredMethod<Action<Workable, object>> UpdateStatusItem =
                typeof(Workable).DetourLazy<Action<Workable, object>>("UpdateStatusItem");

            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return typeof(Deconstructable).GetMethodSafe("QueueDeconstruction", false, PPatchTools.AnyArguments);
                yield return typeof(Deconstructable).GetMethodSafe("CancelDeconstruction", false, PPatchTools.AnyArguments);
            }

            private static void Postfix(Deconstructable __instance, ref bool ___shouldShowSkillPerkStatusItem, bool ___isMarkedForDeconstruction)
            {
                ___shouldShowSkillPerkStatusItem = ___isMarkedForDeconstruction;
                UpdateStatusItem.Invoke(__instance, null);
            }
        }

        // посылка
        [HarmonyPatch(typeof(Immigration), "ConfigureCarePackages")]
        internal static class Immigration_ConfigureCarePackages
        {
            private static bool Prepare()
            {
                return ReBuildableAETNOptions.Instance.CarePackage.Enabled;
            }

            private static bool Condition(Tag tag)
            {
                return (GameClock.Instance.GetCycle() >= ReBuildableAETNOptions.Instance.CarePackage.MinCycle)
                    && (!ReBuildableAETNOptions.Instance.CarePackage.RequireDiscovered
                        || DiscoveredResources.Instance.IsDiscovered(tag));
            }

            private static void Postfix(ref CarePackageInfo[] ___carePackages)
            {
                var core = new CarePackageInfo(ID, 1, () => Condition(ID));
                ___carePackages = ___carePackages.AddItem(core).ToArray();
            }
        }
    }
}

