using UnityEngine;

namespace MoreEmotions
{
    public class KickLazyAssReactable : EmoteReactable
    {
        private Schedulable schedulable;
        private KBatchedAnimController kbak;
        private bool secondaryAnimSet = false;
        private KAnimFile kickKanim;
        private KAnimFile breakKanim;

        public KickLazyAssReactable(GameObject gameObject, bool punch) : base(gameObject,
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
            if (Random.value < 0.2f) // орать
            {
                SetEmote(MoreMinionEmotes.Instance.Rage);
                RegisterEmoteStepCallbacks("rage_loop", null, Trigger);
            }
            else
            {
                if (punch) // бить
                {
                    secondaryAnimSet = true;
                    SetEmote(MoreMinionEmotes.Instance.BreakPunch);
                    RegisterEmoteStepCallbacks("break_loop_punch", null, Trigger);
                }
                else if (Random.value < 0.5f) // пинать
                {
                    secondaryAnimSet = true;
                    SetEmote(MoreMinionEmotes.Instance.BreakKick);
                    RegisterEmoteStepCallbacks("break_loop_kick", null, Trigger);
                }
                else // вариант
                {
                    SetEmote(MoreMinionEmotes.Instance.Kick);
                    RegisterEmoteStepCallbacks("kick_loop", null, Trigger);
                }
            }
            if (secondaryAnimSet)
            {
                kickKanim = Assets.GetAnim("anim_emotes_default_kanim");
                breakKanim = Assets.GetAnim("anim_break_kanim");
            }
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
            if (secondaryAnimSet)
            {
                emote.ApplyAnimOverrides(kbac, kickKanim);
                emote.ApplyAnimOverrides(kbac, breakKanim);
            }
        }

        public override void InternalEnd()
        {
            if (secondaryAnimSet)
            {
                emote.RemoveAnimOverrides(kbac, kickKanim);
                emote.RemoveAnimOverrides(kbac, breakKanim);
            }
            base.InternalEnd();
            kbak.SetSceneLayer(Grid.SceneLayer.Move);
        }
    }
}
