namespace SandboxMutantPlant
{
    internal static class MutantPlantExtensions
    {
        private static void DiscoverSilentlyAndIdentifySubSpecies(PlantSubSpeciesCatalog.SubSpeciesInfo speciesInfo)
        {
            var discovered = PlantSubSpeciesCatalog.Instance.GetAllSubSpeciesForSpecies(speciesInfo.speciesID);
            if (discovered != null && !discovered.Contains(speciesInfo))
            {
                discovered.Add(speciesInfo);
                PlantSubSpeciesCatalog.Instance.IdentifySubSpecies(speciesInfo.ID);
            }
        }

        // применяем случайную мутацию и обновляем кнопки в боковом экране
        // для семян - обычное обнаружение
        // для растений - "тихое" обнаружение и идентификация
        // для дерева - применяем мутацию на ветки
        internal static void Mutator(this MutantPlant mutant)
        {
            if (mutant != null)
            {
                mutant.Mutate();
                mutant.ApplyMutations();
                mutant.AddTag(GameTags.MutatedSeed);
                if (mutant.HasTag(GameTags.Plant))
                    DiscoverSilentlyAndIdentifySubSpecies(mutant.GetSubSpeciesInfo());
                else
                    PlantSubSpeciesCatalog.Instance.DiscoverSubSpecies(mutant.GetSubSpeciesInfo(), mutant);
                var grower = mutant.GetSMI<PlantBranchGrower.Instance>();
                if (!grower.IsNullOrStopped())
                {
                    grower.ActionPerBranch(go =>
                    {
                        if (go.TryGetComponent(out MutantPlant mutant_branch))
                        {
                            mutant.CopyMutationsTo(mutant_branch);
                            mutant_branch.ApplyMutations();
                            DiscoverSilentlyAndIdentifySubSpecies(mutant_branch.GetSubSpeciesInfo());
                        }
                    });
                }
                DetailsScreen.Instance.Trigger((int)GameHashes.UIRefreshData, null);
            }
        }

        // исследуем мутацию и обновляем кнопки в боковом экране
        internal static void IdentifyMutation(this MutantPlant mutant)
        {
            if (mutant != null)
            {
                PlantSubSpeciesCatalog.Instance.IdentifySubSpecies(mutant.SubSpeciesID);
                DetailsScreen.Instance.Trigger((int)GameHashes.UIRefreshData, null);
            }
        }
    }
}
