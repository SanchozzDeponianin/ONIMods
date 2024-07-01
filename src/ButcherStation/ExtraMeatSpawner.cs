using System.Collections.Generic;
using STRINGS;

namespace ButcherStation
{
    public class ExtraMeatSpawner : KMonoBehaviour
    {
        private static HashSet<string> meats = new HashSet<string>() { MeatConfig.ID, FishMeatConfig.ID, ShellfishMeatConfig.ID, "Tallow" };

        public float dropMultiplier = 0f;
        private bool butchered = false;

#pragma warning disable CS0649
        [MyCmpGet]
        private Butcherable butcherable;
#pragma warning restore CS0649

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.Butcher, SpawnExtraMeat);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.Butcher, SpawnExtraMeat);
            base.OnCleanUp();
        }

        private void SpawnExtraMeat(object data)
        {
            if (!butchered && dropMultiplier > 0f && butcherable != null && butcherable.drops != null && butcherable.drops.Length > 0)
            {
                var drops = new Dictionary<string, float>(meats.Count);
                foreach (var drop_id in butcherable.drops)
                    if (meats.Contains(drop_id))
                    {
                        if (drops.ContainsKey(drop_id))
                            drops[drop_id] += 1f;
                        else
                            drops[drop_id] = 1f;
                    }
                if (drops.Count > 0)
                {
                    int cell = Grid.PosToCell(gameObject);
                    float temp = GetComponent<PrimaryElement>().Temperature;
                    foreach (var drop in drops)
                    {
                        var extraMeat = Scenario.SpawnPrefab(cell, 0, 0, drop.Key);
                        extraMeat.SetActive(true);
                        var primaryElement = extraMeat.GetComponent<PrimaryElement>();
                        primaryElement.Units = dropMultiplier * drop.Value;
                        primaryElement.Temperature = temp;
                        var edible = extraMeat.GetComponent<Edible>();
                        if (edible)
                        {
                            ReportManager.Instance.ReportValue(ReportManager.ReportType.CaloriesCreated, edible.Calories, StringFormatter.Replace(UI.ENDOFDAYREPORT.NOTES.BUTCHERED, "{0}", extraMeat.GetProperName()), UI.ENDOFDAYREPORT.NOTES.BUTCHERED_CONTEXT);
                        }
                    }
                }
                butchered = true;
            }
        }
    }
}
