namespace AttributeRestrictions
{
    public class STRINGS
    {
        public class DUPLICANTS
        {
            public class CHORES
            {
                public class PRECONDITIONS
                {
                    public static LocString IS_SUFFICIENT_ATTRIBUTE_LEVEL = "Not allowed by Attribute Restrictions";
                }
            }
        }

        public class UI
        {
            public class UISIDESCREENS
            {
                public class ATTRIBUTE_RESTRICTION_SIDESCREEN
                {
                    public static LocString TITLE = "Attribute Restrictions";

                    public class IS_ENABLE
                    {
                        public static LocString NAME = "Allow to operate this Building when\nthe {0} attribute is";
                        public static LocString TOOLTIP = "Affect to Operate errands.\nDoes not affect, when the Duplicant has already started to perform this errand.\nDoes not affect to Supply, Repair, Disinfect, etc errands.";
                    }

                    public class REQUIRED_LEVEL
                    {
                        public static LocString MIN_MAX = "{0}";
                        public static LocString PRE = " ";
                        public static LocString PST = " ";
                        public static LocString TOOLTIP = "";
                    }
                }
            }
        }
        /*
        public class OPTIONS
        {
        }*/

        internal static void DoReplacement()
        {
            LocString.CreateLocStringKeys(typeof(UI));
        }
    }
}
