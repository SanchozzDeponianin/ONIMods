using System.Collections.Generic;
using SanchozzONIMods.Lib;
using static STRINGS.BUILDINGS;
using static STRINGS.UI;

namespace ReBuildableAETN
{
    public class STRINGS
    {
        private const string MASSIVEHEATSINK = "{MASSIVEHEATSINK}";
        private const string NEUTRONIUM_CORE = "{NEUTRONIUM_CORE}";
        private const string ICYDWARF = "{ICYDWARF}";
        private const string ICEGIANT = "{ICEGIANT}";
        private const string DIRECTOR_DESK = "{DIRECTOR_DESK}";
        private const string SATELLITE3 = "{SATELLITE3}";
        private const string SETLOCKER = "{SETLOCKER}";
        private const string VENDINGMACHINE = "{VENDINGMACHINE}";

        public class ITEMS
        {
            public class MASSIVE_HEATSINK_CORE
            {
                public static LocString NAME = "Neutronium Core";
                public static LocString DESC = $"A mysterious thingy, presumably made of refined {FormatAsLink("Neutronium", "UNOBTANIUM")}. Very cold to the touch.\n\nIt is a necessary component for rebuilding the {MASSIVEHEATSINK}";
            }
        }

        public class MISC
        {
            public class STATUSITEMS
            {
                public class MASSIVEHEATSINK_STUDIED
                {
                    public static LocString NAME = "";
                    public static LocString TOOLTIP = $"Now this Natural Feature can be deconstructed to obtain {NEUTRONIUM_CORE}.";
                }
            }

            public class TAGS
            {
                public static LocString BUILDINGNEUTRONIUMCORE = "";
                public static LocString BUILDINGNEUTRONIUMCORE_DESC = "";
            }
        }

        public class UI
        {
            public class SPACEARTIFACTS
            {
                public class ARTIFACTTIERS
                {
                    public static LocString TIER_CORE = "Something Very Cold";
                }
            }
        }

        public class OPTIONS
        {
            public class ADDLOGICPORT
            {
                public static LocString NAME = $"Add Logic Port to the {MASSIVEHEATSINK}";
            }

            public class CAREPACKAGE
            {
                public static LocString CATEGORY = $"Obtaining a {NEUTRONIUM_CORE} from a Care Packages";
            }

            public class ENABLED
            {
                public static LocString NAME = "Enable";
            }

            public class MINCYCLE
            {
                public static LocString NAME = $"Cycles before the {NEUTRONIUM_CORE} will be available";
            }

            public class REQUIREDISCOVERED
            {
                public static LocString NAME = $"{NEUTRONIUM_CORE} must be discovered";
                public static LocString TOOLTIP = $"First you have to obtain the {NEUTRONIUM_CORE} in a different way";
            }

            public class GRAVITASPOICHANCE
            {
                public static LocString CATEGORY = $"Obtaining a {NEUTRONIUM_CORE} when rummage some objects";
            }

            public class RAREPOICHANCE
            {
                public static LocString NAME = $"A chance to obtain from the {DIRECTOR_DESK} and the {SATELLITE3}";
            }

            public class LOCKERPOICHANCE
            {
                public static LocString NAME = $"A chance to obtain from the {SETLOCKER} and the {VENDINGMACHINE}";
            }

            public class VANILLAPLANETCHANCE
            {
                public static LocString CATEGORY = $"Obtaining a {NEUTRONIUM_CORE} from a Space Planetoids";
            }

            public class ICYDWARFCHANCE
            {
                public static LocString NAME = $"A chance to obtain from the {ICYDWARF}";
            }

            public class ICEGIANTCHANCE
            {
                public static LocString NAME = $"A chance to obtain from the {ICEGIANT}";
            }

            public class SPACEOUTPOICHANCE
            {
                public static LocString CATEGORY = $"Obtaining a {NEUTRONIUM_CORE} from a Space POIs";
            }

            public class SPACEPOICHANCE
            {
                public static LocString NAME = "A chance to obtain from a Space POIs";
                public static LocString TOOLTIP = "The new chance value takes effect on further visits";
            }
        }

        internal static void DoReplacement()
        {
            MISC.STATUSITEMS.MASSIVEHEATSINK_STUDIED.NAME = global::STRINGS.MISC.STATUSITEMS.STUDIED.NAME;
            MISC.TAGS.BUILDINGNEUTRONIUMCORE = ITEMS.MASSIVE_HEATSINK_CORE.NAME;
            MISC.TAGS.BUILDINGNEUTRONIUMCORE_DESC.ReplaceText(ITEMS.MASSIVE_HEATSINK_CORE.DESC.text.Split('\n')[0]);
            var dictionary = new Dictionary<string, string>()
            {
                { MASSIVEHEATSINK, PREFABS.MASSIVEHEATSINK.NAME },
                { NEUTRONIUM_CORE, FormatAsKeyWord(ITEMS.MASSIVE_HEATSINK_CORE.NAME) },
                { ICYDWARF, FormatAsKeyWord(SPACEDESTINATIONS.DWARFPLANETS.ICYDWARF.NAME) },
                { ICEGIANT, FormatAsKeyWord(SPACEDESTINATIONS.GIANTS.ICEGIANT.NAME) },
                { DIRECTOR_DESK, FormatAsKeyWord(PREFABS.PROPFACILITYDESK.NAME) },
                { SATELLITE3, FormatAsKeyWord(PREFABS.PROPSURFACESATELLITE3.NAME) },
                { SETLOCKER, FormatAsKeyWord(PREFABS.SETLOCKER.NAME) },
                { VENDINGMACHINE, FormatAsKeyWord(PREFABS.VENDINGMACHINE.NAME) }
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
            LocString.CreateLocStringKeys(typeof(MISC));
        }
    }
}
