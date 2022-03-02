using System.Collections.Generic;
using STRINGS;
using SanchozzONIMods.Lib;

namespace BetterPlantTending
{
    public class STRINGS
    {
        private const string COLDBREATHER = "{COLDBREATHER}";
        private const string OXYFERN = "{OXYFERN}";
        private const string FARMTINKER = "{FARMTINKER}";
        private const string DIVERGENTCROPTENDED = "{DIVERGENTCROPTENDED}";
        private const string WORMCROPTENDED = "{WORMCROPTENDED}";
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
            public class COLDBREATHER_MODIFIER
            {
                public static LocString TITLE = $"{COLDBREATHER} Throughput +X% Modifier";
                public static LocString TOOLTIP = $"Increase Cooling power when exposed to effect";
            }
            public class OXYFERN_MODIFIER
            {
                public static LocString TITLE = $"{OXYFERN} Throughput +X% Modifier";
                public static LocString TOOLTIP = $"Increase Oxygen production when exposed to effect";
            }

            public class CATEGORY
            {
                public static LocString GENEGAL = "Genegal";
                public static LocString FARMTINKER = "\"{FARMTINKER}\" additional effects";
                public static LocString DIVERGENTCROPTENDED = "\"{DIVERGENTCROPTENDED}\" additional effects";
                public static LocString WORMCROPTENDED = "\"{WORMCROPTENDED}\" additional effects";
            }
        }

        internal static void DoReplacement()
        {
            Strings.Add("STRINGS.DUPLICANTS.ATTRIBUTES.COLDBREATHERTHROUGHPUT.NAME", CREATURES.STATUSITEMS.COOLING.NAME.text);

            var dictionary = new Dictionary<string, string>
            {
                { COLDBREATHER, UI.FormatAsKeyWord(CREATURES.SPECIES.COLDBREATHER.NAME) },
                { OXYFERN, UI.FormatAsKeyWord(CREATURES.SPECIES.OXYFERN.NAME) },
                { FARMTINKER, UI.FormatAsKeyWord(global::STRINGS.DUPLICANTS.MODIFIERS.FARMTINKER.NAME) },
                { DIVERGENTCROPTENDED, UI.FormatAsKeyWord(CREATURES.MODIFIERS.DIVERGENTPLANTTENDED.NAME)},
                { WORMCROPTENDED, UI.FormatAsKeyWord(CREATURES.MODIFIERS.DIVERGENTPLANTTENDEDWORM.NAME)}
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);

            LocString.CreateLocStringKeys(typeof(DUPLICANTS));
        }
    }
}
