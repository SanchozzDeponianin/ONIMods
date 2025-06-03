using Klei.AI;
using PeterHan.PLib.Detours;

namespace BetterPlantTending
{
    using handler = EventSystem.IntraObjectHandler<ExtendedFertilizationIrrigationMonitor>;
    public class ExtendedFertilizationIrrigationMonitor : KMonoBehaviour
    {
        // компонент для ряда дополнительных проверок что надо остановить поглощение удобрений и жидкостей
        // если растение не растёт по иным причинам чем засыхание

        private static readonly handler OnDelegate = new((component, data) => component.QueueUpdateAbsorbing());

#pragma warning disable CS0649
        [MySmiGet]
        private FertilizationMonitor.Instance fertilization;

        [MySmiGet]
        private IrrigationMonitor.Instance irrigation;

        [MyCmpGet]
        private Growing growing;

        [MySmiGet]
        private PlantBranchGrower.Instance grower;

        [MySmiGet]
        private SpaceTreePlant.Instance siropTree;
#pragma warning restore CS0649

        private bool shouldAbsorb = true;
        private int subscribeCount = 0;
        private bool dirty = true;
        private SchedulerHandle updateHandle;

        public bool ShouldAbsorb
        {
            get
            {
                if (dirty)
                {
                    UpdateShouldAbsorb();
                    dirty = false;
                }
                return shouldAbsorb;
            }
        }

        public static void Subscribe(StateMachine.Instance smi)
        {
            if (!BetterPlantTendingOptions.Instance.prevent_fertilization_irrigation_not_growning)
                return;
            if (smi.IsNullOrDestroyed() || smi.GetMaster().IsNullOrDestroyed())
                return;
            var monitor = smi.GetComponent<ExtendedFertilizationIrrigationMonitor>();
            if (monitor != null)
                monitor.Subscribe();
        }

        public static void Unsubscribe(StateMachine.Instance smi)
        {
            if (smi.IsNullOrDestroyed() || smi.GetMaster().IsNullOrDestroyed())
                return;
            var monitor = smi.GetComponent<ExtendedFertilizationIrrigationMonitor>();
            if (monitor != null)
                monitor.Unsubscribe();
        }

        public static void QueueUpdateAbsorbing(StateMachine.Instance smi)
        {
            if (!BetterPlantTendingOptions.Instance.prevent_fertilization_irrigation_not_growning)
                return;
            if (smi.IsNullOrDestroyed() || smi.GetMaster().IsNullOrDestroyed())
                return;
            var monitor = smi.GetComponent<ExtendedFertilizationIrrigationMonitor>();
            if (monitor != null && monitor.subscribeCount > 0)
                monitor.QueueUpdateAbsorbing();
        }

        private void Subscribe()
        {
            if (subscribeCount++ == 0)
            {
                Subscribe((int)GameHashes.Grow, OnDelegate);
                Subscribe((int)GameHashes.Wilt, OnDelegate);
                Subscribe((int)GameHashes.CropWakeUp, OnDelegate);
                Subscribe((int)GameHashes.CropSleep, OnDelegate);
                Subscribe((int)GameHashes.TreeBranchCountChanged, OnDelegate);
            }
        }

        private void Unsubscribe()
        {
            if (subscribeCount > 0)
            {
                subscribeCount--;
                if (subscribeCount <= 0)
                    ForceUnsubscribe();
            }
        }

        private void ForceUnsubscribe()
        {
            Unsubscribe((int)GameHashes.Grow, OnDelegate, true);
            Unsubscribe((int)GameHashes.Wilt, OnDelegate, true);
            Unsubscribe((int)GameHashes.CropWakeUp, OnDelegate, true);
            Unsubscribe((int)GameHashes.CropSleep, OnDelegate, true);
            Unsubscribe((int)GameHashes.TreeBranchCountChanged, OnDelegate, true);
        }

        protected override void OnCleanUp()
        {
            ForceUnsubscribe();
            if (updateHandle.IsValid)
                updateHandle.ClearScheduler();
            base.OnCleanUp();
        }

        private void UpdateAbsorbing(object _)
        {
            dirty = true;
            // тут стартуем поглощение, так как проверка должна быть встроена в патчи
            if (!fertilization.IsNullOrStopped() && fertilization.IsInsideState(fertilization.sm.replanted.fertilized.absorbing))
                fertilization.StartAbsorbing();
            if (!irrigation.IsNullOrStopped() && irrigation.IsInsideState(irrigation.sm.replanted.irrigated.absorbing))
                irrigation.UpdateAbsorbing(true);
        }

        private void QueueUpdateAbsorbing()
        {
            dirty = true;
            if (updateHandle.IsValid)
                updateHandle.ClearScheduler();
            updateHandle = GameScheduler.Instance.Schedule("QueueUpdateAbsorbing", 2 * UpdateManager.SecondsPerSimTick, UpdateAbsorbing);
        }

        private static IDetouredField<SpaceTreeBranch.Instance, AmountInstance> Maturity
            = PDetours.DetourField<SpaceTreeBranch.Instance, AmountInstance>("maturity");

        private void UpdateShouldAbsorb()
        {
            if (growing != null)
            {
                bool fullyGrown = growing.ReachedNextHarvest();
                if (fullyGrown && !siropTree.IsNullOrStopped())
                {
                    // поглощение включено если сироповое дерево генерирует сироп
                    if (siropTree.IsInsideState(siropTree.sm.production.producing))
                    {
                        shouldAbsorb = true;
                        return;
                    }
                }
                if (fullyGrown && !grower.IsNullOrStopped())
                {
                    // проверка всех веток дерева
                    // поглощение включено если есть растущие ветки
                    int growing_branches = 0;
                    if (grower.CurrentBranchCount > 0)
                        grower.ActionPerBranch(branch =>
                        {
                            if (branch.TryGetComponent<Growing>(out var branch_growing))    // деревянные ветки
                            {
                                if (branch_growing.IsGrowing() && !branch_growing.ReachedNextHarvest())
                                {
                                    growing_branches++;
                                }
                            }
                            else
                            {
                                var stbi = branch.GetSMI<SpaceTreeBranch.Instance>();       // сироповые ветки
                                if (!stbi.IsNullOrStopped() && !stbi.IsBranchFullyGrown)
                                {
                                    var maturity = Maturity.Get(stbi);
                                    if (maturity.GetDelta() > 0)
                                        growing_branches++;
                                }
                            }
                        });
                    shouldAbsorb = growing_branches > 0;
                }
                // в иных случаях поглощение включено если растение может расти и не выросло полностью
                else
                    shouldAbsorb = !fullyGrown && growing.IsGrowing();
            }
        }
    }
}
