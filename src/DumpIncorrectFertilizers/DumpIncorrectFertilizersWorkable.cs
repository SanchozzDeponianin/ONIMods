namespace DumpIncorrectFertilizers
{
    [SkipSaveFileSerialization]
    public class DumpIncorrectFertilizersWorkable : Workable
    {
#pragma warning disable CS0649
        [MyCmpReq]
        PlantablePlot plot;

        [MyCmpReq]
        Storage storage;
#pragma warning restore CS0649

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            SetOffsetTable(OffsetGroups.InvertedStandardTableWithCorners);
            ConfigureMultitoolContext("build", EffectConfigs.BuildSplashId);
            workerStatusItem = Db.Get().DuplicantStatusItems.Emptying;
            synchronizeAnims = false;
            faceTargetWhenWorking = true;
            SetWorkTime(1f);
        }

        protected override void OnCompleteWork(WorkerBase worker)
        {
            IrrigationMonitor.Instance.DumpIncorrectFertilizers(storage, plot.Occupant);
            base.OnCompleteWork(worker);
        }
    }
}
