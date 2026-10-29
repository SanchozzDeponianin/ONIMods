using SanchozzONIMods.Lib;
using STRINGS;
using static STRINGS.BUILDINGS.PREFABS;

namespace WhereMyLoot
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        public class OPTIONS
        {
            public class PROTECT
            {
                public static LocString CATEGORY = "Prevent unwanted Demolish of these objects";
            }
            public class MEGABRAINTANK
            {
                public static LocString NAME = "";
            }
            public class CREATUREMANIPULATOR
            {
                public static LocString NAME = "";
            }
            public class FOSSILDIG
            {
                public static LocString NAME = "";
            }
            public class MORBROVERMAKER
            {
                public static LocString NAME = "";
            }
            public class HIJACKEDHEADQUARTERS
            {
                public static LocString NAME = "";
            }
            public class GENESHUFFLER
            {
                public static LocString NAME = "";
            }
            public class MASSIVEHEATSINK
            {
                public static LocString NAME = "";
            }
            public class SATELLITES
            {
                public static LocString NAME = "";
            }
        }

        internal static void DoReplacement()
        {
            OPTIONS.MEGABRAINTANK.NAME.ReplaceText(UI.StripLinkFormatting(MEGABRAINTANK.NAME.text));
            OPTIONS.CREATUREMANIPULATOR.NAME.ReplaceText(UI.StripLinkFormatting(GRAVITASCREATUREMANIPULATOR.NAME.text));
            OPTIONS.FOSSILDIG.NAME.ReplaceText(UI.StripLinkFormatting(FOSSILDIG.NAME.text));
            OPTIONS.MORBROVERMAKER.NAME.ReplaceText(UI.StripLinkFormatting(MORBROVERMAKER.NAME.text));
            OPTIONS.HIJACKEDHEADQUARTERS.NAME.ReplaceText(UI.StripLinkFormatting(HIJACKEDHEADQUARTERS.NAME.text));
            OPTIONS.GENESHUFFLER.NAME.ReplaceText(UI.StripLinkFormatting(GENESHUFFLER.NAME.text));
            OPTIONS.MASSIVEHEATSINK.NAME.ReplaceText(UI.StripLinkFormatting(MASSIVEHEATSINK.NAME.text));
            OPTIONS.SATELLITES.NAME.ReplaceText(WORLD_TRAITS.CRASHED_SATELLITES.NAME.text);
        }
    }
}
