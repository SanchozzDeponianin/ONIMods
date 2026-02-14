using System.Collections.Generic;
using System.Linq;
using STRINGS;
using SanchozzONIMods.Lib;

namespace NoManualDelivery
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        public const string STAR = "\u2605";
        private const string ALLOWMANUALBUTTON = "{ALLOWMANUALBUTTON}";
        private const string SOLIDTRANSFERARM = "{SOLIDTRANSFERARM}";
        private const string ICEKETTLE = "{ICEKETTLE}";

        public class OPTIONS
        {
            public class ALLOWALWAYSPICKUPEDIBLE
            {
                public static LocString NAME = "Duplicants can always pick up Food and Medicine";
                public static LocString TOOLTIP = $"Duplicants will ignore the {ALLOWMANUALBUTTON} checkbox when picking up food and medicine to eat it.";
            }

            public class ALLOWALWAYSPICKUPKETTLE
            {
                public static LocString NAME = "Duplicants can always pick up Water from {ICEKETTLE}";
                public static LocString TOOLTIP = $"Duplicants will ignore the {ALLOWMANUALBUTTON} checkbox when picking up Water from {ICEKETTLE}.";
            }

            public class ALLOWTRANSFERARMPICKUPGASLIQUID
            {
                public static LocString NAME = SOLIDTRANSFERARM + " can pick up and deliver liquid/gas Bottles and Canisters";
                public static LocString TOOLTIP = $"Also adds the {ALLOWMANUALBUTTON} checkbox to the Pitcher Pump, Canister Filler, Bottle Emptier and Canister Emptier.";
            }
            public class HOLDMODE
            {
                public static LocString CATEGORY = $"{STAR} Intellectual mode";
            }
            public class CHORES
            {
                public static LocString NAME = $"Enable for Buildings";
                public static LocString TOOLTIP = $"Additional the {ALLOWMANUALBUTTON} checkbox mode with {STAR}.\nDuplicants will deliver items only if the {SOLIDTRANSFERARM} can't do it in a certain time.";
            }
            public class BYDEFAULT
            {
                public static LocString NAME = "Turn it on for newly built Buildings";
            }
            public class ITEMS
            {
                public static LocString NAME = "Enable for Items on the floor";
                public static LocString TOOLTIP = $"Duplicants will pick up Items on the floor only if the {SOLIDTRANSFERARM} can't do it in a certain time.";
            }
            public class TIMEOUT
            {
                public static LocString NAME = "Timeout";
            }
        }
        public static LocString AUTOMATABLE_TOOLTIP = "Allow only if the {SOLIDTRANSFERARM} can't do it in a certain time";

        internal static void DoReplacement()
        {
            var dictionary = new Dictionary<string, string>
            {
                { ALLOWMANUALBUTTON,    UI.UISIDESCREENS.AUTOMATABLE_SIDE_SCREEN.ALLOWMANUALBUTTON },
                { SOLIDTRANSFERARM,     BUILDINGS.PREFABS.SOLIDTRANSFERARM.NAME },
                { ICEKETTLE,            BUILDINGS.PREFABS.ICEKETTLE.NAME },
            };
            foreach (var k in dictionary.Keys.ToList())
                dictionary[k] = UI.FormatAsKeyWord(UI.StripLinkFormatting(dictionary[k]));
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
        }
    }
}
