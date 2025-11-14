using static STRINGS.BUILDINGS.PREFABS;
using static STRINGS.UI;
using SanchozzONIMods.Lib;

namespace DualDiningTable
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        public class BUILDINGS
        {
            public class PREFABS
            {
                public class DUALMINIONDININGTABLE
                {
                    public static LocString NAME = FormatAsLink("Rendezvous Table", nameof(DUALMINIONDININGTABLE));
                    public static LocString DESC = "";
                    public static LocString EFFECT = "Gives two Duplicants a place to eat.";
                }
            }
        }

        internal static void DoReplacement()
        {
            BUILDINGS.PREFABS.DUALMINIONDININGTABLE.DESC.ReplaceText(MULTIMINIONDININGTABLE.DESC.text);
            var text = MULTIMINIONDININGTABLE.EFFECT.text;
            text = text.Contains("\n\n") ? text.Substring(text.IndexOf("\n\n")) : string.Empty;
            text = BUILDINGS.PREFABS.DUALMINIONDININGTABLE.EFFECT.text + text;
            BUILDINGS.PREFABS.DUALMINIONDININGTABLE.EFFECT.ReplaceText(text);
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
