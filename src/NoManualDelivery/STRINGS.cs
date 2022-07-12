using System.Collections.Generic;
using STRINGS;
using SanchozzONIMods.Lib;

namespace NoManualDelivery
{
    public class STRINGS
    {
        private const string ALLOWMANUALBUTTON = "{ALLOWMANUALBUTTON}";
        private const string SOLIDTRANSFERARM = "{SOLIDTRANSFERARM}";

        public class OPTIONS
        {
            public class ALLOWALWAYSPICKUPEDIBLE
            {
                public static LocString NAME = "Duplicants can always pick up Food and Medicine.";
                public static LocString TOOLTIP = string.Concat(new string[]
                {
                    "Duplicants will ignore the ",
                    UI.FormatAsKeyWord(ALLOWMANUALBUTTON),
                    " checkbox when picking up food and medicine to eat it."
                });
            }

            public class ALLOWTRANSFERARMPICKUPGASLIQUID
            {
                public static LocString NAME = SOLIDTRANSFERARM + " can pick up and deliver liquid/gas Bottles and Canisters.";
                public static LocString TOOLTIP = string.Concat(new string[]
                {
                    "Also adds the ",
                    UI.FormatAsKeyWord(ALLOWMANUALBUTTON),
                    " checkbox to the Pitcher Pump, Canister Filler, Bottle Emptier and Canister Emptier."
                });
            }
        }

        internal static void DoReplacement()
        {

            var dictionary = new Dictionary<string, string>
            {
                { ALLOWMANUALBUTTON, UI.UISIDESCREENS.AUTOMATABLE_SIDE_SCREEN.ALLOWMANUALBUTTON },
                { SOLIDTRANSFERARM, UI.StripLinkFormatting(BUILDINGS.PREFABS.SOLIDTRANSFERARM.NAME) }
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
        }
    }
}
