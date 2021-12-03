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
            var artifactItems = new List<string>();
            foreach (var artifactType in ArtifactConfig.artifactItems.Keys)
                artifactItems.AddRange(ArtifactConfig.artifactItems[artifactType]);
            artifactItems = artifactItems.Distinct().ToList();
            foreach (string artifactID in artifactItems)
            {
                var artifactTier = Assets.GetPrefab(artifactID.ToTag()).GetComponent<SpaceArtifact>().GetArtifactTier();
                int tier = -1;
                for (int i = 0; i < tiers.Length; i++)
                {
                    if (artifactTier == tiers[i])
                    {
                        tier = i;
                        break;
                    }
                }
                if (tier >= 0) // пропускаем добавленные модами артифакты с нестандартной ArtifactTier
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

        // получение списка рандомных посылок с артифактами для дальнейшего внедрения в список посылок
        // используется динамическая вероятность - чем больше артифактов конкретного типа уже есть - тем меньше шанс получить его.
        private const int START_WEIGHT_AMOUNT = 2;
        private class CarePackageWeightInfo
        {
            public CarePackageInfo info;
            public int amount;
            public float weight => 1f / amount;
            public CarePackageWeightInfo(CarePackageInfo carePackageInfo)
            {
                info = carePackageInfo;
                amount = START_WEIGHT_AMOUNT;
            }
        }
        private List<CarePackageInfo> GetRandomCarePackages()
        {
            var possiblePackages = carePackages
                .Where(package => package.requirement == null || package.requirement())
                .ToDictionary(info => info.id, info => new CarePackageWeightInfo(info));
            if (possiblePackages.Count == 0)
                return null;

            // если динамическая вероятность выключена - у всех возможных вариантов будет тупо одинаковый стартовый вес
            if (ArtifactCarePackageOptions.Instance.DynamicProbability)
            {
                foreach (var art in Components.SpaceArtifacts.Items)
                {
                    var id = art.PrefabID().ToString();
                    if (possiblePackages.ContainsKey(id))
                        possiblePackages[id].amount++;
                }
            }
            float total_weight = 0;
            foreach (var keyValuePair in possiblePackages)
                total_weight += keyValuePair.Value.weight;

            var selectedPackages = new List<CarePackageInfo>();
            for (int i = 0; i < DropTableSlots; i++)
            {
                float selected_weight = Random.value * total_weight;
                foreach (var keyValuePair in possiblePackages)
                {
                    selected_weight -= keyValuePair.Value.weight;
                    if (selected_weight <= 0)
                    {
                        selectedPackages.Add(keyValuePair.Value.info);
                        total_weight -= keyValuePair.Value.weight;
                        possiblePackages.Remove(keyValuePair.Key);
                        break;
                    }
                }
                if (total_weight <= 0f)
                    break;
            }
            return selectedPackages;
        }

        internal static List<CarePackageInfo> InjectRandomCarePackages(List<CarePackageInfo> carePackages)
        {
            var injectedCarePackages = Instance?.GetRandomCarePackages();
            if (injectedCarePackages != null)
                carePackages.AddRange(injectedCarePackages);
            return carePackages;
        }
    }
}
