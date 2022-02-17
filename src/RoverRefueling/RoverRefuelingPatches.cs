using Klei.AI;
using TUNING;
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
            Utils.AddBuildingToPlanScreen("Equipment", RoverRefuelingStationConfig.ID);
            Utils.AddBuildingToTechnology("ImprovedGasPiping", RoverRefuelingStationConfig.ID);

            // todo: причесать
            var db = Db.Get();
            var rate = ROBOTS.SCOUTBOT.BATTERY_CAPACITY / RoverRefuelingStationConfig.CHARGE_TIME;
            var modifier = new AttributeModifier(db.Amounts.InternalChemicalBattery.deltaAttribute.Id, rate);

            RefuelingEffect = new Effect(RefuelingEffectID, "name", "deskr", 0, false, true, false);
            RefuelingEffect.Add(modifier);
            db.effects.Add(RefuelingEffect);
        }
    }
}
