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

        /*public class OPTIONS
        {
            
        }*/

        internal static void DoReplacement()
        {
            BUILDINGS.PREFABS.SMELTER.DESC = global::STRINGS.BUILDINGS.PREFABS.METALREFINERY.DESC;
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
