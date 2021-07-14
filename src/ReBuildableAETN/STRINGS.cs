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

        public class ITEMS
        {
            public class MASSIVE_HEATSINK_CORE
            {
                public static LocString NAME = "Neutronium Core";
                public static LocString DESC = string.Concat(
                    "A mysterious thingy, presumably made of refined ",
                    FormatAsLink("Neutronium", "UNOBTANIUM"),
                    ". Very cold to the touch.\n\n",
                    "It is a necessary component for rebuilding the ",
                    MASSIVEHEATSINK
                    );
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
                //public static LocString TOOLTIP = "";
                //public static LocString CATEGORY = "";
            }

            public class CAREPACKAGE
            {
                public static LocString CATEGORY = $"Receiving a {NEUTRONIUM_CORE} from a Care Packages";
            }

            public class ENABLED
            {
                public static LocString NAME = "Enable";
                //public static LocString TOOLTIP = "";
            }

            public class MINCYCLE
            {
                public static LocString NAME = $"Cycles before the {NEUTRONIUM_CORE} will be available";
                //public static LocString TOOLTIP = "";
            }

            public class REQUIREDISCOVERED
            {
                public static LocString NAME = $"{NEUTRONIUM_CORE} must be discovered";
                public static LocString TOOLTIP = $"First you have to obtain the {NEUTRONIUM_CORE} in a different way";
            }

            public class VANILLAPLANET
            {
                public static LocString CATEGORY = $"Receiving a {NEUTRONIUM_CORE} from a Space Planetoids";
            }

            public class ICYDWARFCHANCE
            {
                public static LocString NAME = $"Chance of receiving from the {ICYDWARF}";
                //public static LocString TOOLTIP = "";
            }

            public class ICEGIANTCHANCE
            {
                public static LocString NAME = $"Chance of receiving from the {ICEGIANT}";
                //public static LocString TOOLTIP = "";
            }
        }

        internal static void DoReplacement()
        {
            MISC.STATUSITEMS.MASSIVEHEATSINK_STUDIED.NAME = global::STRINGS.MISC.STATUSITEMS.STUDIED.NAME;
            var dictionary = new Dictionary<string, string>()
            {
                { MASSIVEHEATSINK, PREFABS.MASSIVEHEATSINK.NAME },
                { NEUTRONIUM_CORE, FormatAsKeyWord(ITEMS.MASSIVE_HEATSINK_CORE.NAME) },
                { ICYDWARF, FormatAsKeyWord(SPACEDESTINATIONS.DWARFPLANETS.ICYDWARF.NAME) },
                { ICEGIANT, FormatAsKeyWord(SPACEDESTINATIONS.GIANTS.ICEGIANT.NAME) }
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
            LocString.CreateLocStringKeys(typeof(MISC));
        }
    }
}
