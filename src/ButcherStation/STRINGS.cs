using SanchozzONIMods.Lib;
using STRINGS;
using static STRINGS.UI;

namespace ButcherStation
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

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
                    public static LocString TOOLTIP = $"The fishing hook is not submerged in enough {FormatAsKeyWord("Liquid")}.";
                }
            }
        }
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class BUTCHERSTATION
                {
                    public static LocString NAME = FormatAsLink("Butcher Station", "BUTCHERSTATION");
                    public static LocString DESC = "Grooming critters make them look nice, feel happy... and more yummy.";
                    public static LocString EFFECT = $"Allows the assigned {FormatAsLink("Rancher", "RANCHING1")} to control the population and butchering too old, surplus, or unwanted {FormatAsLink("Critters", "CREATURES")}.\n\nAssigned Duplicants must possess the <link=\"RANCHING1\">Critter Ranching</link> skill.";
                }

                public class FISHINGSTATION
                {
                    public static LocString NAME = FormatAsLink("Fishing Station", "FISHINGSTATION");
                    public static LocString DESC = "Fishing Stations allows to safely fishing " + FormatAsLink("Fishes", "PACU") + " and not soak your feet.";
                    public static LocString EFFECT = $"Allows the assigned {FormatAsLink("Rancher", "RANCHING1")} to control the population and fishing too old, surplus, or unwanted {FormatAsLink("Fishes", "PACU")}.\n\nAssigned Duplicants must possess the <link=\"RANCHING1\">Critter Ranching</link> skill.\n\nA {FormatAsLink("Liquid", "ELEMENTSLIQUID")} depth of {{0}} to {{1}} tiles is required to place a fishing hook.";
                }
            }
        }

        public class DUPLICANTS
        {
            public class ATTRIBUTES
            {
                public class RANCHING
                {
                    public static LocString EFFECTEXTRAMEATMODIFIER = $"{{0}} Extra {FormatAsKeyWord("Meat")} when working at the {FormatAsKeyWord("Butcher")} and {FormatAsKeyWord("Fishing Stations")}";
                }
            }
        }

        public class UI
        {
            public class UISIDESCREENS
            {
                public class BUTCHERSTATIONSIDESCREEN
                {
                    public class WRANGLE_UNSELECTED
                    {
                        public static LocString NAME = "Auto-Wrangle Unwanted";
                        public static LocString TOOLTIP = "A Duplicant will automatically wrangle any critters that do not belong in this stable, that are NOT selected in the filter";
                    }

                    public class WRANGLE_OLD_AGED
                    {
                        public static LocString NAME = "Auto-Wrangle Old Aged";
                        public static LocString TOOLTIP = "A Duplicant will automatically wrangle any critters that are selected in the filter and that their Age are older than the specified value";
                    }

                    public class WRANGLE_SURPLUS
                    {
                        public static LocString NAME = "Auto-Wrangle Surplus";
                        public static LocString TOOLTIP = "A Duplicant will automatically wrangle any critters that are selected in the filter if the total number of critters in this stable exceeds the specified population limit";
                    }

                    public class LEAVE_ALIVE
                    {
                        public static LocString NAME = "Leave critters alive";
                        public static LocString TOOLTIP = "A Duplicant will leave the wrangled critters alive instead of butchering them";
                    }

                    public class AGE_THRESHOLD
                    {
                        public static LocString MIN_MAX = "{0}%";
                        public static LocString PRE = " ";
                        public static LocString PST = "%";
                        public static LocString TOOLTIP = "Critters older than this specified value <b>{0}%</b> will be automatically wrangled";
                        public static LocString TOOLTIP_LIFESPAN = "\n{0} out of {1} cycles lifespan";
                    }

                    public class CREATURE_LIMIT
                    {
                        public static LocString MIN_MAX = "{0}";
                        public static LocString PRE = "Max: ";
                        public static LocString PST = " Critters";
                        public static LocString TOOLTIP = "Critters exceeding this population limit <b>{0}</b> will automatically be wrangled";
                    }

                    public static LocString FILTER_LABEL = "Critters filter:";
                }
            }
        }

        public class OPTIONS
        {
            public class ALLOW_PACIFISTS
            {
                public static LocString NAME = "Allow Pacifist Duplicants to work";
            }

            public class EXTRA_MEAT_PER_RANCHING_ATTRIBUTE
            {
                public static LocString NAME = $"+X% {FormatAsKeyWord("Extra Meat")} from butchered Critters per each {FormatAsKeyWord("Husbandry")} attribute level";
                public static LocString TOOLTIP = $"Set to {FormatAsKeyWord("0")} to disable Extra Meat drop";
            }

            public class MAKE_CENTER_SOLID
            {
                public static LocString NAME = $"Make the middle cell of {FormatAsKeyWord("Fishing Station")} impenetrable";
                public static LocString TOOLTIP = "This will separate Rooms and prevent Items from falling.\nHowever, it looks weird, and Duplicants won't be able to swim there";
            }

            public class MAX_CREATURE_LIMIT
            {
                public static LocString NAME = $"{FormatAsKeyWord("Max Critters")} limit";
                public static LocString TOOLTIP = $"Affects both {FormatAsKeyWord("Butcher")} and {FormatAsKeyWord("Fishing Stations")}\nas well as {FormatAsKeyWord("Critter Drop-Off")} and {FormatAsKeyWord("Fish Release")}";
            }

            public class FILTERED_COUNT
            {
                public static LocString NAME = "Count only the Critters selected in the filter";
                public static LocString TOOLTIP =
$@"If checked, only the Critters selected in the filter will be counted
Similar to the new {FormatAsKeyWord("Critter Drop-Off")} in the U51 version of the game and newer

If unchecked, all Critters in the Room will be counted
Similar to the old {FormatAsKeyWord("Critter Drop-Off")} in the U50 version of the game and older";
            }
        }

        internal static void DoReplacement()
        {
            var text = string.Format(BUILDINGS.PREFABS.FISHINGSTATION.EFFECT.text, FishingStation.MinDepth + 1, FishingStation.MaxDepth);
            BUILDINGS.PREFABS.FISHINGSTATION.EFFECT.ReplaceText(text);
            LocString.CreateLocStringKeys(typeof(BUILDING));
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
            LocString.CreateLocStringKeys(typeof(UI));
            Strings.Add($"STRINGS.MISC.TAGS.{Patches.ButcherableCreature.ToString().ToUpperInvariant()}", MISC.TAGS.BAGABLECREATURE);
            Strings.Add($"STRINGS.MISC.TAGS.{Patches.FisherableCreature.ToString().ToUpperInvariant()}", MISC.TAGS.SWIMMINGCREATURE);
        }
    }
}
