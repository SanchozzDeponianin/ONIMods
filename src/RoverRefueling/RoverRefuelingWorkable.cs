using Klei.AI;
using System.Collections.Generic;
using UnityEngine;

namespace RoverRefueling
{
    public class RoverRefuelingWorkable : Workable, IGameObjectEffectDescriptor
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Storage storage;
#pragma warning restore CS0649

        private float fuelConsumeRate;
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
            fuelConsumeRate = RoverRefuelingOptions.Instance.fuel_mass_per_charge / RoverRefuelingOptions.Instance.charge_time;
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
            worker.TryGetComponent<PrimaryElement>(out primaryElement);
            if (worker.TryGetComponent<Effects>(out var effects) && !effects.HasEffect(RoverRefuelingPatches.RefuelingEffect))
                effects.Add(RoverRefuelingPatches.RefuelingEffect, false);
        }

        protected override void OnStopWork(Worker worker)
        {
            battery = null;
            primaryElement = null;
            if (worker.TryGetComponent<Effects>(out var effects) && effects.HasEffect(RoverRefuelingPatches.RefuelingEffect))
                effects.Remove(RoverRefuelingPatches.RefuelingEffect);
        }

        protected override bool OnWorkTick(Worker worker, float dt)
        {
            if (battery.value >= battery.GetMax())
                return true;
            float need = fuelConsumeRate * dt;
            storage.ConsumeAndGetDisease(RoverRefuelingStationConfig.fuelTag, need, out float consumed, out var diseaseInfo, out float _);
            primaryElement.AddDisease(diseaseInfo.idx, diseaseInfo.count, "Refueling");
            return consumed < need;
        }

        public override List<Descriptor> GetDescriptors(GameObject go)
        {
            var list = base.GetDescriptors(go);
            var item = default(Descriptor);
            item.SetupDescriptor(RoverRefuelingStationConfig.fuelTag.ProperName(),
                string.Format(STRINGS.BUILDINGS.PREFABS.ROVERREFUELINGSTATION.REQUIREMENT_TOOLTIP,
                RoverRefuelingOptions.Instance.fuel_mass_per_charge, RoverRefuelingStationConfig.fuelTag.ProperName()),
                Descriptor.DescriptorType.Requirement);
            list.Add(item);
            return list;
        }

        public override Vector3 GetTargetPoint() => storage.GetTargetPoint();
        public override Vector3 GetFacingTarget() => storage.GetTargetPoint();
    }
}
