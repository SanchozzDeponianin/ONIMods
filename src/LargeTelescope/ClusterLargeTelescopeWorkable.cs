using UnityEngine;

namespace LargeTelescope
{
    public class ClusterLargeTelescopeWorkable : ClusterTelescope.ClusterTelescopeWorkable, IGameObjectEffectDescriptor
    {
#pragma warning disable CS0649
        [MySmiReq]
        private ClusterTelescope.Instance _telescope;

        [MyCmpReq]
        private Storage _storage;
#pragma warning restore CS0649

        [SerializeField]
        public float efficiencyMultiplier = 1.5f;
        private bool isEmpty;
        private bool dirty;

        private static readonly EventSystem.IntraObjectHandler<ClusterLargeTelescopeWorkable> OnStorageChangeDelegate =
            new EventSystem.IntraObjectHandler<ClusterLargeTelescopeWorkable>((component, data) => component.CheckStorageIsEmpty());

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            CheckStorageIsEmpty();
            dirty = false;
        }
        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            base.OnCleanUp();
        }

        public override float GetEfficiencyMultiplier(Worker worker)
        {
            return efficiencyMultiplier * base.GetEfficiencyMultiplier(worker);
        }

        internal void CheckStorageIsEmpty()
        {
            var mass = _storage.GetMassAvailable(SimHashes.Oxygen);
            bool empty = isEmpty;
            if (empty && mass > TUNING.DUPLICANTSTATS.BASESTATS.OXYGEN_USED_PER_SECOND)
                empty = false;
            else if (!empty && mass <= 0f)
                empty = true;
            if (isEmpty != empty)
            {
                isEmpty = empty;
                dirty = true;
            }
        }

        protected override void OnStartWork(Worker worker)
        {
            if (_telescope.def.providesOxygen && !isEmpty)
            {
                dirty = false;
                OverrideGasProvider(worker);
            }
            base.OnStartWork(worker);
        }

        protected override bool OnWorkTick(Worker worker, float dt)
        {
            if (dirty)
            {
                dirty = false;
                if (_telescope.def.providesOxygen)
                {
                    if (isEmpty)
                        RestoreGasProvider(worker);
                    else
                        OverrideGasProvider(worker);
                }
            }
            return base.OnWorkTick(worker, dt);
        }

        protected override void OnStopWork(Worker worker)
        {
            if (_telescope.def.providesOxygen)
                RestoreGasProvider(worker);
            base.OnStopWork(worker);
        }

        // заменяем и восстанавливаем GasProvider. 
        // сделано не как у клеев, чтобы исправить эксплоит и баг со снятием костюма.
        private void OverrideGasProvider(Worker worker)
        {
            if (worker != null && worker.TryGetComponent<OxygenBreather>(out var breather))
                breather.SetGasProvider(this);
        }

        private void RestoreGasProvider(Worker worker)
        {
            if (worker != null && worker.TryGetComponent<OxygenBreather>(out var breather) && ReferenceEquals(this, breather.GetGasProvider()))
            {
                var suitTank = worker.GetComponent<MinionIdentity>()?.GetEquipment()?
                    .GetSlot(Db.Get().AssignableSlots.Suit)?.assignable?.GetComponent<SuitTank>();
                if (suitTank != null && !suitTank.IsEmpty())
                    breather.SetGasProvider(suitTank);
                else
                    breather.SetGasProvider(new GasBreatherFromWorldProvider());
            }
        }
    }
}
