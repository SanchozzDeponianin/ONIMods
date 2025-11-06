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
            Subscribe((int)GameHashes.EffectAdded, OnEffectChanged);
            Subscribe((int)GameHashes.EffectRemoved, OnEffectChanged);
            OnEffectChanged(null);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.EffectAdded, OnEffectChanged);
            Unsubscribe((int)GameHashes.EffectRemoved, OnEffectChanged);
            base.OnCleanUp();
        }

        private void OnEffectChanged(object data)
        {
            if (buildingElementEmitter != null)
            {
                buildingElementEmitter.emitRate = base_methane_production_rate * gameObject.GetAttributes().GetValue(ModAssets.MACHINERY_SPEED_MODIFIER_NAME);
                //Traverse.Create(buildingElementEmitter).Field<bool>("dirty").Value = true;
                DIRTY.Set(buildingElementEmitter, true);
            }
        }
    }
}
