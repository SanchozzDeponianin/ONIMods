using SanchozzONIMods.Lib;
using static STRINGS.BUILDINGS.PREFABS;

namespace BuildableGeneShuffler
{
    public class STRINGS
    {
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class BUILDABLEGENESHUFFLER
                {
                    public static LocString NAME = "";
                    public static LocString DESC = "";
                    public static LocString EFFECT = "";
                }
            }
        }

        public class OPTIONS
        {
            public class CONSTRUCTIONTIME
            {
                public static LocString NAME = "Construction Time to build a Neural Vacillator, seconds";
            }
        }

        internal static void DoReplacement()
        {
            BUILDINGS.PREFABS.BUILDABLEGENESHUFFLER.NAME.ReplaceText(GENESHUFFLER.NAME);
            BUILDINGS.PREFABS.BUILDABLEGENESHUFFLER.DESC.ReplaceText(GENESHUFFLER.DESC);
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
