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
                    public static LocString DESC = "Pickled morb`s brains... mmm... yummy!";
                    public static LocString EFFECT = "";
                }
            }
        }

        public class OPTIONS
        {
            public class CONSTRUCTIONTIME
            {
                public static LocString NAME = "Construction Time to build a new Neural Vacillator, seconds";
            }

            public class MANIPULATIONTIME
            {
                public static LocString NAME = "Manipulation Time to prepare a Neural Vacillator for work, seconds";
            }
        }

        internal static void DoReplacement()
        {
            BUILDINGS.PREFABS.BUILDABLEGENESHUFFLER.NAME.ReplaceText(GENESHUFFLER.NAME);
            BUILDINGS.PREFABS.BUILDABLEGENESHUFFLER.EFFECT.ReplaceText(GENESHUFFLER.DESC);
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
