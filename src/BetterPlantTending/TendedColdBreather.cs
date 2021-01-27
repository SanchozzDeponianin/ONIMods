using Klei.AI;
using TUNING;

namespace BetterPlantTending
{
    public class TendedColdBreather : TendedPlant
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private ReceptacleMonitor receptacleMonitor;

        [MyCmpReq]
        private ColdBreather coldBreather;

        [MyCmpReq]
        private ElementConsumer elementConsumer;
#pragma warning restore CS0649

        protected override bool ApplyModifierOnEffectRemoved => true;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            this.GetAttributes().Add(BetterPlantTendingPatches.ColdBreatherThroughput);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            ApplyModifier();
        }

        public override void ApplyModifier()
        {
            base.ApplyModifier();
            float multiplier = this.GetAttributes().Get(BetterPlantTendingPatches.ColdBreatherThroughput).GetTotalValue();
            elementConsumer.consumptionRate = coldBreather.consumptionRate * (receptacleMonitor.Replanted ? 1 : CROPS.WILD_GROWTH_RATE_MODIFIER) * multiplier;
            elementConsumer.RefreshConsumptionRate();
        }
    }
}
