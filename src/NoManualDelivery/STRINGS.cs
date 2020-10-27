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
                public static LocString TITLE = "Duplicants can always picking up Food and Medicine.";
                public static LocString TOOLTIP = string.Concat(new string[]
                {
                    "Duplicants will ignore the ",
                    UI.FormatAsKeyWord(ALLOWMANUALBUTTON),
                    " checkbox when picking up food and medicine to eat it."
                });
            }

            public class ALLOWTRANSFERARMPICKUPGASLIQUID
            {
                public static LocString TITLE = SOLIDTRANSFERARM + " can pick up and deliver liquid/gas Bottles and Canisters.";
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
            Utils.ReplaceLocString(ref OPTIONS.ALLOWALWAYSPICKUPEDIBLE.TOOLTIP, ALLOWMANUALBUTTON, UI.UISIDESCREENS.AUTOMATABLE_SIDE_SCREEN.ALLOWMANUALBUTTON);
            Utils.ReplaceLocString(ref OPTIONS.ALLOWTRANSFERARMPICKUPGASLIQUID.TITLE, SOLIDTRANSFERARM, UI.StripLinkFormatting(BUILDINGS.PREFABS.SOLIDTRANSFERARM.NAME));
            Utils.ReplaceLocString(ref OPTIONS.ALLOWTRANSFERARMPICKUPGASLIQUID.TOOLTIP, ALLOWMANUALBUTTON, UI.UISIDESCREENS.AUTOMATABLE_SIDE_SCREEN.ALLOWMANUALBUTTON);
        }
    }
}
