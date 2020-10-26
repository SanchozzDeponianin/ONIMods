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
    }
}
