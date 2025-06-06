﻿using System.Collections.Generic;
using Klei.AI;
using STRINGS;
using TUNING;
using UnityEngine;

namespace CarouselCentrifuge
{
    public class CarouselCentrifugeWorkable : Workable, IWorkerPrioritizable, IGameObjectEffectDescriptor
    {
        public const string specificEffectName = "RideonCarousel";
        public const string trackingEffectName = "RecentlyRideonCarousel";

        private static StatusItem vomitStatusItem;
        private static float vomitChancePercent = 5f;
        private static readonly int basePriority = RELAXATION.PRIORITY.TIER5;
        private Attribute workAttribute = null;
        private Chore chore;

#pragma warning disable CS0649
        [MyCmpReq]
        private Operational operational;
#pragma warning restore CS0649

        // эмоции после успешного катания
        private static readonly Tuple<HashedString, HashedString[]>[] emote_anims =
        {
            new Tuple<HashedString, HashedString[]>("anim_cheer_kanim", new HashedString[] { "cheer_pre", "cheer_loop", "cheer_pst" }),
            new Tuple<HashedString, HashedString[]>("anim_clapcheer_kanim", new HashedString[] { "clapcheer_pre", "clapcheer_loop", "clapcheer_pst" }),
        };

        public CarouselCentrifugeWorkable()
        {
            SetReportType(ReportManager.ReportType.PersonalTime);
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            overrideAnims = new KAnimFile[]
            {
                Assets.GetAnim("anim_interacts_centrifuge_kanim")
            };
            workLayer = Grid.SceneLayer.BuildingFront;
            showProgressBar = true;
            resetProgressOnStop = true;
            synchronizeAnims = true;
            if (DlcManager.FeatureClusterSpaceEnabled() && ModOptions.Instance.EnableTraining)
            {
                workAttribute = Db.Get().Attributes.SpaceNavigation;
                attributeExperienceMultiplier = DUPLICANTSTATS.ATTRIBUTE_LEVELING.BARELY_EVER_EXPERIENCE;
                skillExperienceSkillGroup = Db.Get().SkillGroups.Rocketry.Id;
                skillExperienceMultiplier = SKILLS.BARELY_EVER_EXPERIENCE;
            }
            SetWorkTime(TUNING.BUILDINGS.WORK_TIME_SECONDS.MEDIUM_WORK_TIME);
            if (vomitStatusItem == null)
            {
                vomitStatusItem = new StatusItem("CarouselVomiting", STRINGS.DUPLICANTS.STATUSITEMS.CAROUSELVOMITING.NAME, STRINGS.DUPLICANTS.STATUSITEMS.CAROUSELVOMITING.TOOLTIP, string.Empty, StatusItem.IconType.Info, NotificationType.BadMinor, false, OverlayModes.None.ID);

            }
            vomitChancePercent = ModOptions.Instance.DizzinessChancePercent;
        }

        public override Attribute GetWorkAttribute() => workAttribute;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            chore = CreateChore();
        }

        private Chore CreateChore()
        {
            var workChore = new WorkChore<CarouselCentrifugeWorkable>(
                chore_type: Db.Get().ChoreTypes.Relax,
                target: this,
                schedule_block: Db.Get().ScheduleBlockTypes.Recreation,
                allow_in_red_alert: false,
                allow_prioritization: false,
                priority_class: PriorityScreen.PriorityClass.high
                );
            workChore.AddPrecondition(ChorePreconditions.instance.CanDoWorkerPrioritizable, this);
            return workChore;
        }

        protected override void OnStartWork(WorkerBase worker)
        {
            base.OnStartWork(worker);
            operational.SetActive(true, false);
        }

        protected override void OnCompleteWork(WorkerBase worker)
        {
            base.OnCompleteWork(worker);
            bool flag = (Random.Range(0f, 100f) > vomitChancePercent);
            if (worker.TryGetComponent<Effects>(out var effects))
            {
                effects.Add(trackingEffectName, true);
                if (flag) effects.Add(specificEffectName, true);
                if (worker.TryGetComponent<ChoreProvider>(out var chore_provider))
                {
                    if (flag)
                    {
                        int i = Random.Range(0, emote_anims.Length);
                        new EmoteChore(
                            target: chore_provider,
                            chore_type: Db.Get().ChoreTypes.EmoteHighPriority,
                            animFile: emote_anims[i].first,
                            anims: emote_anims[i].second,
                            get_status_item: null
                            );
                    }
                    else
                    {
                        var notification = new Notification(
                            title: STRINGS.DUPLICANTS.STATUSITEMS.CAROUSELVOMITING.NOTIFICATION_NAME,
                            type: NotificationType.BadMinor,
                            tooltip: (List<Notification> notificationList, object data) => STRINGS.DUPLICANTS.STATUSITEMS.CAROUSELVOMITING.NOTIFICATION_TOOLTIP + notificationList.ReduceMessages(false)
                            );
                        new VomitChore(
                            chore_type: Db.Get().ChoreTypes.Vomit,
                            target: chore_provider,
                            status_item: vomitStatusItem,
                            notification: notification
                            );
                    }
                }
            }
            if (chore != null && !chore.isComplete)
            {
                chore.Cancel("completed but not complete??");
            }
            chore = CreateChore();
        }

        protected override void OnStopWork(WorkerBase worker)
        {
            base.OnStopWork(worker);
            operational.SetActive(false, false);
        }

        public bool GetWorkerPriority(WorkerBase worker, out int priority)
        {
            priority = basePriority;
            if (worker.TryGetComponent<Effects>(out var effects))
            {
                if (effects.HasEffect(trackingEffectName))
                {
                    priority = 0;
                    return false;
                }
                if (effects.HasEffect(specificEffectName))
                {
                    priority = RELAXATION.PRIORITY.RECENTLY_USED;
                }
            }
            return true;
        }

        List<Descriptor> IGameObjectEffectDescriptor.GetDescriptors(GameObject go)
        {
            Descriptor item = default;
            item.SetupDescriptor(UI.BUILDINGEFFECTS.RECREATION, UI.BUILDINGEFFECTS.TOOLTIPS.RECREATION, Descriptor.DescriptorType.Effect);
            var list = new List<Descriptor> { item };
            Effect.AddModifierDescriptions(gameObject, list, specificEffectName, true);
            return list;
        }
    }
}
