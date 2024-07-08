using Klei.AI;
using TUNING;
using UnityEngine;

namespace BetterPlantTending
{
    using handler = EventSystem.IntraObjectHandler<TendedPlant>;
    public class TendedSaltPlant : TendedPlant
    {
        private static readonly handler OnGrowDelegate = new handler((component, data) => component.QueueApplyModifier());

        [SerializeField]
        public float consumptionRate;

#pragma warning disable CS0649
        [MyCmpReq]
        private ElementConsumer consumer;
#pragma warning restore CS0649

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.Grow, OnGrowDelegate);
            Subscribe((int)GameHashes.Wilt, OnGrowDelegate);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.Grow, OnGrowDelegate);
            Unsubscribe((int)GameHashes.Wilt, OnGrowDelegate);
            base.OnCleanUp();
        }

        public override void ApplyModifier()
        {
            if (BetterPlantTendingOptions.Instance.saltplant_adjust_gas_consumption)
            {
                // в этих растениях дикость уже учтена внутри Growing
                float multiplier = this.GetAttributes().Get(Db.Get().Amounts.Maturity.deltaAttribute).GetTotalValue() / CROPS.GROWTH_RATE;
                float rate = consumptionRate * multiplier;
                if (consumer.consumptionRate != rate)
                {
                    consumer.consumptionRate = rate;
                    consumer.RefreshConsumptionRate();
                }
            }
        }
    }
}
