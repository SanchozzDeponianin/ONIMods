﻿using System.Collections.Generic;
using System.Linq;
using STRINGS;
using SanchozzONIMods.Lib;

namespace Smelter
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

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
        private const string SULFUR = "{SULFUR}";
        private const string LIQUIDSULFUR = "{LIQUIDSULFUR}";
        private const string RESIN = "{RESIN}";
        private const string ISORESIN = "{ISORESIN}";

        private const string SMELTER = "{SMELTER}";
        private const string METALREFINERY = "{METALREFINERY}";
        private const string GLASSFORGE = "{GLASSFORGE}";

        public class OPTIONS
        {
            public class RECIPES
            {
                public static LocString CATEGORY = $"Enable new recipes";
            }
            public class KATAIRITE_TO_TUNGSTEN
            {
                public static LocString NAME = $"{KATAIRITE} to {TUNGSTEN}";
                public static LocString TOOLTIP = $"Available at {SMELTER}, {METALREFINERY}";
            }
            public class PHOSPHORITE_TO_PHOSPHORUS
            {
                public static LocString NAME = $"{PHOSPHORITE} to {PHOSPHORUS}";
                public static LocString TOOLTIP = $"Available at {SMELTER}, {GLASSFORGE}";
            }
            public class PLASTIC_TO_NAPHTHA
            {
                public static LocString NAME = $"{POLYPROPYLENE} to {NAPHTHA}";
                public static LocString TOOLTIP = $"Available at {SMELTER}, {GLASSFORGE}";
            }
            public class SULFUR_TO_LIQUIDSULFUR
            {
                public static LocString NAME = $"{SULFUR} to {LIQUIDSULFUR}";
                public static LocString TOOLTIP = $"Available at {GLASSFORGE}";
            }
            public class RESIN_TO_ISORESIN
            {
                public static LocString NAME = $"{RESIN} to {ISORESIN}";
                public static LocString TOOLTIP = $"Available at {SMELTER}";
            }

            public class FEATURES
            {
                public static LocString CATEGORY = $"Enable some features for vanilla buildings";
            }

            public class METALREFINERY_DROP_OVERHEATED_COOLANT
            {
                public static LocString NAME = $"{METALREFINERY} will drop the overheated {UI.FormatAsKeyWord("Coolant")}";
                public static LocString TOOLTIP = $"The overheated {UI.FormatAsKeyWord("Coolant")} is dropped directly into the atmosphere\nThis will help prevent damage to the output Pipe";
            }
            public class METALREFINERY_REUSE_COOLANT
            {
                public static LocString NAME = $"{METALREFINERY} will re-use the waste {UI.FormatAsKeyWord("Coolant")}";
                public static LocString TOOLTIP = $"This can speed up production and reduce {UI.FormatAsKeyWord("Coolant")} consumption";
            }
        }

        internal static void DoReplacement()
        {
            BUILDINGS.PREFABS.SMELTER.DESC = global::STRINGS.BUILDINGS.PREFABS.METALREFINERY.DESC;
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
            var elements = new string[] { KATAIRITE, TUNGSTEN, PHOSPHORITE, PHOSPHORUS, POLYPROPYLENE, NAPHTHA, SULFUR, LIQUIDSULFUR, RESIN, ISORESIN };
            var dictionary = new Dictionary<string, string>
            {
                { SMELTER, BUILDINGS.PREFABS.SMELTER.NAME },
                { METALREFINERY, global::STRINGS.BUILDINGS.PREFABS.METALREFINERY.NAME },
                { GLASSFORGE, global::STRINGS.BUILDINGS.PREFABS.GLASSFORGE.NAME }
            }.PrepareReplacementDictionary(elements, "STRINGS.ELEMENTS.{0}.NAME");
            foreach (var key in dictionary.Keys.ToList())
            {
                dictionary[key] = UI.FormatAsKeyWord(UI.StripLinkFormatting(dictionary[key]));
            }
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
        }
    }
}
