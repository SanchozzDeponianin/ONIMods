﻿using Klei.AI;
using TUNING;
using UnityEngine;
using static BetterPlantTending.ModAssets;

namespace BetterPlantTending
{
    public class TendedColdBreather : TendedPlant
    {
        [SerializeField]
        public float emitRads;

#pragma warning disable CS0649
        [MyCmpReq]
        private ReceptacleMonitor receptacleMonitor;

        [MyCmpReq]
        private ColdBreather coldBreather;

        [MyCmpReq]
        private ElementConsumer elementConsumer;

        [MyCmpGet]
        private RadiationEmitter emitter;
#pragma warning restore CS0649

        private AttributeInstance growingRate;

        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            var attributes = this.GetAttributes();
            growingRate = attributes.Add(Db.Get().Amounts.Maturity.deltaAttribute);
            attributes.Add(fakeGrowingRate);
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            ApplyModifier();
        }

        public override void ApplyModifier()
        {
            // а тут нужно учесть дикость
            float grow_multiplier = growingRate.GetTotalValue() / CROPS.GROWTH_RATE;
            float wild_multiplier = (receptacleMonitor.Replanted ? 1 : CROPS.WILD_GROWTH_RATE_MODIFIER);
            float rate = coldBreather.consumptionRate * grow_multiplier * wild_multiplier;
            if (elementConsumer.consumptionRate != rate)
            {
                elementConsumer.consumptionRate = rate;
                elementConsumer.RefreshConsumptionRate();
            }
            if (emitter != null)
            {
                var rads = emitRads;
                if (ModOptions.Instance.coldbreather_adjust_radiation_by_grow_speed)
                    rads *= grow_multiplier;
                if (ModOptions.Instance.coldbreather_decrease_radiation_by_wildness)
                    rads *= wild_multiplier;
                if (emitter.emitRads != rads)
                {
                    emitter.emitRads = rads;
                    emitter.Refresh();
                }
            }
        }
    }
}
