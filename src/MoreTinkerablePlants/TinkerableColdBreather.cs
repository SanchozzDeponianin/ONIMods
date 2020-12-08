using Klei.AI;
using TUNING;

namespace MoreTinkerablePlants
{
    public class TinkerableColdBreather : TinkerableEffectMonitor
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private ReceptacleMonitor receptacleMonitor;

        [MyCmpReq]
        private ColdBreather coldBreather;

        [MyCmpReq]
        private ElementConsumer elementConsumer;
#pragma warning restore CS0649

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            this.GetAttributes().Add(MoreTinkerablePlantsPatches.ColdBreatherThroughput);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            ApplyModifier();
        }

        public override void ApplyModifier()
        {
            base.ApplyModifier();
            float multiplier = this.GetAttributes().Get(MoreTinkerablePlantsPatches.ColdBreatherThroughput).GetTotalValue();
            elementConsumer.consumptionRate = coldBreather.consumptionRate * (receptacleMonitor.Replanted ? 1 : CROPS.WILD_GROWTH_RATE_MODIFIER) * multiplier;
            elementConsumer.RefreshConsumptionRate();
        }
    }
}
