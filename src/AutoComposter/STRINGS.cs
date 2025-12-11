using STRINGS;
using SanchozzONIMods.Lib;

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

        private const string COMPOSTABLE = "{COMPOSTABLE}";
        private const string BUTTON = "{BUTTON}";
        private const string SPECIAL = "{SPECIAL}";

        public class OPTIONS
        {
            public class LABEL
            {
                public static LocString NAME = $"What to do with the {COMPOSTABLE} category ?";
                public static LocString TOOLTIP = $"The {COMPOSTABLE} category was not added by this mod.\nIt has been in the game since 2017 and contains items marked to compost with the {BUTTON} button";
            }
            public class MAKE_SPECIAL
            {
                public static LocString NAME = $"Make it {SPECIAL} in the filter menu";
                public static LocString TOOLTIP = "Move it to bottom in the filter menu of the Storage Bin, etc, like Eggs";
            }
            public class HIDE_FROM_FILTERS
            {
                public static LocString NAME = "Hide it from the filter menu";
                public static LocString TOOLTIP = "Also marked to compost items cannot be stored to Storage Bin, etc";
            }
            public class HIDE_FROM_RESOURCES
            {
                public static LocString NAME = "Hide it from the Resouces screen";
                public static LocString TOOLTIP = "Also unpin if some marked to compost items was pinned";
            }
        }

        internal static void DoReplacement()
        {
            OPTIONS.LABEL.NAME.ReplaceText(COMPOSTABLE, UI.FormatAsKeyWord(MISC.TAGS.COMPOSTABLE.text));
            OPTIONS.LABEL.TOOLTIP.ReplaceText(COMPOSTABLE, UI.FormatAsKeyWord(MISC.TAGS.COMPOSTABLE.text));
            OPTIONS.LABEL.TOOLTIP.ReplaceText(BUTTON, UI.FormatAsKeyWord(UI.USERMENUACTIONS.COMPOST.NAME.text));
            OPTIONS.MAKE_SPECIAL.NAME.ReplaceText(SPECIAL, UI.FormatAsKeyWord(UI.UISIDESCREENS.TREEFILTERABLESIDESCREEN.SPECIAL_RESOURCES.text));
            LocString.CreateLocStringKeys(typeof(BUILDING));
        }
    }
}
