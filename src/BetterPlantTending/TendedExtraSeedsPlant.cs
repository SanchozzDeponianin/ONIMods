using KSerialization;

namespace BetterPlantTending
{
    public class TendedExtraSeedsPlant : TendedPlant
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private SeedProducer seedProducer;
#pragma warning restore CS0649

        [Serialize]
        private bool hasExtraSeedAvailable = false;

        public override void ApplyModifier()
        {
            base.ApplyModifier();
            // todo: для отладки. пока спавним 100% сразу
            hasExtraSeedAvailable = true;
            ExtractExtraSeed();
        }

        public void ExtractExtraSeed()
        {
            if (hasExtraSeedAvailable)
            {
                hasExtraSeedAvailable = false;
                seedProducer.ProduceSeed(seedProducer.seedInfo.seedId);
            }
        }
    }
}
