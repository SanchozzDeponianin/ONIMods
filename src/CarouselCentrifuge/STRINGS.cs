using STRINGS;

namespace CarouselCentrifuge
{
    public class STRINGS
    {
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class CAROUSELCENTRIFUGE
                {
                    public static LocString NAME = UI.FormatAsLink("Carousel", "CAROUSELCENTRIFUGE");
                    public static LocString DESC = "Funny dizzy entertainment.\nSometimes too dizzy.\nBe prepared for unforeseen consequences.";
                    public static LocString EFFECT = "Allows Duplicants to ride on a Carousel on their breaks.\n\nIncreases Duplicant " + UI.FormatAsLink("Morale", "MORALE") + ".";
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
                    public static LocString TOOLTIP = "This Duplicant ride on a Carousel!\n\nLeisure activities increase Duplicants' " + UI.FormatAsKeyWord("Morale");
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
    }
}
