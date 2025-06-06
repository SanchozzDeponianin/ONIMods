using TUNING;
using UnityEngine;

namespace Smelter
{
    public class SmelterWorkableEmpty : Workable
    {
        private static readonly HashedString[] WORK_ANIMS = new HashedString[2] { "sponge_pre", "sponge_loop" };
        private static readonly HashedString[] PST_ANIMS = new HashedString[1] { "sponge_pst" };

        public override Vector3 GetFacingTarget()
        {
            return transform.GetPosition() + Vector3.left;
        }

        public override Vector3 GetWorkOffset() => new(0.7f, 0);

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            workerStatusItem = Db.Get().DuplicantStatusItems.Cleaning;
            workingStatusItem = Db.Get().MiscStatusItems.Cleaning;
            attributeConverter = Db.Get().AttributeConverters.TidyingSpeed;
            attributeExperienceMultiplier = DUPLICANTSTATS.ATTRIBUTE_LEVELING.PART_DAY_EXPERIENCE;
            overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_algae_terarrium_kanim") };
            workAnims = WORK_ANIMS;
            workingPstComplete = PST_ANIMS;
            workingPstFailed = PST_ANIMS;
            synchronizeAnims = false;
            faceTargetWhenWorking = true;
        }
    }
}