using Klei.AI;
using TUNING;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
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
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            //Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            Utils.AddBuildingToPlanScreen("Utilities", RoverRefuelingStationConfig.ID);
            Utils.AddBuildingToTechnology("ArtificialFriends", RoverRefuelingStationConfig.ID);

            // todo: причесать
            var db = Db.Get();
            var rate = ROBOTS.SCOUTBOT.BATTERY_CAPACITY / RoverRefuelingStationConfig.CHARGE_TIME;
            var modifier = new AttributeModifier(db.Amounts.InternalChemicalBattery.deltaAttribute.Id, rate);
            RefuelingEffect = new Effect(RefuelingEffectID, "name", "deskr", 0, false, true, false);
            RefuelingEffect.Add(modifier);
            db.effects.Add(RefuelingEffect);
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
