using Klei.AI;
using TUNING;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace RoverRefueling
{
    internal sealed class RoverRefuelingPatches : KMod.UserMod2
    {
        public const string RefuelingEffectID = "ScoutBotRefueling";
        public static Effect RefuelingEffect;

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(RoverRefuelingPatches));
            new POptions().RegisterOptions(this, typeof(RoverRefuelingOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            Utils.AddBuildingToPlanScreen("Utilities", RoverRefuelingStationConfig.ID, "other utilities", SweepBotStationConfig.ID);
            Utils.AddBuildingToTechnology("ArtificialFriends", RoverRefuelingStationConfig.ID);
            var db = Db.Get();
            var rate = ROBOTS.SCOUTBOT.BATTERY_CAPACITY / RoverRefuelingOptions.Instance.charge_time;
            var modifier = new AttributeModifier(
                attribute_id: db.Amounts.InternalChemicalBattery.deltaAttribute.Id,
                value: rate,
                description: STRINGS.DUPLICANTS.MODIFIERS.SCOUTBOTREFUELING.NAME);
            RefuelingEffect = new Effect(
                id: RefuelingEffectID,
                name: STRINGS.DUPLICANTS.MODIFIERS.SCOUTBOTREFUELING.NAME,
                description: STRINGS.DUPLICANTS.MODIFIERS.SCOUTBOTREFUELING.TOOLTIP,
                duration: 0,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false);
            RefuelingEffect.Add(modifier);
            db.effects.Add(RefuelingEffect);
            db.RobotStatusItems.LowBatteryNoCharge.tooltipText = global::STRINGS.ROBOTS.STATUSITEMS.LOWBATTERY.TOOLTIP;
        }

        public static Tag RoverNeedRefueling = TagManager.Create(nameof(RoverNeedRefueling));

        [HarmonyPatch(typeof(RobotBatteryMonitor), nameof(RobotBatteryMonitor.InitializeStates))]
        private static class RobotBatteryMonitor_InitializeStates
        {
            private static void Postfix(RobotBatteryMonitor __instance)
            {
                __instance.drainingStates.lowBattery.ToggleTag(RoverNeedRefueling);
                __instance.needsRechargeStates.lowBattery.ToggleTag(RoverNeedRefueling);
            }
        }
    }
}
