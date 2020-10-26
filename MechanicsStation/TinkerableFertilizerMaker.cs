using System;
using Harmony;
using Klei.AI;

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

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.EffectAdded, new Action<object>(OnEffectChanged));
            Subscribe((int)GameHashes.EffectRemoved, new Action<object>(OnEffectChanged));
            OnEffectChanged(null);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.EffectAdded, new Action<object>(OnEffectChanged));
            Unsubscribe((int)GameHashes.EffectRemoved, new Action<object>(OnEffectChanged));
            base.OnCleanUp();
        }

        private void OnEffectChanged(object data)
        {
            if (buildingElementEmitter != null)
            {
                buildingElementEmitter.emitRate = base_methane_production_rate * gameObject.GetAttributes().GetValue(MechanicsStationConfig.MACHINERYSPEEDMODIFIERNAME);
                Traverse.Create(buildingElementEmitter).Field<bool>("dirty").Value = true;
            }
        }
    }
}
