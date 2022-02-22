using System.Collections.Generic;
using STRINGS;
using SanchozzONIMods.Lib;

namespace RoverRefueling
{
    public class STRINGS
    {
        private const string ROVER = "{ROVER}";
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class ROVERREFUELINGSTATION
                {
                    public static LocString NAME = UI.FormatAsLink($"{ROVER}'s Recharger", "ROVERREFUELINGSTATION");
                    public static LocString DESC = "Chemical Beer is only for Robots. Duplicants are forbidden to drink!";
                    public static LocString EFFECT = $"Here the {ROVER} can recharge its chemical battery with chemical fuel.\n\nThe {ROVER} will only recharge if its battery charge reaches a low value.\n\nHowever, it is impossible to charge a fully discharged battery.";
                    public static LocString REQUIREMENT_TOOLTIP = $"Requires approximately {UI.FormatAsNegativeRate("{0}")} kg of {{1}} to recharge nearly fully.";
                }
            }
        }

        public class DUPLICANTS
        {
            public class CHORES
            {
                public class PRECONDITIONS
                {
                    public static LocString ROVER_NEED_REFUELING = $"not a {ROVER}";
                }
            }
            public class MODIFIERS
            {
                public class SCOUTBOTREFUELING
                {
                    public static LocString NAME = "Charging";
                    public static LocString TOOLTIP = $"{ROVER} is happily charging at {BUILDINGS.PREFABS.ROVERREFUELINGSTATION.NAME}.\nmmm... yummy Chemical Beer!";
                }
            }
        }

        public class OPTIONS
        {
            public class CHARGE_TIME
            {
                public static LocString NAME = "Recharge time, seconds";
            }
            public class FUEL_MASS_PER_CHARGE
            {
                public static LocString NAME = "Fuel mass per one recharge, kg";
            }
        }

        internal static void DoReplacement()
        {
            var replacement = new Dictionary<string, string>() { { ROVER, ROBOTS.MODELS.SCOUT.NAME } };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), replacement);
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
            Strings.Add("STRINGS.ROBOTS.ATTRIBUTES.INTERNALCHEMICALBATTERYDELTA.NAME", ROBOTS.STATS.INTERNALCHEMICALBATTERY.NAME);
        }
    }
}
