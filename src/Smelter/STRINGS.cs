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
        // todo: доделать 
        public class OPTIONS
        {
            public class KATAIRITE_TO_TUNGSTEN
            {
                public static LocString TITLE = "KATAIRITE_TO_TUNGSTEN";
                public static LocString TOOLTIP = "";
            }
            public class PHOSPHORITE_TO_PHOSPHORUS
            {
                public static LocString TITLE = "PHOSPHORITE_TO_PHOSPHORUS";
                public static LocString TOOLTIP = "";
            }
            public class PLASTIC_TO_NAPHTHA
            {
                public static LocString TITLE = "PLASTIC_TO_NAPHTHA";
                public static LocString TOOLTIP = "";
            }
            public class WOOD_TO_CARBON
            {
                public static LocString TITLE = "WOOD_TO_CARBON";
                public static LocString TOOLTIP = "";
            }
            public class DROP_OVERHEATED_COOLANT
            {
                public static LocString TITLE = "DROP_OVERHEATED_COOLANT";
                public static LocString TOOLTIP = "";
            }
            public class REUSE_COOLANT
            {
                public static LocString TITLE = "REUSE_COOLANT";
                public static LocString TOOLTIP = "";
            }
        }

        internal static void DoReplacement()
        {
            BUILDINGS.PREFABS.SMELTER.DESC = global::STRINGS.BUILDINGS.PREFABS.METALREFINERY.DESC;
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
