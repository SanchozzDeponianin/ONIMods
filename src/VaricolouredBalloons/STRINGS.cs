using STRINGS;
using SanchozzONIMods.Lib;

namespace VaricolouredBalloons
{
    public class STRINGS
    {
        private const string HASBALLOON = "{HASBALLOON}";

        public class OPTIONS
        {
            public class DESTROYFXAFTEREFFECTEXPIRED
            {
                public static LocString TITLE = string.Concat(new string[]
                {
                    "Destroy the Balloon when ",
                    UI.FormatAsKeyWord(HASBALLOON),
                    " effect has ends."
                });
                public static LocString TOOLTIP = string.Concat(new string[]
                {
                    "If enabled, the Balloon will disappear when ",
                    UI.FormatAsKeyWord(HASBALLOON),
                    " effect ends.\n\n",
                    "If disabled, the Balloon does not disappear when ",
                    UI.FormatAsKeyWord(HASBALLOON),
                    " effect ends.\nNice to the eyes, though useless for gameplay.",
                });
            }
        }

        internal static void DoReplacement()
        {
            Utils.ReplaceLocString(ref OPTIONS.DESTROYFXAFTEREFFECTEXPIRED.TOOLTIP, HASBALLOON, DUPLICANTS.MODIFIERS.HASBALLOON.NAME);
            Utils.ReplaceLocString(ref OPTIONS.DESTROYFXAFTEREFFECTEXPIRED.TITLE, HASBALLOON, DUPLICANTS.MODIFIERS.HASBALLOON.NAME);
        }
    }
}
