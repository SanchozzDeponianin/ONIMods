using STRINGS;
using SanchozzONIMods.Lib;

namespace MorePlantMutations
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        public class CREATURES
        {
            public class PLANT_MUTATIONS
            {
                public class GINGER
                {
                    public static LocString NAME = "Spicy";
                    public static LocString DESCRIPTION = "Plants with this mutation are pickier about their conditions but yield so spicy!";
                }
                public class GRASSY
                {
                    public static LocString NAME = "Husky";
                    public static LocString DESCRIPTION = "Plants with this mutation are very husky.";
                }
                public class GLOWSTICK
                {
                    public static LocString NAME = "Radiance";
                    public static LocString DESCRIPTION = "These plants glow as if they've been to Chernobyl.";
                }
            }
        }

        private const string coldbreather = "{COLDBREATHER}";
        private const string glowstick = "{GLOWSTICK}";
        private const string BPT = "{BPT_TITLE}";

        public class OPTIONS
        {
            public class GLOWSTICK
            {
                public static LocString CATEGORY = $"{glowstick} plant mutantion";
            }
            public class EMIT_LIGHT
            {
                public static LocString NAME = "Emits light";
                public static LocString TOOLTIP = "Except for plants that prefer darkness\nTurn it off if you only need radiation";
            }
            public class ADJUST_RADIATION_BY_GROW_SPEED
            {
                public static LocString NAME = "Adjust the radiation emission in proportion to the growth rate";
            }
            public class DECREASE_RADIATION_BY_WILDNESS
            {
                public static LocString NAME = "Decrease the radiation emission of wild plant";
            }

            public static LocString BPT_TITLE = "Better Farming Effects and Tweaks";
            public class BPT_INTERGRATION
            {
                public static LocString CATEGORY = $"Sync with {BPT} mod";
            }
            public class BPT_LABEL
            {
                public static LocString NAME = $"If {BPT} mod is installed and enabled, for consistency,\nthe {glowstick} plant mutantion follows the {coldbreather} settings\ninstead of the settings specified above.";
            }
            public class ENABLE
            {
                public static LocString NAME = "Enable sync";
            }
        }

        internal static void DoReplacement()
        {
            Utils.ReplaceAllLocStringTextByDictionary(typeof(OPTIONS), new()
            {
                { coldbreather, UI.FormatAsKeyWord(UI.StripLinkFormatting(global::STRINGS.CREATURES.SPECIES.COLDBREATHER.NAME.text)) },
                { glowstick,    UI.FormatAsKeyWord(CREATURES.PLANT_MUTATIONS.GLOWSTICK.NAME.text) },
                { BPT,          UI.FormatAsKeyWord(OPTIONS.BPT_TITLE.text) },
            });
            LocString.CreateLocStringKeys(typeof(CREATURES));
        }
    }
}
