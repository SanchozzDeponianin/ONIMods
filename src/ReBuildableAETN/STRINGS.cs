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

        public class OPTIONS
        {
            public class ADD_LOGIC_PORT
            {
                public static LocString TITLE = $"Add Logic Port to the {MASSIVEHEATSINK}";
            }

            public class CARE_PACKAGES
            {
                public class ENABLED
                {
                    public static LocString TITLE = "Enable";
                }

                public class MIN_CYCLE
                {
                    public static LocString TITLE = $"Cycles before the {NEUTRONIUM_CORE} will be available";
                }

                public class REQUIRE_DISCOVERED
                {
                    public static LocString TITLE = $"{NEUTRONIUM_CORE} must be discovered";
                    public static LocString TOOLTIP = $"First you have to obtain the {NEUTRONIUM_CORE} in a different way";
                }

                public static LocString TITLE = $"Receiving a {NEUTRONIUM_CORE} from a Care Packages";
            }
        }

        internal static void DoReplacement()
        {
            MISC.STATUSITEMS.MASSIVEHEATSINK_STUDIED.NAME = global::STRINGS.MISC.STATUSITEMS.STUDIED.NAME;
            var dictionary = new Dictionary<string, string>()
            {
                { MASSIVEHEATSINK, PREFABS.MASSIVEHEATSINK.NAME },
                { NEUTRONIUM_CORE, FormatAsKeyWord(ITEMS.MASSIVE_HEATSINK_CORE.NAME) }
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
            LocString.CreateLocStringKeys(typeof(MISC));
        }
    }
}
