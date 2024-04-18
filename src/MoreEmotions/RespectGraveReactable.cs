using UnityEngine;

namespace MoreEmotions
{
    public class RespectGraveReactable : EmoteReactable
    {
        public RespectGraveReactable(GameObject gameObject) : base(gameObject,
            id: "Respect_Grave",
            chore_type: Db.Get().ChoreTypes.Emote,
            range_width: 3,
            range_height: 3,
            globalCooldown: Constants.SECONDS_PER_CYCLE / 4,
            localCooldown: Constants.SECONDS_PER_CYCLE)
        { }

        public override bool InternalCanBegin(GameObject new_reactor, Navigator.ActiveTransition transition)
        {
            if (new_reactor.TryGetComponent(out ChoreDriver driver) && driver.HasChore() && driver.GetCurrentChore() is MournChore)
                return false;
            return base.InternalCanBegin(new_reactor, transition);
        }

        public override void InternalBegin()
        {
            if (reactor == null) return; // хак чтобы вызывать Begin() для выставления таймаута
            if (reactor.TryGetComponent(out MinionResume resume) && !string.IsNullOrEmpty(resume.CurrentHat))
                SetEmote(MoreMinionEmotes.Instance.Respect);
            else
                SetEmote(MoreMinionEmotes.Instance.Respect_NoHat);
            base.InternalBegin();
        }
    }
}
