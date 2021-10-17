using Klei;
using UnityEngine;

namespace LargeTelescope
{
    public class ClusterLargeTelescopeWorkable : ClusterTelescope.ClusterTelescopeWorkable, OxygenBreather.IGasProvider, IGameObjectEffectDescriptor
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Storage storage;
#pragma warning restore CS0649

        [SerializeField]
        public float efficiencyMultiplier = 1.5f;
        private bool isEmpty;
        private OxygenBreather.IGasProvider workerGasProvider;

        private static readonly EventSystem.IntraObjectHandler<ClusterLargeTelescopeWorkable> OnStorageChangeDelegate =
            new EventSystem.IntraObjectHandler<ClusterLargeTelescopeWorkable>((component, data) => component.OnStorageChange());

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_telescope_kanim") };
            workLayer = Grid.SceneLayer.BuildingFront;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            OnStorageChange();
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

        private void OnStorageChange()
        {
            var mass = storage.GetMassAvailable(GameTags.Oxygen);
            if (isEmpty && mass > TUNING.DUPLICANTSTATS.BASESTATS.OXYGEN_USED_PER_SECOND)
                isEmpty = false;
            else if (!isEmpty && mass <= 0f)
                isEmpty = true;
        }

        protected override void OnStartWork(Worker worker)
        {
            base.OnStartWork(worker);
            if (worker != null)
            {
                if (!isEmpty)
                {
                    SetGasProvider(worker);
                }
                worker.AddTag(GameTags.Shaded);
            }
        }

        protected override bool OnWorkTick(Worker worker, float dt)
        {
            if (worker != null)
            {
                if (isEmpty)
                    ClearGasProvider(worker);
                else
                    SetGasProvider(worker);
            }
            return base.OnWorkTick(worker, dt);
        }

        protected override void OnStopWork(Worker worker)
        {
            if (worker != null)
            {
                ClearGasProvider(worker);
                worker.RemoveTag(GameTags.Shaded);
            }
            base.OnStopWork(worker);
        }

        private void SetGasProvider(Worker worker)
        {
            if (workerGasProvider == null)
            {
                var breather = worker.GetComponent<OxygenBreather>();
                workerGasProvider = breather.GetGasProvider();
                breather.SetGasProvider(this);
            }
        }

        private void ClearGasProvider(Worker worker)
        {
            if (workerGasProvider != null)
            {
                var breather = worker.GetComponent<OxygenBreather>();
                breather.SetGasProvider(workerGasProvider);
                workerGasProvider = null;
            }
        }

        public bool ConsumeGas(OxygenBreather oxygen_breather, float amount)
        {
            if (storage.items.Count <= 0)
                return false;
            storage.ConsumeAndGetDisease(GameTags.Oxygen, amount, out float amount_consumed, out SimUtil.DiseaseInfo _, out float _);
            bool result = amount_consumed > 0f;
            if (result)
            {
                Game.Instance.accumulators.Accumulate(oxygen_breather.O2Accumulator, amount_consumed);
                ReportManager.Instance.ReportValue(ReportManager.ReportType.OxygenCreated, 0f - amount_consumed, oxygen_breather.GetProperName(), null);
            }
            return result;
        }

        public void OnClearOxygenBreather(OxygenBreather oxygen_breather) { }
        public void OnSetOxygenBreather(OxygenBreather oxygen_breather) { }
        public bool ShouldEmitCO2() => false;
        public bool ShouldStoreCO2() => false;
    }
}
