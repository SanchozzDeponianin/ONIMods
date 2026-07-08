using UnityEngine;

namespace CrittersPoopToPlants
{
    [SkipSaveFileSerialization]
    public class PlantPoopStation : BasePoopStation, IPoopStation, IApproachable
    {
#pragma warning disable CS0649
        [MyCmpGet]
        private ReceptacleMonitor receptacle;

        [MyCmpGet]
        private Harvestable harvestable;

        [MySmiReq]
        private FertilizationMonitor.Instance fertilization;
#pragma warning restore CS0649

        public bool IsWild => !receptacle.Replanted;

        public bool IsOnPlanterBox => !IsWild && receptacle.smi.ReceptacleObject is PlantablePlot plot && plot != null && plot.IsOffGround;

        public override Storage Storage => IsWild ? null : receptacle.smi.ReceptacleObject.storage;

        public int GetCell() => Grid.PosToCell(this);

        private CellOffset[] offsets;

        public CellOffset[] GetOffsets()
        {
            if (offsets == null)
            {
                if (TryGetComponent(out OccupyArea area))
                    offsets = area.OccupiedCellsOffsets;
                else
                    offsets = OffsetGroups.Use;
                if (IsOnPlanterBox)
                    offsets = offsets.Append(new CellOffset(0, -1));
            }
            return offsets;
        }

        public override bool IsPoopStationOperational() => (harvestable == null || !harvestable.CanBeHarvested) && (IsWild || CanAcceptMorePoop());

        public override float GetAvailablePoopCapacity()
        {
            if (IsWild || Storage == null)
                return 0f;
            float stored_mass = 0f;
            for (int i = 0; i < fertilization.def.consumedElements.Length; i++)
                stored_mass += Storage.GetMassAvailable(fertilization.def.consumedElements[i].tag);
            float available = GetPoopCapacity() - stored_mass;
            available = Mathf.Min(Storage.capacityKg, available);
            return Mathf.Clamp(available, 0f, GetPoopCapacity());
        }
    }
}
