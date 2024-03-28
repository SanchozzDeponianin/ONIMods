using UnityEngine;

namespace WakeUpLazyAss
{
    public class KickMinionReactable : Reactable
    {
        public KickMinionReactable(GameObject gameObject) : base(gameObject, nameof(KickMinionReactable), Db.Get().ChoreTypes.EmoteHighPriority, 1, 1, true, 10f, 0f, float.PositiveInfinity, 0f, ObjectLayer.Minion)
        {
        }

        public override bool InternalCanBegin(GameObject newReactor, Navigator.ActiveTransition transition)
        {
            if (reactor != null)
                return false;
            if (GameClock.Instance.GetTime() - creationTime < 2f)
                return false;
            // для совместимости с фосфорными специями
            if (gameObject.HasTag(GameTags.EmitsLight)
                && gameObject.TryGetComponent(out Schedulable schedulable)
                && schedulable.IsAllowed(Db.Get().ScheduleBlockTypes.Sleep))
                return false;
            return newReactor.TryGetComponent(out Navigator navigator) && navigator.IsMoving();
        }

        public override void Update(float dt)
        {
            reactor.GetComponent<Facing>().SetFacing(gameObject.GetComponent<Facing>().GetFacing());
        }

        protected override void InternalBegin()
        {
            if (reactor.TryGetComponent(out KAnimControllerBase kbac))
            {
                kbac.AddAnimOverrides(Assets.GetAnim("anim_emotes_default_kanim"), 0f);
                kbac.Play("kick_pre", KAnim.PlayMode.Once, 1f, 0f);
                kbac.Queue("kick_loop", KAnim.PlayMode.Once, 1f, 0f);
                kbac.Queue("kick_pst", KAnim.PlayMode.Once, 1f, 0f);
                kbac.onAnimComplete += Finish;
            }
        }

        private void Finish(HashedString anim)
        {
            if (anim == "kick_pst")
            {
                if (reactor != null && reactor.TryGetComponent(out KAnimControllerBase kbac))
                {
                    kbac.onAnimComplete -= Finish;
                    gameObject.Trigger((int)WakeUpLazyAssPatches.SleepDisturbedByKick);
                }
                End();
            }
        }

        protected override void InternalEnd() { }
        protected override void InternalCleanup() { }
    }
}
