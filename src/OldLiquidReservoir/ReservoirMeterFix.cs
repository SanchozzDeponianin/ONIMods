using System;
using PeterHan.PLib.Detours;

namespace OldLiquidReservoir
{
    [SkipSaveFileSerialization]
    public class ReservoirMeterFix : KMonoBehaviour
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Reservoir reservoir;
#pragma warning restore CS0649

        private KBatchedAnimController kbac;
        private KBatchedAnimTracker tracker;

        private static readonly IDetouredField<Reservoir, MeterController> Reservoir_meter
            = PDetours.DetourFieldLazy<Reservoir, MeterController>("meter");

        private static readonly Action<KBatchedAnimController> kbac_UpdateFromSync
            = typeof(KBatchedAnimController).TryDetour<Action<KBatchedAnimController>>("UpdateFromSync");

        protected override void OnSpawn()
        {
            base.OnSpawn();
            var meter = Reservoir_meter.Get(reservoir);
            kbac = meter.meterController;
            kbac.TryGetComponent(out tracker);
            Subscribe((int)Patches.BuildingFacadeChanged, UpdateMeterDelegate, this);
        }

        private static readonly Action<object, object> UpdateMeterDelegate = (cmp, data) => ((ReservoirMeterFix)cmp).UpdateMeter();
        private void UpdateMeter()
        {
            if (!kbac.IsNullOrDestroyed() && kbac_UpdateFromSync != null)
                kbac_UpdateFromSync(kbac);
            if (!tracker.IsNullOrDestroyed())
                tracker.enabled = true;
        }
    }
}
