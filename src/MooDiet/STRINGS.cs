using STRINGS;

namespace MooDiet
{
    public class STRINGS
    {
        public class CREATURES
        {
            public class MODIFIERS
            {
                public class MOOFLOWERFED
                {
                    public static LocString NAME = "Flower Diet";
                    public static LocString TOOLTIP = "";
                }
            }
        }

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
            public class GAS
            {
                public static LocString NAME = "gas multiplier";
                public static LocString TOOLTIP = "";
            }
            public class BECKONING
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord("Accu-moo-lation")} when {UI.FormatAsKeyWord("Gassy Moo")} eats {UI.FormatAsKeyWord("Balm Lily Flowers")}";
                public static LocString TOOLTIP = "Balm Lily is actually a free plant that does not require resources\nIn order to maintain the balance, there should be a penalty";
            }
            public class ZERO
            {
                public static LocString NAME = "Zero";
            }
            public class QUARTER
            {
                public static LocString NAME = "Quarter";
            }
            public class HALF
            {
                public static LocString NAME = "Half";
            }
            public class FULL
            {
                public static LocString NAME = "Full";
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
