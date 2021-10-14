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
                    public static LocString DESC = "";
                    public static LocString EFFECT = $"A {UI.FormatAsLink("Large Telescope", "CLUSTERLARGETELESCOPE")} allows you to look further into space.\n\n";
                }
            }
        }

        public class OPTIONS
        {
            /*
            public class MORALEBONUS
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord(MORALE)} bonus";
            }
            public class SPECIFICEFFECTDURATION
            {
                public static LocString NAME = $"Duration of the {UI.FormatAsKeyWord("Stargazing")} effect";
            }
            public class STRESSDELTA
            {
                public static LocString NAME = "Stress recovery during use, % per day";
            }
            */
        }

        internal static void DoReplacement()
        {
            BUILDINGS.PREFABS.CLUSTERLARGETELESCOPE.DESC = CLUSTERTELESCOPE.DESC;
            BUILDINGS.PREFABS.CLUSTERLARGETELESCOPE.EFFECT += CLUSTERTELESCOPE.EFFECT;
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
