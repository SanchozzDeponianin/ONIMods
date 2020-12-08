using Klei.AI;

namespace MoreTinkerablePlants
{
    public class TinkerableOxyfern : TinkerableEffectMonitor
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Oxyfern oxyfern;
#pragma warning restore CS0649

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            this.GetAttributes().Add(MoreTinkerablePlantsPatches.OxyfernThroughput);
        }

        public override void ApplyModifier()
        {
            base.ApplyModifier();
            oxyfern.SetConsumptionRate();
        }
    }
}
