using TUNING;

namespace MoreTinkerablePlants
{
    public class TinkerableColdBreather : TinkerableEffectMonitor
    {
        internal static float ThroughputMultiplier = DefaultThroughputMultiplier;

#pragma warning disable CS0649
        [MyCmpReq]
        private ReceptacleMonitor receptacleMonitor;

        [MyCmpReq]
        private ColdBreather coldBreather;

        [MyCmpReq]
        private ElementConsumer elementConsumer;
#pragma warning restore CS0649

        public override void ApplyEffect()
        {
            base.ApplyEffect();
            if (receptacleMonitor.Replanted)
            {
                elementConsumer.consumptionRate = coldBreather.consumptionRate;
            }
            else
            {
                elementConsumer.consumptionRate = coldBreather.consumptionRate * CROPS.WILD_GROWTH_RATE_MODIFIER;
            }
            if (effects.HasEffect(FARMTINKEREFFECTID))
            {
                elementConsumer.consumptionRate *= ThroughputMultiplier;
            }
            elementConsumer.RefreshConsumptionRate();
        }
    }
}
