using Klei.AI;
using TUNING;
using UnityEngine;

namespace BetterPlantTending
{
    public class TendedSaltPlant : TendedPlant
    {
        [SerializeField]
        public float consumptionRate;

#pragma warning disable CS0649
        [MyCmpReq]
        private ElementConsumer consumer;
#pragma warning restore CS0649

        public override void ApplyModifier()
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
