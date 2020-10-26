using Klei.AI;
using TUNING;
using STRINGS;
using System.Collections.Generic;
using UnityEngine;

namespace CarouselCentrifuge
{
    public class CarouselCentrifugeWorkable : Workable, IWorkerPrioritizable, IGameObjectEffectDescriptor
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Operational operational;
#pragma warning restore CS0649

        private Chore chore;
        private static StatusItem vomitStatusItem;
        private static float vomitChancePercent = 5f;
        private static readonly int basePriority = RELAXATION.PRIORITY.TIER5;
        public static readonly string specificEffect = "RideonCarousel";
        public static readonly string trackingEffect = "RecentlyRideonCarousel";

        // эмоции после успешного катания
        private static Tuple<HashedString, HashedString[]>[] emote_anims =
        {
            new Tuple<HashedString, HashedString[]>("anim_cheer_kanim", new HashedString[] { "cheer_pre", "cheer_loop", "cheer_pst" }),
            new Tuple<HashedString, HashedString[]>("anim_clapcheer_kanim", new HashedString[] { "clapcheer_pre", "clapcheer_loop", "clapcheer_pst" }),
            new Tuple<HashedString, HashedString[]>("anim_react_thumbsup_kanim", new HashedString[] { "react" }),
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
            SetWorkTime(30f);
            if (vomitStatusItem == null)
            {
                vomitStatusItem = new StatusItem("CarouselVomiting", STRINGS.DUPLICANTS.STATUSITEMS.CAROUSELVOMITING.NAME, STRINGS.DUPLICANTS.STATUSITEMS.CAROUSELVOMITING.TOOLTIP, string.Empty, StatusItem.IconType.Info, NotificationType.BadMinor, false, OverlayModes.None.ID);
                vomitChancePercent = Config.Get().DizzinessChancePercent;
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            chore = CreateChore();
        }

        private Chore CreateChore()
        {
            WorkChore<CarouselCentrifugeWorkable> workChore = new WorkChore<CarouselCentrifugeWorkable>(
                chore_type: Db.Get().ChoreTypes.Relax,
                target: this,
                schedule_block: Db.Get().ScheduleBlockTypes.Recreation,
                allow_in_red_alert: false,
                allow_prioritization: false,
                priority_class: PriorityScreen.PriorityClass.high);
            workChore.AddPrecondition(ChorePreconditions.instance.CanDoWorkerPrioritizable, this);
            return workChore;
        }

        protected override void OnStartWork(Worker worker)
        {
            base.OnStartWork(worker);
            operational.SetActive(true, false);
        }

        protected override void OnCompleteWork(Worker worker)
        {
            base.OnCompleteWork(worker);
            bool flag = (Random.Range(0f, 100f) > vomitChancePercent);

            Effects effects = worker.GetComponent<Effects>();
            if (effects != null)
            {
                effects.Add(trackingEffect, true);
                if (flag) effects.Add(specificEffect, true);

                ChoreProvider chore_provider = worker.GetComponent<ChoreProvider>();
                if (chore_provider != null)
                {
                    if (flag)
                    {
                        int i = Random.Range(0, emote_anims.Length);
                        new EmoteChore(chore_provider, Db.Get().ChoreTypes.EmoteHighPriority, emote_anims[i].first, emote_anims[i].second, null);
                    }
                    else
                    {
                        Notification notification = new Notification(STRINGS.DUPLICANTS.STATUSITEMS.CAROUSELVOMITING.NOTIFICATION_NAME, NotificationType.BadMinor, HashedString.Invalid, (List<Notification> notificationList, object data) => STRINGS.DUPLICANTS.STATUSITEMS.CAROUSELVOMITING.NOTIFICATION_TOOLTIP + notificationList.ReduceMessages(false), null, true, 0f);

                        new VomitChore(Db.Get().ChoreTypes.Vomit, chore_provider, vomitStatusItem, notification, null);
                    }
                }
            }
            if (chore != null && !chore.isComplete)
            {
                chore.Cancel("completed but not complete??");
            }
            chore = CreateChore();
        }

        protected override void OnStopWork(Worker worker)
        {
            base.OnStopWork(worker);
            operational.SetActive(false, false);
        }

        public bool GetWorkerPriority(Worker worker, out int priority)
        {
            priority = basePriority;
            Effects effects = worker.GetComponent<Effects>();
            if (effects.HasEffect(trackingEffect))
            {
                priority = 0;
                return false;
            }
            if (effects.HasEffect(specificEffect))
            {
                priority = RELAXATION.PRIORITY.RECENTLY_USED;
            }
            return true;
        }

        
        List<Descriptor> IGameObjectEffectDescriptor.GetDescriptors(GameObject go)
        {
            List<Descriptor> list = new List<Descriptor>();
            Descriptor item = default(Descriptor);
            item.SetupDescriptor(UI.BUILDINGEFFECTS.RECREATION, UI.BUILDINGEFFECTS.TOOLTIPS.RECREATION, Descriptor.DescriptorType.Effect);
            list.Add(item);
            Effect.AddModifierDescriptions(gameObject, list, specificEffect, true);
            return list;
        }
        
    }
}
