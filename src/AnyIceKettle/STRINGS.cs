using SanchozzONIMods.Lib;
using static STRINGS.ELEMENTS;

namespace AnyIceKettle
{
    public class STRINGS
    {
        public class OPTIONS
        {
            public class LABEL
            {
                public static LocString NAME = "Allow melting of some non-Ice stuff";
            }
            public class MELT_RESIN
            {
                public static LocString NAME = "";
            }
            public class MELT_GUNK
            {
                public static LocString NAME = "";
            }
            public class MELT_PHYTOOIL
            {
                public static LocString NAME = "";
            }
        }

        internal static void DoReplacement()
        {
            OPTIONS.MELT_RESIN.NAME.ReplaceText(SOLIDRESIN.NAME);
            OPTIONS.MELT_GUNK.NAME.ReplaceText(GUNK.NAME);
            OPTIONS.MELT_PHYTOOIL.NAME.ReplaceText(FROZENPHYTOOIL.NAME);
        }
    }
}
