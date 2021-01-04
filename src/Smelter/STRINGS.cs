using System.Linq;
using STRINGS;
using SanchozzONIMods.Lib;

namespace Smelter
{
    public class STRINGS
    {
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class SMELTER
                {
                    public static LocString NAME = UI.FormatAsLink("Smelter", "SMELTER");
                    public static LocString DESC = "";
                    public static LocString EFFECT = string.Concat(new string[]
                    {
                        $"Produces {UI.FormatAsLink("Refined Metals", "REFINEDMETAL")} from raw {UI.FormatAsLink("Metal Ore", "RAWMETAL")}. ",
                        $"Сonsumes {UI.FormatAsLink("Refined Carbon", "REFINEDCARBON")} as fuel.\n\n",
                        $"Significantly {UI.FormatAsLink("Heats", "HEAT")} the {UI.FormatAsLink("Liquid", "ELEMENTSLIQUID")} piped into it. ",
                        "Must be clean out of too hot liquid, otherwise it will evaporate into the atmosphere.\n\n",
                        "Duplicants will not fabricate items unless recipes are queued.",
                    });
                    public static LocString SIDE_SCREEN_CHECKBOX = "Allow Overheating";
                    public static LocString SIDE_SCREEN_CHECKBOX_TOOLTIP = "If enabled, the overheated Coolant will evaporate into the atmosphere\nIf disabled, Duplicants will empty too hot Coolant into bottles";
                }
            }
            public class STATUSITEMS
            {
                public class SMELTERNEEDSEMPTYING
                {
                    public static LocString NAME = "Requires Emptying";
                    public static LocString TOOLTIP = $"This building needs to be emptied of {UI.FormatAsLink("too hot", "HEAT")} {UI.FormatAsLink("Coolant", "ELEMENTSLIQUID")} to resume function";
                }
            }
        }

        private const string KATAIRITE = "{KATAIRITE}";
        private const string TUNGSTEN = "{TUNGSTEN}";
        private const string PHOSPHORITE = "{PHOSPHORITE}";
        private const string PHOSPHORUS = "{PHOSPHORUS}";
        private const string POLYPROPYLENE = "{POLYPROPYLENE}";
        private const string NAPHTHA = "{NAPHTHA}";
        private const string WOOD = "{WOOD}";
        private const string REFINEDCARBON = "{REFINEDCARBON}";

        private const string SMELTER = "{SMELTER}";
        private const string METALREFINERY = "{METALREFINERY}";
        private const string GLASSFORGE = "{GLASSFORGE}";
        private const string KILN = "{KILN}";

        public class OPTIONS
        {
            public class RECIPES
            {
                public static LocString TITLE = $"Enable new recipes";
            }

            public class KATAIRITE_TO_TUNGSTEN
            {
                public static LocString TITLE = $"Recipe {KATAIRITE} to {TUNGSTEN}";
                public static LocString TOOLTIP = $"Available at {SMELTER}, {METALREFINERY}";
            }
            public class PHOSPHORITE_TO_PHOSPHORUS
            {
                public static LocString TITLE = $"Recipe {PHOSPHORITE} to {PHOSPHORUS}";
                public static LocString TOOLTIP = $"Available at {SMELTER}, {GLASSFORGE}";
            }
            public class POLYPROPYLENE_TO_NAPHTHA
            {
                public static LocString TITLE = $"Recipe {POLYPROPYLENE} to {NAPHTHA}";
                public static LocString TOOLTIP = $"Available at {SMELTER}";
            }
            public class WOOD_TO_REFINEDCARBON
            {
                public static LocString TITLE = $"Recipe {WOOD} to {REFINEDCARBON}";
                public static LocString TOOLTIP = $"Available at {KILN}";
            }

            public class FEATURES
            {
                public static LocString TITLE = $"Enable some features for vanilla buildings";
            }

            public class DROP_OVERHEATED_COOLANT
            {
                public static LocString TITLE = $"{METALREFINERY} will drop the overheated {UI.FormatAsKeyWord("Coolant")}";
                public static LocString TOOLTIP = $"The overheated {UI.FormatAsKeyWord("Coolant")} is dropped directly into the atmosphere\nThis will help prevent damage to the output Pipe";
            }
            public class REUSE_COOLANT
            {
                public static LocString TITLE = $"{METALREFINERY} will re-use the waste {UI.FormatAsKeyWord("Coolant")}";
                public static LocString TOOLTIP = $"This can speed up production and reduce {UI.FormatAsKeyWord("Coolant")} consumption";
            }
        }

        internal static void DoReplacement()
        {
            BUILDINGS.PREFABS.SMELTER.DESC = global::STRINGS.BUILDINGS.PREFABS.METALREFINERY.DESC;
            LocString.CreateLocStringKeys(typeof(BUILDINGS));

            var elements = new string[] { KATAIRITE, TUNGSTEN, PHOSPHORITE, PHOSPHORUS, POLYPROPYLENE, NAPHTHA, REFINEDCARBON };
            //var buildings = new string[] { SMELTER, METALREFINERY, GLASSFORGE, KILN};
            var dictionary = Utils.PrepareReplacementDictionary(null, elements, "STRINGS.ELEMENTS.{0}.NAME");
            //.PrepareReplacementDictionary(buildings, "STRINGS.BUILDINGS.PREFABS.{0}.NAME");
            // блядь! ключи для "STRINGS.BUILDINGS.PREFABS" создаются слишком поздно, в LegacyModMain.LoadBuildings
            // диалог опций судя по всему инициируется раньше. поэтому обломалась идея сделать красивую подстановку.
            dictionary.Add(SMELTER, BUILDINGS.PREFABS.SMELTER.NAME);
            dictionary.Add(METALREFINERY, global::STRINGS.BUILDINGS.PREFABS.METALREFINERY.NAME);
            dictionary.Add(GLASSFORGE, global::STRINGS.BUILDINGS.PREFABS.GLASSFORGE.NAME);
            dictionary.Add(KILN, global::STRINGS.BUILDINGS.PREFABS.KILN.NAME);

            dictionary.Add(WOOD, ITEMS.INDUSTRIAL_PRODUCTS.WOOD.NAME);
            foreach (var key in dictionary.Keys.ToList())
            {
                dictionary[key] = UI.FormatAsKeyWord(UI.StripLinkFormatting(dictionary[key]));
            }
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
        }
    }
}
