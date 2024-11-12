using System.Collections.Generic;
using static STRINGS.BUILDINGS.PREFABS;
using SanchozzONIMods.Lib;

namespace LargeTelescope
{
    public class STRINGS
    {
        private const string ENCLOSED_TELESCOPE = "{ENCLOSED_TELESCOPE}";
        private const string VANILLA_TELESCOPE = "{VANILLA_TELESCOPE}";

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
                public static LocString NAME = $"Add Glass to the construction cost of the {ENCLOSED_TELESCOPE}";
            }
            public class VANILLA_ADD_GLASS
            {
                public static LocString NAME = $"Add Glass to the construction cost of the {VANILLA_TELESCOPE}";
            }
            public class NOT_REQUIRE_GAS_PIPE
            {
                public static LocString NAME = "Make the Gas Pipe connection requirement optional instead of mandatory";
            }
            public class PROHIBIT_INSIDE_ROCKET
            {
                public static LocString NAME = $"Prohibit the construction of the {ENCLOSED_TELESCOPE} inside a Rocket";
            }
        }

        internal static void DoReplacement()
        {
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), new Dictionary<string, string>
                { { ENCLOSED_TELESCOPE, CLUSTERTELESCOPEENCLOSED.NAME },
                  { VANILLA_TELESCOPE, TELESCOPE.NAME} });
        }
    }
}
