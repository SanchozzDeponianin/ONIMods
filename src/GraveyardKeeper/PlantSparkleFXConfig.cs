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

        public virtual string[] GetDlcIds() => null;

        public void OnPrefabInit(GameObject inst) { }

        public void OnSpawn(GameObject inst) { }
    }
}
