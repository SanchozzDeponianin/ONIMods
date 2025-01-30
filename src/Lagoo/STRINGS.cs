using static STRINGS.UI;

namespace Lagoo
{
    public class STRINGS
    {
        private const string squirrel = "SQUIRREL";
        public class CREATURES
        {
            public class SPECIES
            {
                public class SQUIRREL
                {
                    public class VARIANT_LAGOO
                    {
                        public class BABY
                        {
                            public static LocString NAME = FormatAsLink("Lagoonie", squirrel);
                            public static LocString DESC = $"A little purring Lagoonie.\n\nIn time it will mature into a fully grown {FormatAsLink("Lagoo", "SQUIRREL")}.";
                        }

                        public static LocString NAME = FormatAsLink("Lagoo", squirrel);
                        public static LocString DESC = $"Lagoo is a deeply mutated Pip, adapted to survive in a harsh cold climate. Sometimes It can even devour very {FormatAsLink("inedible things", "SULFUR")}.\n\nLagoo knows a lot about warm hugs and is ready to share its warmth with Duplicants.";

                        public static LocString EGG_NAME = FormatAsLink("Lagoo Egg", squirrel);
                        public static LocString EGG_DESC = $"This is not a Dirt. It's a {FormatAsLink("Lagoo Egg", squirrel)}.";
                    }
                }
            }
        }
        public class OPTIONS
        {
            public class WARM_TOUCH_DURATION
            {
                public static LocString NAME = $"{FormatAsKeyWord("Frost Resistant")} effect duration after hugging, cycles";
            }
        }

        internal static void DoReplacement()
        {
            LocString.CreateLocStringKeys(typeof(CREATURES));
        }
    }
}
