using UnityEngine;

namespace CrittersPoopToPlants
{
    [SkipSaveFileSerialization]
    public class BuildingPoopStation : BasePoopStation, IPoopStation
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Storage storage;

        [MyCmpGet]
        private Operational operational;
#pragma warning restore CS0649

        public override Storage Storage => storage;

        public override bool IsPoopStationOperational() => (operational == null || operational.IsFunctional) && CanAcceptMorePoop();

        public override float GetAvailablePoopCapacity()
        {
            if (Storage == null)
                return 0f;
            float available = GetPoopCapacity() - Storage.MassStored();
            available = Mathf.Min(Storage.capacityKg, available);
            return Mathf.Clamp(available, 0f, GetPoopCapacity());
        }
    }
}
