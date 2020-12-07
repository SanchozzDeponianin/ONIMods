using STRINGS;

namespace MoreTinkerablePlants
{
    public class STRINGS
    {
        public class DUPLICANTS
        {
            public class MODIFIERS
            {
                public class FARMTINKER
                {
                    public static LocString ADDITIONAL_EFFECTS = string.Concat(new string[]
                    {
                        "Increases the ",
                        UI.FormatAsKeyWord("Throughput"),
                        " of ",
                        UI.FormatAsLink("Oxyfern", "OXYFERN"),
                        " and ",
                        UI.FormatAsLink("Wheezewort", "COLDBREATHER")
                    });
                }
            }
        }

        public class OPTIONS
        {
            public class COLDBREATHER_MULTIPLIER
            {
                public static LocString TITLE = "";
                public static LocString TOOLTIP = "";
            }
            public class OXYFERN_MULTIPLIER
            {
                public static LocString TITLE = "";
                public static LocString TOOLTIP = "";
            }
        }

        internal static void DoReplacement()
        {
            LocString.CreateLocStringKeys(typeof(DUPLICANTS));
        }
    }
}
