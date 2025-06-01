using KSerialization;
using UnityEngine;

namespace ReBuildableAETN
{
    public class MassiveHeatSinkCoreSpawner : KMonoBehaviour
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private SetLocker setLocker;
#pragma warning restore CS0649

        [SerializeField]
        public float chance;

        [Serialize]
        private float random = -1;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (random < 0)
                random = Random.Range(0f, 1f);
            Subscribe((int)GameHashes.LockerDroppedContents, OnLockerLooted);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.LockerDroppedContents, OnLockerLooted);
            base.OnCleanUp();
        }

        private void OnLockerLooted(object data)
        {
            if (random < chance)
            {
                int cell = Grid.OffsetCell(Grid.PosToCell(this), setLocker.dropOffset.x, setLocker.dropOffset.y);
                var go = SpawnCore(cell);
                go.AddTag(GameTags.TerrestrialArtifact);
            }
        }

        internal static GameObject SpawnCore(int cell)
        {
            var core = GameUtil.KInstantiate(Assets.GetPrefab(MassiveHeatSinkCoreConfig.TAG), Grid.CellToPosCBC(cell, Grid.SceneLayer.Ore), Grid.SceneLayer.Ore);
            core.SetActive(true);
            return core;
        }
    }
}
