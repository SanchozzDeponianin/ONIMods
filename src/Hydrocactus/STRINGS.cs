using STRINGS;

namespace Hydrocactus
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        public class OPTIONS
        {
            public class YIELD_AMOUNT
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord("Hydrocactus")} yield amount, kg of {UI.FormatAsKeyWord("Water")}";
            }
            public class CAREPACKAGE_SEEDS_AMOUNT
            {
                public static LocString NAME = $"The number of {UI.FormatAsKeyWord("Hydrocactus")} seeds in the Care Package";
            }
        }
    }
}
