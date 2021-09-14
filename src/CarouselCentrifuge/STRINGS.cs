using System.Collections.Generic;
using STRINGS;
using SanchozzONIMods.Lib;

namespace CarouselCentrifuge
{
    public class STRINGS
    {
        private const string MORALE = "{Morale}";

        public class BUILDINGS
        {
            public class PREFABS
            {
                public class CAROUSELCENTRIFUGE
                {
                    public static LocString NAME = UI.FormatAsLink("Carousel", "CAROUSELCENTRIFUGE");
                    public static LocString DESC = "Funny dizzy entertainment.\nSometimes too dizzy.\nBe prepared for unforeseen consequences.";
                    public static LocString EFFECT = "Allows Duplicants to ride on a Carousel on their breaks.\n\nIncreases Duplicant " + UI.FormatAsLink(MORALE, "MORALE") + ".";
                }
            }
        }

        public class DUPLICANTS
        {
            public class MODIFIERS
            {
                public class RIDEONCAROUSEL
                {
                    public static LocString NAME = "Ride on a Carousel";
                    public static LocString TOOLTIP = "This Duplicant ride on a Carousel!\n\nLeisure activities increase Duplicants' " + UI.FormatAsKeyWord(MORALE);
                }
            }

            public class STATUSITEMS
            {
                public class CAROUSELVOMITING
                {
                    public static LocString NAME = "Dizziness vomiting";
                    public static LocString TOOLTIP = string.Concat(new string[]
                    {
                        "This Duplicant has unceremoniously hurled as the result of a Dizziness",
                        UI.HORIZONTAL_BR_RULE,
                        "Duplicant-related \"spills\" can be cleaned up using the ",
                        UI.FormatAsKeyWord("Mop Tool"),
                        " ",
                        UI.FormatAsHotkey("[M]")
                    });

                    public static LocString NOTIFICATION_NAME = "Unforeseen Consequences";
                    public static LocString NOTIFICATION_TOOLTIP = string.Concat(new string[]
                    {
                        "The ",
                        UI.FormatAsTool("Mop Tool", "[M]"),
                        " can used to clean up Duplicant-related \"spills\"",
                        UI.HORIZONTAL_BR_RULE,
                        "The entertainment was too dizzy. These Duplicantes threw up:"
                    });
                }
            }
        }

        public class OPTIONS
        {
            public class DIZZINESSCHANCEPERCENT
            {
                public static LocString NAME = $"Chance of {UI.FormatAsKeyWord("Unforeseen consequences")}";
                public static LocString TOOLTIP = $"Set to {UI.FormatAsPositiveRate("0")} to disable {UI.FormatAsKeyWord("Unforeseen consequences")}";
            }
            public class MORALEBONUS
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord(MORALE)} bonus";
            }
            public class SPECIFICEFFECTDURATION
            {
                public static LocString NAME = $"Duration of the {UI.FormatAsKeyWord("Ride on a Carousel")} effect";
            }
        }

        internal static void DoReplacement()
        {
            var dictionary = new Dictionary<string, string>
            {
                { MORALE, global::STRINGS.DUPLICANTS.ATTRIBUTES.QUALITYOFLIFE.NAME }
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
