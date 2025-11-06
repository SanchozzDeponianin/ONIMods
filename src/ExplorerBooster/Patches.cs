using System.Collections.Generic;
using STRINGS;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;
using static TUNING.ITEMS.BIONIC_UPGRADES;

namespace ExplorerBooster
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (!DlcManager.IsContentSubscribed(DlcManager.DLC3_ID) || this.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            base.OnLoad(harmony);
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [HarmonyPatch(typeof(BionicUpgradeComponentConfig), nameof(BionicUpgradeComponentConfig.CreatePrefabs))]
        private static class BionicUpgradeComponentConfig_CreatePrefabs
        {
            private static void Postfix(List<GameObject> __result)
            {
                const string upgradeID = "Booster_Explorer";
                bool basic = ModOptions.Instance.craft_at == CraftAt.Basic;
                if (basic)
                    BionicUpgradeComponentConfig.BASIC_BOOSTERS.Add(upgradeID);
                float wattage = POWER_COST.TIER_2;
                switch (ModOptions.Instance.wattage)
                {
                    case WattageCost.TIER_0:
                        wattage = POWER_COST.TIER_0;
                        break;
                    case WattageCost.TIER_1:
                        wattage = POWER_COST.TIER_1;
                        break;
                    case WattageCost.TIER_2:
                        wattage = POWER_COST.TIER_2;
                        break;
                    case WattageCost.TIER_3:
                        wattage = POWER_COST.TIER_3;
                        break;
                }
                var description = string.Format("{0}\n{1}\n\n",
                    UI.UISIDESCREENS.BIONIC_SIDE_SCREEN.BOOSTER_ASSIGNMENT.HEADER_PERKS,
                    STRINGS.ITEMS.BIONIC_BOOSTERS.BOOSTER_EXPLORER.EFFECT);
                if (wattage > 0f)
                {
                    description += string.Format("<b>{0}</b>: {1}\n\n",
                        DUPLICANTS.MODIFIERS.BIONIC_WATTS.NAME,
                        GameUtil.GetFormattedWattage(wattage, GameUtil.WattageFormatterUnit.Watts));
                }
                description += string.Format(ITEMS.BIONIC_BOOSTERS.FABRICATION_SOURCE,
                        basic ? BUILDINGS.PREFABS.CRAFTINGTABLE.NAME : BUILDINGS.PREFABS.ADVANCEDCRAFTINGTABLE.NAME);
                var booster = BionicUpgradeComponentConfig.CreateNewUpgradeComponent(
                    id: upgradeID,
                    wattageCost: wattage,
                    stateMachine: smi => new BionicUpgrade_ExplorerBoosterMonitor.Instance(smi.GetMaster(),
                        new BionicUpgrade_ExplorerBoosterMonitor.Def(upgradeID)),
                    sm_description: description,
                    dlcIDs: DlcManager.DLC3,
                    animFile: "upgrade_disc_explorer_kanim",
                    animStateName: "explorer",
                    booster: BionicUpgradeComponentConfig.BoosterType.Special,
                    isStartingBooster: true,
                    isCarePackage: ModOptions.Instance.care_package,
                    skillPerks: new Database.SkillPerk[0]
                    );
                booster.AddOrGetDef<BionicUpgrade_ExplorerBooster.Def>();
                __result.Add(booster);
                if (!ModOptions.Instance.starting_booster)
                    TUNING.DUPLICANTSTATS.BIONICUPGRADETRAITS.RemoveAll(TraitVal => TraitVal.id == "StartWith" + upgradeID);
            }
        }

        // IsInBedTimeChore смотрит на наличие тэга GameTags.BionicBedTime,
        // поэтому в сочетании с событиями GameHashes.ScheduleBlocksChanged / GameHashes.ScheduleChanged
        // оно даёт неверный результат. так как BionicBedTimeModeChore ещё не добавило / удалило тэг
        // добавим проверки при добавлении / удалении тэга
        [HarmonyPatch(typeof(BionicUpgrade_ExplorerBoosterMonitor), nameof(BionicUpgrade_ExplorerBoosterMonitor.InitializeStates))]
        private static class BionicUpgrade_ExplorerBoosterMonitor_InitializeStates
        {
            private static void Postfix(BionicUpgrade_ExplorerBoosterMonitor __instance)
            {
                __instance.Inactive
                    .EventHandlerTransition(GameHashes.TagsChanged, __instance.Active, (smi, data) =>
                    {
                        var @event = ((Boxed<TagChangedEventData>)data).value;
                        return @event.tag == GameTags.BionicBedTime && @event.added == true
                            && BionicUpgrade_ExplorerBoosterMonitor.ShouldBeActive(smi);
                    });

                __instance.Active
                    .TriggerOnEnter(GameHashes.BionicUpgradeWattageChanged, null)
                    .EventHandlerTransition(GameHashes.TagsChanged, __instance.Inactive, (smi, data) =>
                    {
                        var @event = ((Boxed<TagChangedEventData>)data).value;
                        return @event.tag == GameTags.BionicBedTime && @event.added == false
                            && !BionicUpgrade_ExplorerBoosterMonitor.IsInBedTimeChore(smi);
                    });
            }
        }
    }
}
