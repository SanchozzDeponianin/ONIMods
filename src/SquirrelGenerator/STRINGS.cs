using STRINGS;

namespace SquirrelGenerator
{
    public class STRINGS
    {
        public const string SQUIRREL = "{Squirrel}";

        public class BUILDINGS
        {
            public class PREFABS
            {
                public class SQUIRRELGENERATOR
                {
                    public static LocString NAME = UI.FormatAsLink("Squirrel Wheel", "SQUIRRELGENERATOR");
                    public static LocString DESC = $"Watching {SQUIRREL} run on it is adorable... the electrical power is just an added bonus.";
                    public static LocString EFFECT = string.Concat(new string[]
                    {
                        "Converts a creature’s muscular labor into electrical ",
                        UI.FormatAsLink("Power", "POWER"),
                        ".\n\n",
                        $"A {SQUIRREL} is happier when it runs in a wheel, but consumes more food.",
                    });
                }
            }
        }

        public class CREATURES
        {
            public class MODIFIERS
            {
                public class RUN_IN_WHEEL
                {
                    public static LocString NAME = "Running in a Wheel";
                    public static LocString TOOLTIP = string.Concat(new string[]
                    {
                        "This critter is running in a ",
                        UI.FormatAsKeyWord("Squirrel Wheel"),
                        " and generate ",
                        UI.FormatAsKeyWord("Power"),
                    });
                }
            }

            public class STATUSITEMS
            {
                public class EXCITED_TO_RUN_IN_WHEEL
                {
                    public static LocString NAME = "Interested";
                    public static LocString TOOLTIP = "This creature has discovered an entertaining mechanical thing and is very interested";
                }
            }
        }
    }
}
