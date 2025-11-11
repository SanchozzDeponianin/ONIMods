using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using STRINGS;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;
using static TUNING.ITEMS.BIONIC_UPGRADES;
using monitor = BionicUpgrade_ExplorerBoosterMonitor;

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
                var def = new monitor.Def(upgradeID);
                var booster = BionicUpgradeComponentConfig.CreateNewUpgradeComponent(
                    id: upgradeID,
                    wattageCost: wattage,
                    stateMachine: smi => new monitor.Instance(smi.GetMaster(), def),
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

        private static readonly GameHashes GeyserRevealed = (GameHashes)Hash.SDBMLower(nameof(GeyserRevealed));

        // IsInBedTimeChore смотрит на наличие тэга GameTags.BionicBedTime,
        // поэтому в сочетании с событиями GameHashes.ScheduleBlocksChanged / GameHashes.ScheduleChanged
        // оно даёт неверный результат. так как BionicBedTimeModeChore ещё не добавило / удалило тэг
        // добавим проверки при добавлении / удалении тэга

        // добавим статусы чтобы различать просто бездействие / больше нечего исследовать
        [HarmonyPatch(typeof(monitor), nameof(monitor.InitializeStates))]
        private static class BionicUpgrade_ExplorerBoosterMonitor_InitializeStates
        {
            private static void Postfix(monitor __instance)
            {
                var Standby = __instance.CreateState("JustStandby", __instance.Inactive);
                var Finished = __instance.CreateState("NoGeysersToDiscover", __instance.Inactive);

                __instance.Inactive
                    .EventHandlerTransition(GameHashes.TagsChanged, __instance.Active, (smi, data) =>
                    {
                        var @event = ((Boxed<TagChangedEventData>)data).value;
                        return @event.tag == GameTags.BionicBedTime && @event.added == true
                            && monitor.ShouldBeActive(smi);
                    })
                    .DefaultState(Standby);

                Standby
                    .EnterTransition(Finished, monitor.Not(monitor.IsThereGeysersToDiscover))
                    .EventHandlerTransition(GeyserRevealed, smi => Game.Instance, Finished, IsThereNoMoreGeysersToDiscoverInMyWorld)
                    .EventTransition(GameHashes.MinionMigration, smi => Game.Instance, Finished,
                        monitor.And(ShouldBeInActive, monitor.Not(monitor.IsThereGeysersToDiscover)));

                Finished
                    .ToggleStatusItem(
                        name: STRINGS.DUPLICANTS.STATUSITEMS.BIONICEXPLORERBOOSTER_FINISHED.NAME,
                        tooltip: STRINGS.DUPLICANTS.STATUSITEMS.BIONICEXPLORERBOOSTER_FINISHED.TOOLTIP,
                        icon_type: StatusItem.IconType.Exclamation,
                        notification_type: NotificationType.Bad)
                    .EventTransition(GameHashes.MinionMigration, smi => Game.Instance, Standby,
                        monitor.And(ShouldBeInActive, monitor.IsThereGeysersToDiscover));

                __instance.Active
                    .TriggerOnEnter(GameHashes.BionicUpgradeWattageChanged, null)
                    .EventHandlerTransition(GameHashes.TagsChanged, __instance.Inactive, (smi, data) =>
                    {
                        var @event = ((Boxed<TagChangedEventData>)data).value;
                        return @event.tag == GameTags.BionicBedTime && @event.added == false
                            && !monitor.IsInBedTimeChore(smi);
                    });

                // если один бионик нашел гейзер, заставим остальных биоников перепроверить остались ли ещё гейзеры
                __instance.Active.gatheringData
                    .EventHandlerTransition(GeyserRevealed, smi => Game.Instance, Finished, IsThereNoMoreGeysersToDiscoverInMyWorld);

                __instance.Active.discover
                    .Enter(smi => GameScheduler.Instance.Schedule("", 1f, ScheduleGeyserDiscoveredCB, smi));
            }

            private static void ScheduleGeyserDiscoveredCB(object data)
            {
                var smi = data as monitor.Instance;
                if (!smi.IsNullOrStopped())
                    Game.Instance.BoxingTrigger((int)GeyserRevealed, smi.GetMyParentWorldId());
            }

            private static bool ShouldBeInActive(monitor.Instance smi) => !(monitor.IsOnline(smi) && monitor.IsInBedTimeChore(smi));

            private static bool IsThereNoMoreGeysersToDiscoverInMyWorld(monitor.Instance smi, object data)
            {
                int worldId = ((Boxed<int>)data).value;
                return (smi.GetMyParentWorldId() == worldId) && !monitor.IsThereGeysersToDiscover(smi);
            }
        }

        [HarmonyPatch]
        private static class BionicUpgrade_ExplorerBoosterMonitor_DoUse_ParentWorldId
        {
            // myWorld.id  =>  myWorld.ParentWorldId 
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new[] {
                    typeof(monitor).GetMethod(nameof(monitor.IsThereGeysersToDiscover), new []{ typeof(monitor.Instance) }),
                    typeof(monitor).GetMethod(nameof(monitor.RevealUndiscoveredGeyser), new []{ typeof(monitor.Instance) })};
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var worldId = typeof(WorldContainer).GetField(nameof(WorldContainer.id));
                var parentWorldId = typeof(WorldContainer).GetProperty(nameof(WorldContainer.ParentWorldId)).GetGetMethod();
                if (worldId == null || parentWorldId == null)
                    return false;
                for (int i = 0; i < instructions.Count; i++)
                {
                    if (instructions[i].LoadsField(worldId))
                    {
                        instructions[i].opcode = OpCodes.Callvirt;
                        instructions[i].operand = parentWorldId;
                    }
                }
                return true;
            }
        }
    }
}
