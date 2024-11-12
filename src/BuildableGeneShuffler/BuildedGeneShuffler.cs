using KSerialization;
using PeterHan.PLib.Detours;

namespace BuildableGeneShuffler
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class BuildedGeneShuffler : KMonoBehaviour
    {
        private static readonly IDetouredField<KAnimLayering, KAnimControllerBase> ForegroundController =
            PDetours.DetourFieldLazy<KAnimLayering, KAnimControllerBase>("foregroundController");
        private static readonly IDetouredField<KAnimLayering, KAnimLink> Link =
            PDetours.DetourFieldLazy<KAnimLayering, KAnimLink>("link");

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
                // клеи не предусмотрели при замене анима - ситуацию
                // наличия контроллера переднего плана и его корректную обработку.
                // поэтому небольшой хак, перед заменой обнуляем его.
                // todo: вынести в утилиты, потом, если понадобиться еще гдето.
                var kbac = GetComponent<KBatchedAnimController>();
                var layering = kbac.GetLayering();
                if (layering != null)
                {
                    var kbac_fg = ForegroundController.Get(layering);
                    if (kbac_fg != null)
                    {
                        kbac.GetSynchronizer().Remove(kbac_fg);
                        kbac_fg.gameObject.DeleteObject();
                        ForegroundController.Set(layering, null);
                    }
                    var link = Link.Get(layering);
                    if (link != null)
                    {
                        link.Unregister();
                        Link.Set(layering, null);
                    }
                }
                kbac.SwapAnims(new KAnimFile[] { Assets.GetAnim(BuildableGeneShufflerConfig.anim) });
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
