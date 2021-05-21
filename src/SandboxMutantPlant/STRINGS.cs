using STRINGS;
using SanchozzONIMods.Lib;

namespace SandboxMutantPlant
{
    public class STRINGS
    {
        private const string GENETICANALYSISSTATION = "{GENETICANALYSISSTATION}";
        public class UI
        {
            public class USERMENUACTIONS
            {
                public class MUTATOR
                {
                    public static LocString NAME = "Mutate";
                    public static LocString TOOLTIP = "Apply a random Mutation to this Seed or Plant.";
                }
                public class IDENTIFY_MUTATION
                {
                    public static LocString NAME = "Identify";
                    public static LocString TOOLTIP = $"Instantly identify Mutation without analysis it at the {GENETICANALYSISSTATION}.";
                }
            }
        }

        internal static void DoReplacement()
        {
            UI.USERMENUACTIONS.IDENTIFY_MUTATION.TOOLTIP.ReplaceText(GENETICANALYSISSTATION, BUILDINGS.PREFABS.GENETICANALYSISSTATION.NAME);
        }
    }
}
