using System;
using Klei.AI;
using STRINGS;
using UnityEngine;

namespace ButcherStation
{
    class ExtraMeatSpawner : KMonoBehaviour
    {
        public string onDeathDropID = string.Empty;
        public int onDeathDropCount = 0;
        public float onDeathDropMultiplier = 0f;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.Butcher, new Action<object> (SpawnExtraMeat));
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.Butcher, new Action<object>(SpawnExtraMeat));
            base.OnCleanUp();
        }

        private void SpawnExtraMeat(object data)
        {
            if (onDeathDropID != string.Empty && onDeathDropCount > 0 && onDeathDropMultiplier > 0)
            {
                GameObject extraMeat = Scenario.SpawnPrefab(Grid.PosToCell(gameObject), 0, 0, onDeathDropID);
                extraMeat.SetActive(true);
                PrimaryElement component = extraMeat.GetComponent<PrimaryElement>();
                component.Units = onDeathDropMultiplier * onDeathDropCount;
                component.Temperature = gameObject.GetComponent<PrimaryElement>().Temperature;
                Edible edible = extraMeat.GetComponent<Edible>();
                if (edible)
                {
                    ReportManager.Instance.ReportValue(ReportManager.ReportType.CaloriesCreated, edible.Calories, StringFormatter.Replace(UI.ENDOFDAYREPORT.NOTES.BUTCHERED, "{0}", extraMeat.GetProperName()), UI.ENDOFDAYREPORT.NOTES.BUTCHERED_CONTEXT);
                }
            }
        }
    }
}
