using Klei.AI;
using UnityEngine;

namespace MoreEmotions
{
    using static MoreEmotionsEffects;

    public class RespectGraveReactable : EmoteReactable
    {
        public RespectGraveReactable(GameObject gameObject) : base(gameObject,
            id: "Respect_Grave",
            chore_type: Db.Get().ChoreTypes.Emote,
            range_width: 3,
            range_height: 1,
            globalCooldown: 0.2f * RespectGrave.duration,
            localCooldown: RespectGrave.duration)
        {
            initialDelay = globalCooldown;
        }

        public override bool InternalCanBegin(GameObject new_reactor, Navigator.ActiveTransition transition)
        {
            if (new_reactor.TryGetComponent(out ChoreDriver driver) && driver.HasChore() && driver.GetCurrentChore() is MournChore)
                return false;
            if (new_reactor.TryGetComponent(out Effects effects) && effects.HasEffect(RespectGrave))
                return false;
            return base.InternalCanBegin(new_reactor, transition);
        }

        public override void InternalBegin()
        {
            if (reactor.TryGetComponent(out MinionResume resume) && !string.IsNullOrEmpty(resume.CurrentHat))
            {
                SetEmote(MoreMinionEmotes.Instance.Respect);
                RegisterEmoteStepCallbacks("react", null, AddEffect);
            }
            else
            {
                SetEmote(MoreMinionEmotes.Instance.Respect_NoHat);
                RegisterEmoteStepCallbacks("react_no_hat", null, AddEffect);
            }
            base.InternalBegin();
        }

        private static void AddEffect(GameObject reactor)
        {
            if (MoreEmotionsOptions.Instance.respect_grave_add_effect
                && !reactor.IsNullOrDestroyed() && reactor.TryGetComponent(out Effects effects))
                effects.Add(RespectGrave, true);
        }
    }
}
