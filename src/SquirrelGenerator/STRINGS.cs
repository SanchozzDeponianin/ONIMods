using STRINGS;
using SanchozzONIMods.Lib;

namespace SquirrelGenerator
{
    public class STRINGS
    {
        public const string SQUIRREL = "{Squirrel}";
        public const string WATT = "{WATT}";
        public const string DTU_S = "{DTU_S}";

        public class BUILDINGS
        {
            public class PREFABS
            {
                public class SQUIRRELGENERATOR
                {
                    public static LocString NAME = UI.FormatAsLink("Squirrel Wheel", "SQUIRRELGENERATOR");
                    public static LocString DESC = $"Watching {SQUIRREL} run on it is adorable... the electrical power is just an added bonus.";
                    public static LocString EFFECT = string.Concat(new string[]
                    {
                        "Converts a creature’s muscular labor into electrical ",
                        UI.FormatAsLink("Power", "POWER"),
                        ".\n\n",
                        $"A {SQUIRREL} is happier when it runs in a wheel, but consumes more food.",
                    });
                }
            }
        }

        public class CREATURES
        {
            public class MODIFIERS
            {
                public class RUN_IN_WHEEL
                {
                    public static LocString NAME = "Running in a Wheel";
                    public static LocString TOOLTIP = $"This critter is running in a {UI.FormatAsKeyWord("Squirrel Wheel")} and generate {UI.FormatAsKeyWord("Power")}";
                }
            }

            public class STATUSITEMS
            {
                public class EXCITED_TO_RUN_IN_WHEEL
                {
                    public static LocString NAME = "Interested";
                    public static LocString TOOLTIP = "This creature has discovered an entertaining mechanical thing and is very interested";
                }
            }
        }

        public class OPTIONS
        {
            public class GENERATORWATTAGE
            {
                public static LocString TITLE = "Wattage";
                public static LocString TOOLTIP = $"Maximum {UI.FormatAsKeyWord("Wattage")},{WATT} of Squirrel Wheel";
            }
            public class SELFHEAT
            {
                public static LocString TITLE = "Heat Production";
                public static LocString TOOLTIP = $"{UI.FormatAsKeyWord("Heat")} Production,{DTU_S} of Squirrel Wheel";
            }
            public class HAPPINESSBONUS
            {
                public static LocString TITLE = "Happiness Bonus";
                public static LocString TOOLTIP = $"{UI.FormatAsKeyWord("Happiness")} Bonus value when the {SQUIRREL} running in the Wheel";
            }
            public class METABOLISMBONUS
            {
                public static LocString TITLE = "Metabolism Bonus";
                public static LocString TOOLTIP = $"{UI.FormatAsKeyWord("Metabolism")} Bonus value when the {SQUIRREL} running in the Wheel";
            }
            public class SEARCHWHEELRADIUS
            {
                public static LocString TITLE = "Search Radius";
                public static LocString TOOLTIP = $"Radius in cells in which the {SQUIRREL} is looking for a Wheel";
            }
            public class SEARCHMININTERVAL
            {
                public static LocString TITLE = "Search Min Interval";
                public static LocString TOOLTIP = $"Time in seconds until next Wheel search is random value between {UI.FormatAsKeyWord("Search Min Interval")} and {UI.FormatAsKeyWord("Search Max Interval")}";
            }
            public class SEARCHMAXINTERVAL
            {
                public static LocString TITLE = "Search Max Interval";
            }
        }

        internal static void DoReplacement()
        {
            string squirrel = UI.FormatAsKeyWord(global::STRINGS.CREATURES.SPECIES.SQUIRREL.NAME);
            Utils.ReplaceLocString(ref BUILDINGS.PREFABS.SQUIRRELGENERATOR.DESC, SQUIRREL, squirrel);
            Utils.ReplaceLocString(ref BUILDINGS.PREFABS.SQUIRRELGENERATOR.EFFECT, SQUIRREL, squirrel);
            Utils.ReplaceLocString(ref OPTIONS.SEARCHWHEELRADIUS.TOOLTIP, SQUIRREL, squirrel);
            Utils.ReplaceLocString(ref OPTIONS.HAPPINESSBONUS.TOOLTIP, SQUIRREL, squirrel);
            Utils.ReplaceLocString(ref OPTIONS.METABOLISMBONUS.TOOLTIP, SQUIRREL, squirrel);
            Utils.ReplaceLocString(ref OPTIONS.GENERATORWATTAGE.TOOLTIP, WATT, UI.UNITSUFFIXES.ELECTRICAL.WATT);
            Utils.ReplaceLocString(ref OPTIONS.SELFHEAT.TOOLTIP, DTU_S, UI.UNITSUFFIXES.HEAT.DTU_S);
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
