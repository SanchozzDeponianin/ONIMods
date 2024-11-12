using UnityEngine;

namespace LargeTelescope
{
    public class TelescopeGasProvider : KMonoBehaviour, OxygenBreather.IGasProvider
    {
        private static readonly EventSystem.IntraObjectHandler<TelescopeGasProvider> OnStorageChangeDelegate =
            new EventSystem.IntraObjectHandler<TelescopeGasProvider>((component, data) => component.CheckStorageIsEmpty());

#pragma warning disable CS0649
        [MyCmpReq]
        private Storage storage;
#pragma warning restore CS0649

        // модификатор скорости работы телескопов, умножается на dt
        // хоть и не имеет отношения к снабжению кислородом
        // храним сдесь, чтобы не плодить компоненты
        [SerializeField]
        public float efficiencyMultiplier = 1f;

        private OxygenBreather oxygenBreather;
        public bool HasBreather => oxygenBreather != null;
        public bool IsEmpty { get; private set; }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            CheckStorageIsEmpty();
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            base.OnCleanUp();
        }

        private void CheckStorageIsEmpty()
        {
            var mass = storage.GetMassAvailable(SimHashes.Oxygen);
            if (IsEmpty && mass >= ConduitFlow.MAX_GAS_MASS)
                IsEmpty = false;
            else if (!IsEmpty && mass <= 0f)
                IsEmpty = true;
        }

        // заменяем и восстанавливаем GasProvider. 
        // сделано не как у клеев, чтобы исправить эксплоит и баг со снятием костюма.
        public void OverrideGasProvider(WorkerBase worker)
        {
            if (worker != null && worker.TryGetComponent<OxygenBreather>(out var breather) && !ReferenceEquals(this, breather.GetGasProvider()))
                breather.SetGasProvider(this);
        }

        public void RestoreGasProvider()
        {
            if (oxygenBreather != null && ReferenceEquals(this, oxygenBreather.GetGasProvider()))
            {
                if (TryGetSuitTank(oxygenBreather, out var suitTank))
                    oxygenBreather.SetGasProvider(suitTank);
                else
                    oxygenBreather.SetGasProvider(new GasBreatherFromWorldProvider());
            }
        }

        private bool TryGetSuitTank(KMonoBehaviour kmb, out SuitTank suitTank)
        {
            suitTank = null;
            if (kmb.TryGetComponent<MinionIdentity>(out var identity))
            {
                var equipment = identity.GetEquipment();
                if (equipment != null)
                {
                    var assignable = equipment.GetSlot(Db.Get().AssignableSlots.Suit)?.assignable;
                    if (assignable != null)
                        return assignable.TryGetComponent(out suitTank);
                }
            }
            return false;
        }

        public bool ConsumeGas(OxygenBreather oxygen_breather, float amount)
        {
            if (storage.items.Count > 0)
            {
                var go = storage.items[0];
                if (go != null && go.TryGetComponent<PrimaryElement>(out var pe))
                {
                    float consumed = Mathf.Min(pe.Mass, amount);
                    if (consumed > 0)
                    {
                        pe.Mass -= consumed;
                        Game.Instance.accumulators.Accumulate(oxygen_breather.O2Accumulator, consumed);
                        ReportManager.Instance.ReportValue(ReportManager.ReportType.OxygenCreated, -consumed, oxygen_breather.GetProperName());
                        return true;
                    }
                }
            }
            IsEmpty = true;
            return false;
        }

        public void OnClearOxygenBreather(OxygenBreather oxygen_breather) => oxygenBreather = null;
        public void OnSetOxygenBreather(OxygenBreather oxygen_breather) => oxygenBreather = oxygen_breather;
        public bool ShouldEmitCO2() => false;
        public bool ShouldStoreCO2() => false;
        public virtual bool IsLowOxygen() => IsEmpty;
    }
}
