using STRINGS;
using SanchozzONIMods.Lib;

namespace MoreTinkerablePlants
{
    public class STRINGS
    {
        private const string COLDBREATHER = "{COLDBREATHER}";
        private const string OXYFERN = "{OXYFERN}";
        private const string FARMTINKER = "{FARMTINKER}";
        public class DUPLICANTS
        {
            public class ATTRIBUTES
            {
                public class OXYFERNTHROUGHPUT
                {
                    public static LocString NAME = "Oxygen production";
                }
            }
            public class MODIFIERS
            {
                public class FARMTINKER
                {
                    public static LocString ADDITIONAL_EFFECTS = string.Concat(new string[]
                    {
                        "Increases the ",
                        UI.FormatAsKeyWord("Throughput"),
                        " of ",
                        UI.FormatAsLink(OXYFERN, "OXYFERN"),
                        " and ",
                        UI.FormatAsLink(COLDBREATHER, "COLDBREATHER")
                    });
                }
            }
        }

        public class OPTIONS
        {
            public class COLDBREATHER_MULTIPLIER
            {
                public static LocString TITLE = $"{COLDBREATHER} Throughput Multiplier";
                public static LocString TOOLTIP = $"Increase Cooling power when exposed to {FARMTINKER} effect";
            }
            public class OXYFERN_MULTIPLIER
            {
                public static LocString TITLE = $"{OXYFERN} Throughput Multiplier";
                public static LocString TOOLTIP = $"Increase Oxygen production when exposed to {FARMTINKER} effect";
            }
        }

        internal static void DoReplacement()
        {
            Strings.Add("STRINGS.DUPLICANTS.ATTRIBUTES.COLDBREATHERTHROUGHPUT.NAME", CREATURES.STATUSITEMS.COOLING.NAME.text);
            string coldbreather = CREATURES.SPECIES.COLDBREATHER.NAME;
            string oxyfern = CREATURES.SPECIES.OXYFERN.NAME;
            string farmtinker = UI.FormatAsKeyWord(global::STRINGS.DUPLICANTS.MODIFIERS.FARMTINKER.NAME);
            Utils.ReplaceLocString(ref DUPLICANTS.MODIFIERS.FARMTINKER.ADDITIONAL_EFFECTS, COLDBREATHER, coldbreather);
            Utils.ReplaceLocString(ref DUPLICANTS.MODIFIERS.FARMTINKER.ADDITIONAL_EFFECTS, OXYFERN, oxyfern);
            Utils.ReplaceLocString(ref OPTIONS.COLDBREATHER_MULTIPLIER.TITLE, COLDBREATHER, UI.FormatAsKeyWord(coldbreather));
            Utils.ReplaceLocString(ref OPTIONS.COLDBREATHER_MULTIPLIER.TOOLTIP, FARMTINKER, farmtinker);
            Utils.ReplaceLocString(ref OPTIONS.OXYFERN_MULTIPLIER.TITLE, OXYFERN, UI.FormatAsKeyWord(oxyfern));
            Utils.ReplaceLocString(ref OPTIONS.OXYFERN_MULTIPLIER.TOOLTIP, FARMTINKER, farmtinker);
            LocString.CreateLocStringKeys(typeof(DUPLICANTS));
        }
    }
}
