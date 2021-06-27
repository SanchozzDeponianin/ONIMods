namespace SandboxMutantPlant
{
    internal static class MutantPlantExtensions
    {
        // применяем случайную мутацию и обновляем кнопки в боковом экране
        internal static void Mutator(this MutantPlant mutant)
        {
            if (mutant != null)
            {
                mutant.Mutate();
                mutant.ApplyMutations();
                PlantSubSpeciesCatalog.Instance.DiscoverSubSpecies(mutant.GetSubSpeciesInfo(), mutant);
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
