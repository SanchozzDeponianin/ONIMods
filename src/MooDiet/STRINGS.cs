using STRINGS;

namespace MooDiet
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        public class OPTIONS
        {
            public class FLOWER_DIET
            {
                public static LocString CATEGORY = "Balm Lily Flower Diet";
            }
            public class LILY_PER_COW
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord("Balm Lily")} plants per one {UI.FormatAsKeyWord("Gassy Moo")}";
                public static LocString TOOLTIP = "How many plants do you need to plant to feed one tamed happy Cow\nThe consumed mass per cycle is calculated automatically depending on the yield of the plant";
            }
            public class EAT_LILY
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord("Gassy Moo")} can eat { UI.FormatAsKeyWord("Balm Lily")} live plants";
            }
            public class EAT_FLOWER
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord("Gassy Moo")} can eat { UI.FormatAsKeyWord("Balm Lily Flowers")}";
            }
            public class PALMERA_DIET
            {
                public static LocString CATEGORY = "Palmera Tree Diet";
            }
            public class PALMERA_LABEL
            {
                public static LocString NAME = $"Only if the {UI.FormatAsKeyWord("Palmera Tree")} mod is installed";
            }
            public class PALMERA_BUTTON
            {
                public static LocString NAME = "Browse Steam Workshop";
            }
            public class PALMERA_PER_COW
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord("Palmera Tree")} plants per one {UI.FormatAsKeyWord("Gassy Moo")}";
                public static LocString TOOLTIP = "";
            }
            public class EAT_PALMERA
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord("Gassy Moo")} can eat { UI.FormatAsKeyWord("Palmera Tree")} live plants";
            }
            public class EAT_BERRY
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord("Gassy Moo")} can eat { UI.FormatAsKeyWord("Palmera Berries")}";
            }
        }

        internal static void DoReplacement()
        {
            OPTIONS.PALMERA_PER_COW.TOOLTIP = OPTIONS.LILY_PER_COW.TOOLTIP;
        }
    }
}
