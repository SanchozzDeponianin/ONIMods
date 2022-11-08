using UnityEngine;

namespace GraveyardKeeper
{
    public class PlantSparkleFXConfig : IEntityConfig
    {
        public const string ID = nameof(PlantSparkleFX);

        public GameObject CreatePrefab()
        {
            var go = EntityTemplates.CreateEntity(ID, ID, false);
            go.AddOrGet<PlantSparkleFX>();
            return go;
        }

        public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;

        public void OnPrefabInit(GameObject inst) { }

        public void OnSpawn(GameObject inst) { }
    }
}
