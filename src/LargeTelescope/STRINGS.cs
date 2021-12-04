using STRINGS;
using static STRINGS.BUILDINGS.PREFABS;

namespace LargeTelescope
{
    public class STRINGS
    {
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
                public static LocString TOOLTIP = "A small Telescope has a radius = 3";
            }
            public class EFFICIENCY_MULTIPLIER
            {
                public static LocString NAME = "Workspeed modifier, +X%";
            }
            public class FIX_NO_CONSUME_POWER_BUG
            {
                public static LocString NAME = "Fix the Bug";
                public static LocString TOOLTIP = "that Telescopes don't actually consume Power";
            }
        }

        internal static void DoReplacement()
        {
            BUILDINGS.PREFABS.CLUSTERLARGETELESCOPE.EFFECT = CLUSTERTELESCOPE.EFFECT + BUILDINGS.PREFABS.CLUSTERLARGETELESCOPE.EFFECT;
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
