using System.Collections.Generic;
using STRINGS;
using SanchozzONIMods.Lib;

namespace VirtualPlanetarium
{
    public class STRINGS
    {
        private const string MORALE = "{MORALE}";
        private const string DATABANK = "{DATABANK}";

        public class BUILDINGS
        {
            public class PREFABS
            {
                public class VIRTUALPLANETARIUM
                {
                    public static LocString NAME = UI.FormatAsLink("Virtual Planetarium", "VIRTUALPLANETARIUM");
                    public static LocString DESC = "Looking at distant stars in comfortable conditions without taking your ass off a comfy chair is cool.";
                    public static LocString EFFECT = $"Allows duplicants to virtually look at the stars.\nTo work properly, required a {UI.FormatAsLink(DATABANK, "RESEARCH_DATABANK")}\nIncreases Duplicant {UI.FormatAsLink(MORALE, "MORALE")}.";
                    public static LocString DATABANK_DESC = $"A cartridge with various data obtained using the {UI.FormatAsLink("Telescope", "CLUSTERTELESCOPE")}.\n\nIt is necessary for the {UI.FormatAsLink("Virtual Planetarium", "VIRTUALPLANETARIUM")} to work.";
                }
            }
        }

        public class DUPLICANTS
        {
            public class MODIFIERS
            {
                public class STARGAZING
                {
                    public static LocString NAME = "Stargazing";
                    public static LocString TOOLTIP = "This Duplicant is watching the stars in the Virtual Planetarium";
                }
                public class STARGAZED
                {
                    public static LocString NAME = "Stargazing";
                    public static LocString TOOLTIP = $"This Duplicant recently watched the stars in the Virtual Planetarium\n\nLeisure activities increase Duplicants' {UI.FormatAsKeyWord(MORALE)}";
                }
            }
        }

        public class OPTIONS
        {
            public class MORALEBONUS
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord(MORALE)} bonus";
            }
            public class SPECIFICEFFECTDURATION
            {
                public static LocString NAME = $"Duration of the {UI.FormatAsKeyWord("Stargazing")} effect";
            }
            public class STRESSDELTA
            {
                public static LocString NAME = "Stress recovery during use, % per day";
            }
        }

        internal static void DoReplacement()
        {
            var dictionary = new Dictionary<string, string>
            {
                { MORALE, global::STRINGS.DUPLICANTS.ATTRIBUTES.QUALITYOFLIFE.NAME },
                { DATABANK, ITEMS.INDUSTRIAL_PRODUCTS.RESEARCH_DATABANK.NAME },
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
            ITEMS.INDUSTRIAL_PRODUCTS.RESEARCH_DATABANK.DESC = BUILDINGS.PREFABS.VIRTUALPLANETARIUM.DATABANK_DESC;
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
