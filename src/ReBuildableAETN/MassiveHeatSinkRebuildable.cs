using System;
using System.Linq;
using KSerialization;
using TUNING;
using UnityEngine;
using static STRINGS.UI.UISIDESCREENS.STUDYABLE_SIDE_SCREEN;

namespace ReBuildableAETN
{
    public class MassiveHeatSinkRebuildable : Workable, ISidescreenButtonControl
    {
        private const float STUDY_WORK_TIME = 3600f;

        private static readonly EventSystem.IntraObjectHandler<MassiveHeatSinkRebuildable> OnDeconstructCompleteDelegate =
            new EventSystem.IntraObjectHandler<MassiveHeatSinkRebuildable>(
                (MassiveHeatSinkRebuildable component, object data) => component.OnDeconstructComplete(data));

        private bool isConstructed = false;

        [Serialize]
        private bool studied = false;

        [Serialize]
        private bool markedForStudy = false;

        private Chore chore;
        private Guid statusItemGuid;

        private static StatusItem Studied;

#pragma warning disable CS0649
        [MyCmpReq]
        private Deconstructable deconstructable;

        [MyCmpReq]
        private Building building;

        [MyCmpReq]
        private BuildingComplete buildingComplete;
#pragma warning restore CS0649

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_use_machine_kanim") };
            faceTargetWhenWorking = true;
            synchronizeAnims = false;
            workerStatusItem = Db.Get().DuplicantStatusItems.Studying;
            resetProgressOnStop = false;
            requiredSkillPerk = Db.Get().SkillPerks.CanStudyWorldObjects.Id;
            attributeConverter = Db.Get().AttributeConverters.ResearchSpeed;
            attributeExperienceMultiplier = DUPLICANTSTATS.ATTRIBUTE_LEVELING.MOST_DAY_EXPERIENCE;
            skillExperienceSkillGroup = Db.Get().SkillGroups.Research.Id;
            skillExperienceMultiplier = SKILLS.MOST_DAY_EXPERIENCE;
            SetWorkTime(STUDY_WORK_TIME);
            if (Studied == null)
            {
                Studied = new StatusItem("MassiveHeatSink_Studied", "MISC", "", StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID);
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // убираем упоминание едра, чтобы в сейф не записывалось, на всякий...
            if (deconstructable != null && deconstructable.constructionElements != null
                && deconstructable.constructionElements.Contains(MassiveHeatSinkCoreConfig.tag))
            {
                deconstructable.constructionElements = deconstructable.constructionElements.Where(tag => tag != MassiveHeatSinkCoreConfig.tag).ToArray();
            }
            // был ли этот аетн построен или заспавнен
            if (buildingComplete != null && buildingComplete.creationTime > 0)
            {
                isConstructed = true;
                studied = true;
                markedForStudy = false;
            }
            Subscribe((int)GameHashes.DeconstructComplete, OnDeconstructCompleteDelegate);
            Refresh();
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.DeconstructComplete, OnDeconstructCompleteDelegate);
            base.OnCleanUp();
        }

        // спавним едро
        private static readonly Vector2 INITIAL_VELOCITY_RANGE = new Vector2(0.5f, 4f);
        private void OnDeconstructComplete(object data)
        {
            var prefab = Assets.GetPrefab(MassiveHeatSinkCoreConfig.tag);
            var extents = building.GetExtents();
            var cells = new int[] {
                Grid.XYToCell(extents.x, extents.y),
                Grid.XYToCell(extents.x + extents.width - 1, extents.y) };
            foreach (var cell in cells)
            {
                var result = GameUtil.KInstantiate(prefab, Grid.CellToPosCBC(cell, Grid.SceneLayer.Ore), Grid.SceneLayer.Ore);
                result.SetActive(true);
                result.GetComponent<SpaceArtifact>().RemoveCharm();
                var initial_velocity = new Vector2(UnityEngine.Random.Range(-1f, 1f) * INITIAL_VELOCITY_RANGE.x, INITIAL_VELOCITY_RANGE.y);
                if (GameComps.Fallers.Has(result))
                    GameComps.Fallers.Remove(result);
                GameComps.Fallers.Add(result, initial_velocity);
            }
        }

        public void CreateChore()
        {
            if (chore == null)
            {
                chore = new WorkChore<MassiveHeatSinkRebuildable>(Db.Get().ChoreTypes.Research, this, null, true, null, null, null, true, null, false, false, null, false, true, true, PriorityScreen.PriorityClass.basic, 5, false, true);
            }
        }

        public void CancelChore()
        {
            if (chore != null)
            {
                chore.Cancel("Studyable.CancelChore");
                chore = null;
            }
        }

        public void Refresh()
        {
            if (!isLoadingScene)
            {
                KSelectable selectable = GetComponent<KSelectable>();
                if (isConstructed)
                {
                    statusItemGuid = selectable.RemoveStatusItem(statusItemGuid, false);
                    requiredSkillPerk = null;
                    shouldShowSkillPerkStatusItem = false;
                    deconstructable.allowDeconstruction = true;
                }
                else
                {
                    if (studied)
                    {
                        statusItemGuid = selectable.ReplaceStatusItem(statusItemGuid, Studied, null);
                        requiredSkillPerk = null;
                        shouldShowSkillPerkStatusItem = false;
                        deconstructable.allowDeconstruction = true;
                    }
                    else
                    {
                        if (markedForStudy)
                        {
                            CreateChore();
                            statusItemGuid = selectable.ReplaceStatusItem(statusItemGuid, Db.Get().MiscStatusItems.AwaitingStudy, null);
                            shouldShowSkillPerkStatusItem = true;
                        }
                        else
                        {
                            CancelChore();
                            statusItemGuid = selectable.RemoveStatusItem(statusItemGuid, false);
                            shouldShowSkillPerkStatusItem = true;
                        }
                    }
                }
                UpdateStatusItem(null);
            }
        }

        private void ToggleStudyChore()
        {
            if (DebugHandler.InstantBuildMode)
            {
                studied = true;
                markedForStudy = false;
                CancelChore();
            }
            else
            {
                markedForStudy = !markedForStudy;
            }
            Refresh();
        }

        protected override void OnCompleteWork(Worker worker)
        {
            base.OnCompleteWork(worker);
            studied = true;
            chore = null;
            Refresh();
        }

        public override Vector3 GetFacingTarget()
        {
            return transform.GetPosition() + Vector3.right;
        }

        public string SidescreenButtonText => (studied ? STUDIED_BUTTON : (markedForStudy ? PENDING_BUTTON : SEND_BUTTON));

        public string SidescreenButtonTooltip => (studied ? STUDIED_STATUS : (markedForStudy ? PENDING_STATUS : SEND_STATUS));

        public bool SidescreenEnabled()
        {
            return !studied;
        }

        public bool SidescreenButtonInteractable()
        {
            return !studied;
        }

        public void OnSidescreenButtonPressed()
        {
            ToggleStudyChore();
        }

        public int ButtonSideScreenSortOrder()
        {
            return 20;
        }
    }
}
