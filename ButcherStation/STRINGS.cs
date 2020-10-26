using STRINGS;

namespace ButcherStation
{
    class STRINGS
    {
        public class BUILDING
        {
            public class STATUSITEMS
            {
                public class NOTENOUGHDEPTH
                {
                    public static LocString NAME = "Not enough depth";
                    public static LocString TOOLTIP = "Not enough depth to place a fishing hook, or other buildings interfere.";
                }

                public class NOTENOUGHWATER
                {
                    public static LocString NAME = "Not enough liquid";
                    public static LocString TOOLTIP = $"The fishing hook is not submerged in enough " + global::STRINGS.UI.FormatAsKeyWord("Liquid") + ".";
                }
            }
        }
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class BUTCHERSTATION
                {
                    public static LocString NAME = global::STRINGS.UI.FormatAsLink("Butcher Station", "BUTCHERSTATION");
                    public static LocString DESC = "Grooming critters make them look nice, feel happy... and more yummy.";
                    public static LocString EFFECT = string.Concat(new string[]                   
                    {
                        "Allows the assigned ",
                        global::STRINGS.UI.FormatAsLink("Rancher", "RANCHER"),
                        " to control the population and butchering too old, surplus, or unwanted ",
                        global::STRINGS.UI.FormatAsLink("Critters", "CRITTERS"),
                        ".\n\nAssigned Duplicants must possess the ",
                        global::STRINGS.UI.FormatAsLink("Critter Wrangling", "RANCHER"),
                        " trait."
                    });
                }

                public class FISHINGSTATION
                {
                    public static LocString NAME = global::STRINGS.UI.FormatAsLink("Fishing Station", "FISHINGSTATION");
                    public static LocString DESC = "Fishing Stations allows to safely fishing " + global::STRINGS.UI.FormatAsLink("Pacu", "PACU") + " and not soak your feet.";
                    public static LocString EFFECT = string.Concat(new string[]
                    {
                        "Allows the assigned ",
                        global::STRINGS.UI.FormatAsLink("Rancher", "RANCHER"),
                        " to control the population and fishing too old, surplus, or unwanted ",
                        global::STRINGS.UI.FormatAsLink("Fishes", "PACU"),
                        ".\n\nAssigned Duplicants must possess the ",
                        global::STRINGS.UI.FormatAsLink("Critter Wrangling", "RANCHER"),
                        " trait.\n\nA ",
                        global::STRINGS.UI.FormatAsLink("Liquid", "ELEMENTSLIQUID"),
                        " depth of 2 to 4 tiles is required to place a fishing hook."
                    });
                }
            }
        }

        public class DUPLICANTS
        {
            public class ATTRIBUTES
            {
                public class RANCHING
                {
                    public static LocString EFFECTEXTRAMEATMODIFIER = string.Concat(new string[]
                        {
                        "{0} Extra ",
                        global::STRINGS.UI.FormatAsKeyWord("Meat"),
                        " when working at the ",
                        global::STRINGS.UI.FormatAsKeyWord("Butcher"),
                        " and ",
                        global::STRINGS.UI.FormatAsKeyWord("Fishing Stations")
                        });
                }
            }
        }

        public class UI
        {
            public class UISIDESCREENS
            {
                public class BUTCHERSTATIONSIDESCREEN
                {
                    public static LocString TITLE = "Creature Age Threshold";
                    public static LocString TOOLTIP = string.Concat(new string[]
                    {
                        "A Duplicant will automatically wrangle selected ",
                        global::STRINGS.UI.FormatAsKeyWord("Critters"),
                        ", if their ",
                        global::STRINGS.UI.FormatAsKeyWord("Age"),
                        " is older than the specified value <b>{0}%</b>"
                    });
                    public static LocString TOOLTIP_OUTOF = " out of ";
                    public static LocString TOOLTIP_CYCLES = " cycles";
                }
            }
        }
    }
}
