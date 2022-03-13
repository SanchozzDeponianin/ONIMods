using PeterHan.PLib.Detours;

namespace BetterPlantTending
{
    public class ExtendedFertilizationIrrigationMonitor : TendedPlant
    {
        private static readonly EventSystem.IntraObjectHandler<ExtendedFertilizationIrrigationMonitor> OnDelegate =
            new EventSystem.IntraObjectHandler<ExtendedFertilizationIrrigationMonitor>((component, data) =>
            {
                component.dirty = true;
                component.QueueApplyModifier();
            });

        IDetouredField<TreeBud, Growing> branch_growing = PDetours.DetourField<TreeBud, Growing>("growing");

        protected override bool ApplyModifierOnEffectAdded => false;
        protected override bool ApplyModifierOnEffectRemoved => false;

#pragma warning disable CS0649
        [MySmiGet]
        FertilizationMonitor.Instance fertilization;

        [MySmiGet]
        IrrigationMonitor.Instance irrigation;

        [MyCmpGet]
        Growing growing;

        [MyCmpGet]
        BuddingTrunk buddingTrunk;
#pragma warning restore CS0649

        private bool subscribed;
        private bool shouldAbsorbing = true;
        private bool dirty = true;

        public void Subscribe()
        {
            if (!subscribed && BetterPlantTendingOptions.Instance.prevent_fertilization_irrigation_not_growning)
            {
                Subscribe((int)GameHashes.Grow, OnDelegate);
                Subscribe((int)GameHashes.Wilt, OnDelegate);
                Subscribe((int)GameHashes.CropWakeUp, OnDelegate);
                Subscribe((int)GameHashes.CropSleep, OnDelegate);
                subscribed = true;
            }
        }

        public void Unsubscribe()
        {
            if (subscribed)
            {
                Unsubscribe((int)GameHashes.Grow, OnDelegate);
                Unsubscribe((int)GameHashes.Wilt, OnDelegate);
                Unsubscribe((int)GameHashes.CropWakeUp, OnDelegate);
                Unsubscribe((int)GameHashes.CropSleep, OnDelegate);
                subscribed = false;
            }
        }

        protected override void OnCleanUp()
        {
            Unsubscribe();
            base.OnCleanUp();
        }

        public override void ApplyModifier()
        {
            // тут стартуем поглощение, так как проверка должна быть встроена в патчи
            if (!fertilization.IsNullOrStopped() && fertilization.IsInsideState(fertilization.sm.replanted.fertilized.absorbing))
                fertilization.StartAbsorbing();
            if (!irrigation.IsNullOrStopped() && irrigation.IsInsideState(irrigation.sm.replanted.irrigated.absorbing))
                irrigation.UpdateAbsorbing(true);
        }

        public bool ShouldAbsorbing()
        {
            if (dirty)
            {
                if (growing != null)
                {
                    if (buddingTrunk != null && growing.ReachedNextHarvest())
                    {
                        // проверка всех веток дерева
                        // поглощение включено если есть растущие ветки или их меньше максимума
                        int num_branches = 0;
                        int num_growing_branches = 0;
                        for (int i = 0; i < ForestTreeConfig.NUM_BRANCHES; i++)
                        {
                            var branch = buddingTrunk.GetBranchAtPosition(i);
                            if (branch != null)
                            {
                                num_branches++;
                                var bg = branch_growing.Get(branch);
                                if (bg != null && bg.IsGrowing() && !bg.ReachedNextHarvest())
                                    num_growing_branches++;
                            }
                        }
                        shouldAbsorbing = num_growing_branches > 0 || num_branches < buddingTrunk.maxBuds;
                    }
                    else
                        shouldAbsorbing = growing.IsGrowing() && !growing.ReachedNextHarvest();
                }
                dirty = false;
            }
            return shouldAbsorbing;
        }
    }
}
