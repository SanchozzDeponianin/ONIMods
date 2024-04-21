using UnityEngine;

namespace MoreEmotions
{
    public class KickLazyAssReactable : EmoteReactable
    {
        private Schedulable schedulable;
        private KBatchedAnimController kbak;
        public KickLazyAssReactable(GameObject gameObject) : base(gameObject,
            id: "KickLazyAss",
            chore_type: Db.Get().ChoreTypes.Emote,
            range_width: 1,
            range_height: 1,
            globalCooldown: 10f,
            localCooldown: 0f)
        {
            initialDelay = 2f;
            gameObject.TryGetComponent(out schedulable);
            gameObject.TryGetComponent(out kbak);
            SetEmote(MoreMinionEmotes.Instance.Kick);
            RegisterEmoteStepCallbacks("kick_loop", Trigger, null);
        }

        private void Trigger(GameObject reactor)
        {
            gameObject.Trigger((int)MoreEmotionsPatches.SleepDisturbedByKick);
        }

        public override bool InternalCanBegin(GameObject new_reactor, Navigator.ActiveTransition transition)
        {
            // для совместимости с фосфорными специями
            if (gameObject.HasTag(GameTags.EmitsLight)
                || schedulable.IsAllowed(Db.Get().ScheduleBlockTypes.Sleep))
                return false;
            return base.InternalCanBegin(new_reactor, transition);
        }

        public override void InternalBegin()
        {
            kbak.SetSceneLayer(Grid.SceneLayer.Creatures);
            base.InternalBegin();
        }

        public override void InternalEnd()
        {
            base.InternalEnd();
            kbak.SetSceneLayer(Grid.SceneLayer.Move);
        }
    }
}
