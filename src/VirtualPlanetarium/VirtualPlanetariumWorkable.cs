using System.Collections.Generic;
using Klei.AI;
using STRINGS;
using TUNING;
using UnityEngine;

namespace VirtualPlanetarium
{
    public class VirtualPlanetariumWorkable : Workable, IWorkerPrioritizable, IGameObjectEffectDescriptor
    {
        public const string USING_EFFECT = "Stargazing";
        public const string SPECIFIC_EFFECT = "Stargazed";
        public const string TRACKING_EFFECT = "RecentlyStargazed";

        public const float INGREDIENT_MASS_PER_USE = 1f;
        public static Tag INGREDIENT_TAG = ResearchDatabankConfig.TAG;

        private static readonly int basePriority = RELAXATION.PRIORITY.TIER5;
        private Chore chore;

        private static readonly EventSystem.IntraObjectHandler<VirtualPlanetariumWorkable> OnStorageChangeDelegate =
            new EventSystem.IntraObjectHandler<VirtualPlanetariumWorkable>(
                (VirtualPlanetariumWorkable component, object data) => component.UpdateChore());

#pragma warning disable CS0649
        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private Storage storage;
#pragma warning restore CS0649

        public VirtualPlanetariumWorkable()
        {
            SetReportType(ReportManager.ReportType.PersonalTime);
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            overrideAnims = new KAnimFile[]
            {
                Assets.GetAnim("anim_interacts_research_space_kanim")
            };
            workLayer = Grid.SceneLayer.BuildingFront;
            showProgressBar = true;
            resetProgressOnStop = true;
            synchronizeAnims = true;
            SetWorkTime(TUNING.BUILDINGS.WORK_TIME_SECONDS.MEDIUM_WORK_TIME);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            UpdateChore();
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            CancelChore();
            base.OnCleanUp();
        }

        private void CreateChore()
        {
            if (chore == null)
            {
                chore = new WorkChore<VirtualPlanetariumWorkable>(
                    chore_type: Db.Get().ChoreTypes.Relax,
                    target: this,
                    schedule_block: Db.Get().ScheduleBlockTypes.Recreation,
                    allow_in_red_alert: false,
                    allow_prioritization: false,
                    priority_class: PriorityScreen.PriorityClass.high
                    );
                chore.AddPrecondition(ChorePreconditions.instance.CanDoWorkerPrioritizable, this);
            }
        }

        private void CancelChore()
        {
            if (chore != null)
            {
                chore.Cancel("VirtualPlanetariumWorkable.CancelChore");
                chore = null;
            }
        }

        private bool IsReady()
        {
            return storage.GetMassAvailable(INGREDIENT_TAG) >= INGREDIENT_MASS_PER_USE;
        }

        private void UpdateChore()
        {
            if (IsReady()) CreateChore();
            else CancelChore();
        }

        protected override void OnStartWork(Worker worker)
        {
            base.OnStartWork(worker);
            worker.GetComponent<Effects>()?.Add(USING_EFFECT, false);
            operational.SetActive(true, false);
        }

        protected override void OnCompleteWork(Worker worker)
        {
            base.OnCompleteWork(worker);
            var effects = worker.GetComponent<Effects>();
            if (effects != null)
            {
                effects.Add(TRACKING_EFFECT, true);
                effects.Add(SPECIFIC_EFFECT, true);
            }
            chore = null;
            UpdateChore();
            storage.ConsumeAndGetDisease(INGREDIENT_TAG, INGREDIENT_MASS_PER_USE, out _, out _, out _);
        }

        protected override void OnStopWork(Worker worker)
        {
            base.OnStopWork(worker);
            worker.GetComponent<Effects>()?.Remove(USING_EFFECT);
            operational.SetActive(false, false);
        }

        public bool GetWorkerPriority(Worker worker, out int priority)
        {
            priority = basePriority;
            var effects = worker.GetComponent<Effects>();
            if (effects.HasEffect(TRACKING_EFFECT))
            {
                priority = 0;
                return false;
            }
            if (effects.HasEffect(SPECIFIC_EFFECT))
            {
                priority = RELAXATION.PRIORITY.RECENTLY_USED;
            }
            return true;
        }

        private void AddRequirementDesc(List<Descriptor> descs, Tag tag, float mass)
        {
            string arg = tag.ProperName();
            var item = default(Descriptor);
            item.SetupDescriptor(string.Format(UI.BUILDINGEFFECTS.ELEMENTCONSUMEDPERUSE, arg, GameUtil.GetFormattedMass(mass, GameUtil.TimeSlice.None, GameUtil.MetricMassFormat.Kilogram, true, "{0:0.##}")), string.Format(UI.BUILDINGEFFECTS.TOOLTIPS.ELEMENTCONSUMEDPERUSE, arg, GameUtil.GetFormattedMass(mass, GameUtil.TimeSlice.None, GameUtil.MetricMassFormat.Kilogram, true, "{0:0.##}")), Descriptor.DescriptorType.Requirement);
            descs.Add(item);
        }

        List<Descriptor> IGameObjectEffectDescriptor.GetDescriptors(GameObject go)
        {
            var item = default(Descriptor);
            item.SetupDescriptor(UI.BUILDINGEFFECTS.RECREATION, UI.BUILDINGEFFECTS.TOOLTIPS.RECREATION, Descriptor.DescriptorType.Effect);
            var list = new List<Descriptor> { item };
            Effect.AddModifierDescriptions(gameObject, list, SPECIFIC_EFFECT, true);
            AddRequirementDesc(list, INGREDIENT_TAG, INGREDIENT_MASS_PER_USE);
            return list;
        }
    }
}
