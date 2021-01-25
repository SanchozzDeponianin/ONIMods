using System.Collections.Generic;
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

            var dictionary = new Dictionary<string, string>
            {
                { COLDBREATHER, UI.FormatAsKeyWord(CREATURES.SPECIES.COLDBREATHER.NAME) },
                { OXYFERN, UI.FormatAsKeyWord(CREATURES.SPECIES.OXYFERN.NAME) },
                { FARMTINKER, UI.FormatAsKeyWord(global::STRINGS.DUPLICANTS.MODIFIERS.FARMTINKER.NAME) }
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);

            LocString.CreateLocStringKeys(typeof(DUPLICANTS));
        }
    }
}
