using STRINGS;

namespace AutoComposter
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        public class BUILDING
        {
            public class STATUSITEMS
            {
                public class COMPOSTACCEPTSMUTANTSEEDS
                {
                    public static LocString NAME = "Compost accepts mutant seeds";
                    public static LocString TOOLTIP = $"This Compost is allowed to use {UI.FormatAsKeyWord("Mutant Seeds")} as compostable";
                }
            }
        }
    }
}
