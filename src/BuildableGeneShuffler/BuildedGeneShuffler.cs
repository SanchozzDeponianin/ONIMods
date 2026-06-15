using KSerialization;

namespace BuildableGeneShuffler
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class BuildedGeneShuffler : KMonoBehaviour
    {
        [Serialize]
        public bool isBuilded = false;

        [Serialize]
        public float[] constructionMass = new float[0];

        private bool destroyed;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (isBuilded)
            {
                var kbac = GetComponent<KBatchedAnimController>();
                kbac.SwapAnims(new[] { Assets.GetAnim(BuildableGeneShufflerConfig.anim) });
                kbac.SetSceneLayer(Grid.SceneLayer.BuildingBack);
            }
        }

        public void SpawnItemsFromConstruction(WorkerBase chore_worker)
        {
            if (isBuilded && !destroyed)
            {
                destroyed = true;
                if (TryGetComponent<Deconstructable>(out var deconstructable))
                {
                    deconstructable.allowDeconstruction = true;
                    deconstructable.SpawnItemsFromConstruction(chore_worker);
                }
            }
        }
    }
}
