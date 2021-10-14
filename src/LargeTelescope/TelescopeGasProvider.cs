using System.Collections.Generic;
using Klei;
using STRINGS;
using UnityEngine;

namespace LargeTelescope
{
    public class TelescopeGasProvider : KMonoBehaviour, OxygenBreather.IGasProvider, IGameObjectEffectDescriptor
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private Storage storage;

        [MyCmpReq]
        private ClusterTelescope.ClusterTelescopeWorkable workable;
#pragma warning restore CS0649

        private OxygenBreather.IGasProvider workerGasProvider;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            workable.OnWorkableEventCB += OnWorkableEvent;
        }

        private void OnWorkableEvent(Workable.WorkableEvent ev)
        {
            // todo: сделать чтобы возможность дышать из телескопа подключалась отключалась при пустом полном хранилище
            var worker = workable.worker;
            if (worker != null)
            {
                var breather = worker.GetComponent<OxygenBreather>();
                var kPrefabID = worker.GetComponent<KPrefabID>();
                switch (ev)
                {
                    case Workable.WorkableEvent.WorkStarted:
                        workerGasProvider = breather.GetGasProvider();
                        breather.SetGasProvider(this);
                        kPrefabID.AddTag(GameTags.Shaded, false);
                        break;
                    case Workable.WorkableEvent.WorkStopped:
                        breather.SetGasProvider(workerGasProvider);
                        kPrefabID.RemoveTag(GameTags.Shaded);
                        break;
                }
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

        public void OnClearOxygenBreather(OxygenBreather oxygen_breather)
        {
        }

        public void OnSetOxygenBreather(OxygenBreather oxygen_breather)
        {
        }

        public bool ShouldEmitCO2() => false;

        public bool ShouldStoreCO2() => false;

        public List<Descriptor> GetDescriptors(GameObject go)
        {
            var list = new List<Descriptor>();
            var element = ElementLoader.FindElementByHash(SimHashes.Oxygen);
            var item = default(Descriptor);
            item.SetupDescriptor(element.tag.ProperName(), string.Format(BUILDINGS.PREFABS.TELESCOPE.REQUIREMENT_TOOLTIP, element.tag.ProperName()), Descriptor.DescriptorType.Requirement);
            list.Add(item);
            return list;
        }
    }
}
