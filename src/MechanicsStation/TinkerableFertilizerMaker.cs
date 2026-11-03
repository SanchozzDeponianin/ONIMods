using Klei.AI;
using PeterHan.PLib.Detours;

namespace MechanicsStation
{
    public class TinkerableFertilizerMaker : KMonoBehaviour
    {
        private const float METHANE_PRODUCTION_RATE = 0.01f;
        public static float base_methane_production_rate = METHANE_PRODUCTION_RATE;

#pragma warning disable CS0649
        [MyCmpGet]
        private BuildingElementEmitter buildingElementEmitter;
#pragma warning restore CS0649

        private static readonly IDetouredField<BuildingElementEmitter, bool> DIRTY = PDetours.DetourField<BuildingElementEmitter, bool>("dirty");

        protected override void OnSpawn()
        {
            base.OnSpawn();
            hEffectAdded = Subscribe((int)GameHashes.EffectAdded, OnEffectChangedDelegate, this);
            hEffectRemoved = Subscribe((int)GameHashes.EffectRemoved, OnEffectChangedDelegate, this);
            OnEffectChanged(null);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe(hEffectAdded);
            Unsubscribe(hEffectRemoved);
            base.OnCleanUp();
        }

        private int hEffectAdded;
        private int hEffectRemoved;
        private static readonly System.Action<object, object> OnEffectChangedDelegate = (cmp, data) => ((TinkerableFertilizerMaker)cmp).OnEffectChanged(data);
        private void OnEffectChanged(object data)
        {
            if (buildingElementEmitter != null)
            {
                buildingElementEmitter.emitRate = base_methane_production_rate * gameObject.GetAttributes().GetValue(ModAssets.MACHINERY_SPEED_MODIFIER_NAME);
                DIRTY.Set(buildingElementEmitter, true);
            }
        }
    }
}
