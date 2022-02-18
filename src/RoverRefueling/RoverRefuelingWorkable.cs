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

        internal AmountInstance battery;
        private PrimaryElement primaryElement;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            resetProgressOnStop = true;
            showProgressBar = false;
            SetWorkTime(float.PositiveInfinity);
            synchronizeAnims = false;
            lightEfficiencyBonus = false;
            faceTargetWhenWorking = true;
            multitoolContext = "fetchliquid";
            multitoolHitEffectTag = WhirlPoolFxEffectConfig.ID;
            storage.gunTargetOffset = new Vector2(0.6f, 0.5f);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // красявости. так как "рабочая точка" смещена, то и позицию для использования тоже смещаем, с учетом переворота постройки
            var offset = GetComponent<Rotatable>().GetRotatedCellOffset(new CellOffset(1, 0));
            SetOffsetTable(OffsetGroups.BuildReachabilityTable(new CellOffset[] { offset }, OffsetGroups.InvertedStandardTable, null));
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

        public override Vector3 GetTargetPoint() => storage.GetTargetPoint();
        public override Vector3 GetFacingTarget() => storage.GetTargetPoint();
    }
}
