using System.Collections.Generic;
using STRINGS;
using static STRINGS.BUILDINGS.PREFABS;
using SanchozzONIMods.Lib;

namespace LargeTelescope
{
    public class STRINGS
    {
        private const string ENCLOSED_TELESCOPE = "{ENCLOSED_TELESCOPE}";
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class CLUSTERLARGETELESCOPE
                {
                    public static LocString NAME = UI.FormatAsLink("Large Telescope", "CLUSTERLARGETELESCOPE");
                    public static LocString DESC = "A Large Telescope allows you to study the depths of space faster and further.";
                    public static LocString EFFECT = $"\n\nCan be connected to a {UI.FormatAsLink("Gas Pipe", "GASCONDUIT")} to provide assigned Duplicant with {UI.FormatAsLink("Oxygen", "OXYGEN")}.";
                }
            }
        }

        public class OPTIONS
        {
            public class ANALYZE_CLUSTER_RADIUS
            {
                public static LocString NAME = "Scan Radius, hex";
                public static LocString TOOLTIP = "Unmodded radius = 4\nA small Telescope has a radius = 3";
            }
            public class EFFICIENCY_MULTIPLIER
            {
                public static LocString NAME = "Scan Workspeed modifier, +X%";
            }
            public class ADD_GLASS
            {
                public static LocString NAME = $"Add Glass to the {ENCLOSED_TELESCOPE}`s construction cost";
            }
            public class NOT_REQUIRE_GAS_PIPE
            {
                public static LocString NAME = "Make the Gas Pipe connection requirement optional instead of mandatory";
            }
            public class PROHIBIT_INSIDE_ROCKET
            {
                public static LocString NAME = $"Prohibit the construction of the {ENCLOSED_TELESCOPE} inside a Rocket";
            }
            public class FIX_NO_CONSUME_POWER_BUG
            {
                public static LocString NAME = "Fix a bug where telescopes don't actually consume Power";
            }
        }

        internal static void DoReplacement()
        {
            BUILDINGS.PREFABS.CLUSTERLARGETELESCOPE.EFFECT = CLUSTERTELESCOPE.EFFECT + BUILDINGS.PREFABS.CLUSTERLARGETELESCOPE.EFFECT;
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), new Dictionary<string, string>
                { { ENCLOSED_TELESCOPE, CLUSTERTELESCOPEENCLOSED.NAME } });
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
