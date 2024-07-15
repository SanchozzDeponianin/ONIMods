using System.Collections.Generic;
using System.Linq;
using STRINGS;
using SanchozzONIMods.Lib;

namespace NoManualDelivery
{
    public class STRINGS
    {
        private const string ALLOWMANUALBUTTON = "{ALLOWMANUALBUTTON}";
        private const string SOLIDTRANSFERARM = "{SOLIDTRANSFERARM}";
        private const string ICEKETTLE = "{ICEKETTLE}";

        public class OPTIONS
        {
            public class ALLOWALWAYSPICKUPEDIBLE
            {
                public static LocString NAME = "Duplicants can always pick up Food and Medicine.";
                public static LocString TOOLTIP = $"Duplicants will ignore the {ALLOWMANUALBUTTON} checkbox when picking up food and medicine to eat it.";
            }

            public class ALLOWALWAYSPICKUPKETTLE
            {
                public static LocString NAME = "Duplicants can always pick up Water from {ICEKETTLE}.";
                public static LocString TOOLTIP = $"Duplicants will ignore the {ALLOWMANUALBUTTON} checkbox when picking up Water from {ICEKETTLE}.";
            }

            public class ALLOWTRANSFERARMPICKUPGASLIQUID
            {
                public static LocString NAME = SOLIDTRANSFERARM + " can pick up and deliver liquid/gas Bottles and Canisters.";
                public static LocString TOOLTIP = $"Also adds the {ALLOWMANUALBUTTON} checkbox to the Pitcher Pump, Canister Filler, Bottle Emptier and Canister Emptier.";
            }
        }

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
