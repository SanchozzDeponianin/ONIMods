using System.Collections.Generic;
using SanchozzONIMods.Lib;
using static STRINGS.BUILDING;
using static STRINGS.BUILDINGS;
using static STRINGS.UI;

namespace OilWellCapBugFix
{
    public class STRINGS
    {
        private const string OILWELLCAP = "{OILWELLCAP}";
        private const string EMPTYPIPE = "{EMPTYPIPE}";

        public class OPTIONS
        {
            public class ALLOWDEPRESSURIZEWHENOUTOFWATER
            {
                public static LocString NAME = $"Allow depressurization when {OILWELLCAP} is out of Water";
                public static LocString TOOLTIP = $"If enabled, Duplicants can depressurize {OILWELLCAP} if it is out of Water" +
                                                  $"\nBut {OILWELLCAP} will never show \"{EMPTYPIPE}\" status";
            }
        }

        internal static void DoReplacement()
        {
            var dictionary = new Dictionary<string, string>()
            {
                { OILWELLCAP, FormatAsKeyWord(PREFABS.OILWELLCAP.NAME) },
                { EMPTYPIPE, FormatAsKeyWord(STATUSITEMS.LIQUIDPIPEEMPTY.NAME) },
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
        }
    }
}
