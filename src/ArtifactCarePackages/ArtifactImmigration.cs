using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static TUNING.DECOR.SPACEARTIFACT;

namespace ArtifactCarePackages
{
    internal class ArtifactImmigration
    {
        private List<CarePackageInfo> carePackages;
        private int DropTableSlots;
        internal static ArtifactImmigration Instance;

        internal ArtifactImmigration()
        {
            Instance = this;
            carePackages = new List<CarePackageInfo>();
            var tiers = new ArtifactTier[] { TIER0, TIER1, TIER2, TIER3, TIER4, TIER5 };
            int a = ArtifactCarePackageOptions.Instance.CyclesUntilTier0;
            int b = ArtifactCarePackageOptions.Instance.CyclesUntilTierNext;
            DropTableSlots = ArtifactCarePackageOptions.Instance.RandomArtifactDropTableSlots;
            foreach (string artifactID in ArtifactConfig.artifactItems)
            {
                var artifactTier = Assets.GetPrefab(artifactID.ToTag()).GetComponent<SpaceArtifact>().GetArtifactTier();
                int tier = 0;
                for (int i = 0; i < tiers.Length; i++)
                {
                    if (artifactTier == tiers[i])
                    {
                        tier = i;
                        break;
                    }
                }
                carePackages.Add(new CarePackageInfo(artifactID, 1, () => CycleCondition(a + b * tier)));
            }
            carePackages.Add(new CarePackageInfo(GeneShufflerRechargeConfig.ID, 1, () => CycleCondition(a + b * tiers.Length)));
        }

        internal static void DestroyInstance()
        {
            Immigration.Instance = null;
        }

        private bool CycleCondition(int cycle)
        {
            return GameClock.Instance.GetCycle() >= cycle;
        }

        private List<CarePackageInfo> GetRandomCarePackages()
        {
            var possiblePackages = carePackages.Where(package => package.requirement == null || package.requirement()).ToList();
            possiblePackages.Shuffle();
            if (possiblePackages.Count == 0)
                return possiblePackages;
            return possiblePackages.GetRange(0, Mathf.Min(possiblePackages.Count, DropTableSlots));
        }

        internal static List<CarePackageInfo> InjectRandomCarePackages(List<CarePackageInfo> carePackages)
        {
            var injectedCarePackages = Instance?.GetRandomCarePackages();
            if (injectedCarePackages != null)
            {
                carePackages.AddRange(injectedCarePackages);
            }
            return carePackages;
        }
    }
}
