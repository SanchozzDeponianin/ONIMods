using STRINGS;

namespace ExoticSpices
{
    public class STRINGS
    {
        public class DUPLICANTS
        {
            public class ATTRIBUTES
            {
                public class JOY_EXTRA_CHANCE
                {
                    public static LocString NAME = $"{UI.FormatAsKeyWord("Joy Reaction")} chance";
                }
            }
            public class MODIFIERS
            {
                public class PHOSPHO_RUFUS_SPICE
                {
                    public static LocString ADDITIONAL_EFFECTS = $"{UI.FormatAsKeyWord("Phosphorescence")}\n{UI.FormatAsKeyWord("Sound Sleep")}";
                }
                public class MOO_COSPLAY_SPICE
                {
                    public static LocString ADDITIONAL_EFFECTS = $"{UI.FormatAsKeyWord("Mooteorism")}";
                }
                public class ZOMBIE_COSPLAY_SPICE
                {
                    public static LocString ADDITIONAL_EFFECTS = $"{UI.FormatAsKeyWord("Tireless")}";
                }
            }
        }

        public class ITEMS
        {
            public class SPICES
            {
                public class PHOSPHO_RUFUS_SPICE
                {
                    public static LocString NAME = "Phosphorus Spice";
                    public static LocString DESC = "Duplicants are simply glowing with happiness.\nNo longer need a Lantern to visit the Toilet.";
                }
                public class MOO_COSPLAY_SPICE
                {
                    public static LocString NAME = "Moo Spice";
                    public static LocString DESC = $"Be strong as {UI.FormatAsLink("Moo", "MOO")}. Let the whole Cosmos hear the powerful roar of your Jet Thruster.";
                }
                public class ZOMBIE_COSPLAY_SPICE
                {
                    public static LocString NAME = "Zombie Spice";
                    public static LocString DESC = "Hurray! Zombie party!\nBrains not included.";
                }
            }
        }

        internal static void DoReplacement()
        {
            LocString.CreateLocStringKeys(typeof(DUPLICANTS));
            LocString.CreateLocStringKeys(typeof(ITEMS));
        }
    }
}
