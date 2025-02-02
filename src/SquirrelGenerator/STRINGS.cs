using System.Collections.Generic;
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

        public class MISC
        {
            public class TAGS
            {
                public static LocString SEED_DESC = $"Seeds can be used not only to grow {UI.FormatAsLink("Plants", "PLANTS")}, but also to attract the attention of some {UI.FormatAsLink("Creatures", "SQUIRREL")}.";
            }
        }

        public class OPTIONS
        {
            public class GENERATORWATTAGERATING
            {
                public static LocString NAME = "Wattage";
                public static LocString TOOLTIP = $"Maximum {UI.FormatAsKeyWord("Wattage")},{WATT} of Squirrel Wheel";
            }
            public class SELFHEATWATTS
            {
                public static LocString NAME = "Heat Production";
                public static LocString TOOLTIP = $"{UI.FormatAsKeyWord("Heat")} Production,{DTU_S} of Squirrel Wheel";
            }
            public class HAPPINESSBONUS
            {
                public static LocString NAME = "Happiness Bonus";
                public static LocString TOOLTIP = $"{UI.FormatAsKeyWord("Happiness")} Bonus value when the {SQUIRREL} running in the Wheel";
            }
            public class METABOLISMBONUS
            {
                public static LocString NAME = "Metabolism Bonus";
                public static LocString TOOLTIP = $"{UI.FormatAsKeyWord("Metabolism")} Bonus value when the {SQUIRREL} running in the Wheel";
            }
            public class SEARCHWHEELRADIUS
            {
                public static LocString NAME = "Search Radius";
                public static LocString TOOLTIP = $"Radius in cells in which the {SQUIRREL} is looking for a Wheel";
            }
            public class SEARCHMININTERVAL
            {
                public static LocString NAME = "Search Min Interval";
                public static LocString TOOLTIP = $"Time in seconds until next Wheel search is random value between {UI.FormatAsKeyWord("Search Min Interval")} and {UI.FormatAsKeyWord("Search Max Interval")}";
            }
            public class SEARCHMAXINTERVAL
            {
                public static LocString NAME = "Search Max Interval";
                public static LocString TOOLTIP = "";
            }
        }

        internal static void DoReplacement()
        {
            var dictionary = new Dictionary<string, string>
            {
                { SQUIRREL, UI.FormatAsKeyWord(global::STRINGS.CREATURES.SPECIES.SQUIRREL.NAME) },
                { WATT, UI.UNITSUFFIXES.ELECTRICAL.WATT },
                { DTU_S, UI.UNITSUFFIXES.HEAT.DTU_S }
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
            OPTIONS.SEARCHMAXINTERVAL.TOOLTIP = OPTIONS.SEARCHMININTERVAL.TOOLTIP;
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
            LocString.CreateLocStringKeys(typeof(MISC));
        }
    }
}
