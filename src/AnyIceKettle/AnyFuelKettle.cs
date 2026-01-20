using System.Collections.Generic;
using KSerialization;
using UnityEngine;

namespace AnyIceKettle
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class AnyFuelKettle : KMonoBehaviour, IUserControlledCapacity
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private Storage fuelStorage;

        [MyCmpReq]
        private ManualDeliveryKG fuel_mdkg;

        [MyCmpReq]
        private TreeFilterable filterable;

        [MySmiReq]
        private IceKettle.Instance kettle;

        [MyCmpReq]
        private AnyIceKettle anykettle;
#pragma warning restore CS0649

        private FilteredStorage filtered;

        [Serialize]
        private bool paused = false;

        [SerializeField]
        public List<Tag> discoverResourcesOnSpawn;

        public bool IsOperational => operational.IsOperational;

        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            fuel_mdkg.Pause(true, "filtered");
            fuelStorage.OnStorageChange += OnStorageChange;
            var chore_type = Db.Get().ChoreTypes.Get(fuel_mdkg.choreTypeIDHash);
            filtered = new(this, null, this, false, chore_type);
            filtered.storage = fuelStorage;
            Subscribe((int)GameHashes.OperationalChanged, filtered.OnFunctionalChanged);
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            if (discoverResourcesOnSpawn != null)
            {
                foreach (var tag in discoverResourcesOnSpawn)
                {
                    var element = ElementLoader.GetElement(tag);
                    if (element != null)
                        DiscoveredResources.Instance.Discover(element.tag, element.GetMaterialCategoryTag());
                }
            }
            filterable.OnFilterChanged += OnFilterChanged;
            CheckPause();
        }

        public override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.OperationalChanged, filtered.OnFunctionalChanged);
            if (fuelStorage != null)
                fuelStorage.OnStorageChange -= OnStorageChange;
            if (filterable != null)
                filterable.OnFilterChanged -= OnFilterChanged;
            filtered.CleanUp();
            base.OnCleanUp();
        }

        private void OnFilterChanged(HashSet<Tag> filter)
        {
            anykettle.SetPipedEverythingConsumer();
            if (kettle.IsInsideState(kettle.sm.operational.melting.working) && !IceKettle.CanMeltNextBatch(kettle))
            {
                kettle.GoTo(kettle.sm.operational.melting.exit);
                IceKettle.ResetMeltingTimer(kettle);
            }
        }

        private void OnStorageChange(object _) => CheckPause();

        private void CheckPause()
        {
            var mass = fuelStorage.MassStored();
            if ((!paused && (mass >= fuel_mdkg.Capacity - TUNING.STORAGE.STORAGE_LOCKER_FILLED_MARGIN))
                || (paused && (mass < fuel_mdkg.refillMass)))
            {
                paused = !paused;
                filtered.FilterChanged();
            }
        }

        // IUserControlledCapacity
        public bool ControlEnabled() => false;
        public float UserMaxCapacity { get => paused ? 0f : fuel_mdkg.Capacity; set { } }
        public float AmountStored => fuelStorage.MassStored();
        public float MinCapacity => 0f;
        public float MaxCapacity => fuelStorage.capacityKg;
        public bool WholeValues => false;
        public LocString CapacityUnits => GameUtil.GetCurrentMassUnit(false);
    }
}
