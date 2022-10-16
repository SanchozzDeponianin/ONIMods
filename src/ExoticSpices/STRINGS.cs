using STRINGS;
using static STRINGS.ELEMENTS;

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
                    public static LocString ADDITIONAL_EFFECTS = UI.FormatAsKeyWord("Mooteorism");
                }
                public class ZOMBIE_COSPLAY_SPICE
                {
                    public static LocString ADDITIONAL_EFFECTS = UI.FormatAsKeyWord("Tireless");
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

        public class OPTIONS
        {
            public class PHOSPHO_RUFUS_SPICE
            {
                public static LocString CATEGORY = "";
            }
            public class GASSY_MOO_SPICE
            {
                public static LocString CATEGORY = "";
            }
            public class ZOMBIE_SPICE
            {
                public static LocString CATEGORY = "";
            }
            public class JOY_REACTION_CHANCE
            {
                public static LocString NAME = "Joy Reaction chance, +X%";
                public static LocString TOOLTIP = "Additional chance to start a Joy Reaction\neven if the Duplicant does not have enough Morale.";
            }
            public class RANGE
            {
                public static LocString NAME = "Light Range, tiles";
            }
            public class LUX
            {
                public static LocString NAME = "Brightness, Lux";
            }
            public class ATTRIBUTE_BUFF
            {
                public static LocString NAME = "Strength and Athletics, +X";
            }
            public class STAMINA_BUFF
            {
                public static LocString NAME = "Stamina recovery, +X% per cycle";
            }
            public class EMIT_MASS
            {
                public static LocString NAME = "\"Jet Thruster\" exhaust power";
                public static LocString TOOLTIP = "Exhaust power is calculated according to an empirical formula,\ntaking into account crop productivity of Gas Grass,\ndiet of Gassy Moo and other parameters.\nBut you can slightly decrease or increase it.";
            }
            public class X0_25
            {
                public static LocString NAME = "x0.25";
            }
            public class X0_5
            {
                public static LocString NAME = "x0.5";
            }
            public class X1
            {
                public static LocString NAME = "normal";
            }
            public class X2
            {
                public static LocString NAME = "x2";
            }
            public class X4
            {
                public static LocString NAME = "x4";
            }
            public class EMIT_GAS
            {
                public static LocString NAME = "\"Jet Thruster\" exhaust type";
                public static LocString TOOLTIP = "What do you prefer? A little free fuel or the opportunity to sniff gases?";
            }
            public class METHANE
            {
                public static LocString NAME = "";
            }
            public class CONTAMINATEDOXYGEN
            {
                public static LocString NAME = "";
            }
        }

        internal static void DoReplacement()
        {
            OPTIONS.PHOSPHO_RUFUS_SPICE.CATEGORY = ITEMS.SPICES.PHOSPHO_RUFUS_SPICE.NAME;
            OPTIONS.GASSY_MOO_SPICE.CATEGORY = ITEMS.SPICES.MOO_COSPLAY_SPICE.NAME;
            OPTIONS.ZOMBIE_SPICE.CATEGORY = ITEMS.SPICES.ZOMBIE_COSPLAY_SPICE.NAME;
            OPTIONS.METHANE.NAME = UI.StripLinkFormatting(METHANE.NAME);
            OPTIONS.CONTAMINATEDOXYGEN.NAME = UI.StripLinkFormatting(CONTAMINATEDOXYGEN.NAME);
            LocString.CreateLocStringKeys(typeof(DUPLICANTS));
            LocString.CreateLocStringKeys(typeof(ITEMS));
        }
    }
}
