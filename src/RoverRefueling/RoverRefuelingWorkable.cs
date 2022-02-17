using Klei.AI;
using UnityEngine;

namespace RoverRefueling
{
    public class RoverRefuelingWorkable : Workable
    {
        private const float CONSUME_RATE = RoverRefuelingStationConfig.CHARGE_MASS / RoverRefuelingStationConfig.CHARGE_TIME;

#pragma warning disable CS0649
        [MyCmpReq]
        private Storage storage;
#pragma warning restore CS0649

        private AmountInstance battery;
        private PrimaryElement primaryElement;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            resetProgressOnStop = true;
            showProgressBar = false;
            SetWorkTime(float.PositiveInfinity);
            synchronizeAnims = false;
            lightEfficiencyBonus = false;
            multitoolContext = "fetchliquid";
            multitoolHitEffectTag = WhirlPoolFxEffectConfig.ID;
            SetOffsetTable(OffsetGroups.InvertedStandardTable);
            storage.gunTargetOffset = new Vector2(0.5f, 0.3f);
        }

        protected override void OnStartWork(Worker worker)
        {
            battery = Db.Get().Amounts.InternalChemicalBattery.Lookup(worker);
            primaryElement = worker.GetComponent<PrimaryElement>();
            var effects = worker.GetComponent<Effects>();
            if (effects != null && !effects.HasEffect(RoverRefuelingPatches.RefuelingEffect))
                effects.Add(RoverRefuelingPatches.RefuelingEffect, false);
        }

        protected override void OnStopWork(Worker worker)
        {
            battery = null;
            primaryElement = null;
            var effects = worker.GetComponent<Effects>();
            if (effects != null && effects.HasEffect(RoverRefuelingPatches.RefuelingEffect))
                effects.Remove(RoverRefuelingPatches.RefuelingEffect);
        }

        protected override bool OnWorkTick(Worker worker, float dt)
        {
            if (battery.value >= battery.GetMax())
                return true;
            float need = CONSUME_RATE * dt;
            storage.ConsumeAndGetDisease(GameTags.CombustibleLiquid, need, out float consumed, out var diseaseInfo, out float _);
            primaryElement.AddDisease(diseaseInfo.idx, diseaseInfo.count, "Refueling");
            return consumed < need;
        }

        public override Vector3 GetTargetPoint()
        {
            return storage.GetTargetPoint();
        }
    }
}
