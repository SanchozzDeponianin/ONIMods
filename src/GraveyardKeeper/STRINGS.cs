using System.Collections.Generic;
using System.Linq;
using SanchozzONIMods.Lib;
using STRINGS;

namespace GraveyardKeeper
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        private const string COLDBREATHER = "{COLDBREATHER}";
        private const string OXYFERN = "{OXYFERN}";
        private const string BASICFORAGEPLANT = "{BASICFORAGEPLANT}";
        private const string SQUIRREL = "{SQUIRREL}";
        private const string EVILFLOWER = "{EVILFLOWER}";

        public class OPTIONS
        {
            public class TITLE
            {
                public static LocString NAME = $"Choose the types of Plants with which the {SQUIRREL}\nwill decorate the burial place of the Duplicant's corpse.";
                public static LocString TOOLTIP = $"If nothing is selected, the {SQUIRREL} will use the {EVILFLOWER}.";
            }
            public class NON_YIELDING_PLANTS
            {
                public static LocString NAME = "Non-Yielding Plants";
                public static LocString TOOLTIP = $"{COLDBREATHER}, {OXYFERN} and decorative plants.";
            }
            public class SINGLE_HARVEST_PLANTS
            {
                public static LocString NAME = "Single-Harvest Plants";
                public static LocString TOOLTIP = $"{BASICFORAGEPLANT}, etc.";
            }
            public class REGULAR_PLANTS
            {
                public static LocString NAME = "Regular Plants";
                public static LocString TOOLTIP = "Most of the Plants that produce crops regularly.";
            }
            public class MAX_PLANTS_SPAWN
            {
                public static LocString NAME = $"The maximum number of Plants planted by {SQUIRREL}\nduring one burial of the corpse.";
                public static LocString TOOLTIP = $"In any case, {SQUIRREL} will not plant several identical Plants at a time.";
            }
        }

        internal static void DoReplacement()
        {
            var dictionary = new Dictionary<string, string>()
            {
                { COLDBREATHER, CREATURES.SPECIES.COLDBREATHER.NAME },
                { OXYFERN, CREATURES.SPECIES.OXYFERN.NAME },
                { BASICFORAGEPLANT, ITEMS.FOOD.BASICFORAGEPLANT.NAME },
                { SQUIRREL, CREATURES.SPECIES.SQUIRREL.NAME },
                { EVILFLOWER, CREATURES.SPECIES.EVILFLOWER.NAME },
            };
            foreach (var key in dictionary.Keys.ToArray())
                dictionary[key] = UI.FormatAsKeyWord(UI.StripLinkFormatting(dictionary[key]));
            Utils.ReplaceAllLocStringTextByDictionary(typeof(OPTIONS), dictionary);
        }
    }
}
