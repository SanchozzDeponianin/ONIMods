using System.Collections.Generic;
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
                public static LocString NAME =  $"Destroy the Balloon when the {HASBALLOON} effect has ends";
                public static LocString TOOLTIP = $"If enabled, the Balloon will disappear when the {HASBALLOON} effect ends.\n\nIf disabled, the Balloon does not disappear when the {HASBALLOON} effect ends.\nNice to the eyes, though useless for gameplay.";
            }

            public class FIXEFFECTDURATION
            {
                public static LocString NAME = $"Correct the duration of the {HASBALLOON} effect";
                public static LocString TOOLTIP = $"The Game shows that the {HASBALLOON} effect has a duration of two cycles,\nbut it suddenly disappears after one cycle.\n\nIf enabled, the {HASBALLOON} effect will actually continue for two cycles.\n\nIf disabled, just the mod will make the game show the proper duration of the {HASBALLOON} effect.";
            }
        }

        internal static void DoReplacement()
        {
            var name = UI.FormatAsKeyWord(DUPLICANTS.MODIFIERS.HASBALLOON.NAME);
            Utils.ReplaceAllLocStringTextByDictionary(typeof(OPTIONS), new Dictionary<string, string>(){ {HASBALLOON, name } });
        }
    }
}
