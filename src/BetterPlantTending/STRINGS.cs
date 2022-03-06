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
                public class EXTRASEEDCHANCE
                {
                    public static LocString NAME = "Extra Seed Chance";
                }
            }
        }

        // todo: сделать строки когда опции будут готовы
        public class OPTIONS
        {

        }

        internal static void DoReplacement()
        {
            /*var dictionary = new Dictionary<string, string>
            {
                { COLDBREATHER, UI.FormatAsKeyWord(CREATURES.SPECIES.COLDBREATHER.NAME) },
                { OXYFERN, UI.FormatAsKeyWord(CREATURES.SPECIES.OXYFERN.NAME) },
                { FARMTINKER, UI.FormatAsKeyWord(global::STRINGS.DUPLICANTS.MODIFIERS.FARMTINKER.NAME) },
                { DIVERGENTCROPTENDED, UI.FormatAsKeyWord(CREATURES.MODIFIERS.DIVERGENTPLANTTENDED.NAME)},
                { WORMCROPTENDED, UI.FormatAsKeyWord(CREATURES.MODIFIERS.DIVERGENTPLANTTENDEDWORM.NAME)}
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
            */
            LocString.CreateLocStringKeys(typeof(DUPLICANTS));
        }
    }
}
