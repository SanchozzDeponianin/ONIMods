using Klei.AI;
using static BetterPlantTending.BetterPlantTendingAssets;

namespace BetterPlantTending
{
    public class TendedOxyfern : TendedPlant
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Oxyfern oxyfern;
#pragma warning restore CS0649

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            var attributes = this.GetAttributes();
            attributes.Add(Db.Get().Amounts.Maturity.deltaAttribute);
            attributes.Add(fakeGrowingRate);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            ApplyModifier();
        }

        public override void ApplyModifier()
        {
            oxyfern.SetConsumptionRate();
        }
    }
}
