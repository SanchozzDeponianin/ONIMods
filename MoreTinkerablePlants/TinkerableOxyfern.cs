namespace MoreTinkerablePlants
{
    public class TinkerableOxyfern : TinkerableEffectMonitor
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private ElementConsumer elementConsumer;

        [MyCmpReq]
        private ElementConverter elementConverter;

        [MyCmpReq]
        private Oxyfern oxyfern;
#pragma warning restore CS0649

        public override void ApplyEffect()
        {
            base.ApplyEffect();
            oxyfern.SetConsumptionRate();
            if (effects.HasEffect(FARMTINKEREFFECTID))
            {
                elementConsumer.consumptionRate *= PLANTTHROUGHPUTMODIFIER;
                elementConverter.SetWorkSpeedMultiplier(PLANTTHROUGHPUTMODIFIER);
            }
            else
            {
                elementConverter.SetWorkSpeedMultiplier(1f);
            }
            elementConsumer.RefreshConsumptionRate();
        }
    }
}
