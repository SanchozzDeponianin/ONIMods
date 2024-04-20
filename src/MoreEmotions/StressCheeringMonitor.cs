using Klei.AI;
using UnityEngine;

namespace MoreEmotions
{
    using static MoreEmotionsPatches;
    using static MoreEmotionsEffects;

    // успокаивание стрессующего
    public class StressCheeringMonitor : GameStateMachine<StressCheeringMonitor, StressCheeringMonitor.Instance>
    {
        public const float anim_timeout = 15f;

        public new class Instance : GameInstance
        {
            private Effects effects;
            private Facing facing;
            private KBatchedAnimController kbac;
            private Navigator navigator;
            public EmoteReactable StressedReactable;
            public EmoteReactable CheeringReactable;

            public Instance(IStateMachineTarget master) : base(master)
            {
                gameObject.TryGetComponent(out effects);
                gameObject.TryGetComponent(out facing);
                gameObject.TryGetComponent(out kbac);
                gameObject.TryGetComponent(out navigator);
            }

            public bool HasCheeringEffect()
            {
                return effects.HasEffect(StressedCheering);
            }

            public bool IsOnFloor(GameObject _, Navigator.ActiveTransition _1)
            {
                return navigator.IsMoving() && navigator.CurrentNavType == NavType.Floor;
            }

            public bool ReactorIsMe(GameObject reactor, Navigator.ActiveTransition _)
            {
                return reactor == gameObject;
            }

            public bool ReactorIsCloseEnough(GameObject reactor, Navigator.ActiveTransition _)
            {
                // если стрессующий движется к успокаивателю - 2-3 клетки, наоборот - 1-2 клетки
                var offset = transform.GetPosition().x - reactor.transform.GetPosition().x;
                bool is_left = offset < 0f;
                offset = Mathf.Abs(offset);
                if (is_left ^ facing.GetFacing())
                    offset -= 1f;
                return offset >= 1f && offset < 2f;
            }

            public bool ReactorIsNotStressed(GameObject reactor, Navigator.ActiveTransition _)
            {
                var smi = reactor.GetSMI<StressMonitor.Instance>();
                return smi != null && !smi.IsStressed();
            }

            public void CheeringStart(GameObject reactor)
            {
                if (!this.IsNullOrStopped())
                    GoTo(sm.ready.actingOut);
            }

            public void CheeringCheck(GameObject reactor)
            {
                if (!this.IsNullOrStopped() && (StressedReactable == null || !StressedReactable.IsReacting))
                    CheeringReactable?.NextStep(null);
            }

            public void CheeringEnd(GameObject reactor)
            {
                if (!this.IsNullOrStopped() && StressedReactable != null && StressedReactable.IsReacting)
                {
                    effects.Add(StressedCheering, true);
                    // подавляем реакцию "опасения" на стрессующего
                    var rmi = reactor.GetSMI<ReactionMonitor.Instance>();
                    if (!rmi.IsNullOrStopped())
                        rmi.lastReactTimes["StressConcern"] = GameClock.Instance.GetTime();
                }
            }

            public void StressingStart(GameObject reactor)
            {
                if (!this.IsNullOrStopped() && CheeringReactable != null && CheeringReactable.IsReacting)
                    kbac.SetSceneLayer(Grid.SceneLayer.Front);
            }

            public void StressingCheck(GameObject reactor)
            {
                if (!this.IsNullOrStopped() && (CheeringReactable == null || !CheeringReactable.IsReacting))
                    StressedReactable?.NextStep(null);
            }

            public void StressingEnd(GameObject reactor)
            {
                if (!this.IsNullOrStopped())
                    GoTo(sm.ready.idle);
            }
        }

        public class ReadyStates : State
        {
            public State idle;
            public State actingOut;
        }

        public State satisfied;
        public ReadyStates ready;

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = satisfied;
            satisfied
                .EnterTransition(ready, smi => !smi.HasCheeringEffect())
                .EventTransition(GameHashes.EffectRemoved, ready, smi => !smi.HasCheeringEffect())
                .DoNothing();

            ready
                .DefaultState(ready.idle)
                .ToggleReactable(CreatePasserbyReactable)
                .Exit(smi => smi.CheeringReactable = null);

            ready.idle
                .EnterTransition(satisfied, smi => smi.HasCheeringEffect())
                .EventTransition(GameHashes.EffectAdded, satisfied, smi => smi.HasCheeringEffect())
                .DoNothing();

            ready.actingOut
                .ToggleReactable(CreateSelfReactable)
                .ScheduleGoTo(anim_timeout, ready.idle)
                .Exit(smi => smi.StressedReactable = null);
        }

        private static Reactable CreatePasserbyReactable(Instance smi)
        {
            smi.CheeringReactable =
                new EmoteReactable(
                    gameObject: smi.gameObject,
                    id: "Stressed_Cheering",
                    chore_type: Db.Get().ChoreTypes.Emote,
                    range_width: 7,
                    range_height: 1,
                    globalCooldown: 30f,
                    localCooldown: StressedCheering.duration,
                    max_initial_delay: 1.5f * StressedCheering.duration)
                .SetEmote(MoreMinionEmotes.Instance.Cheering)
                .RegisterEmoteStepCallbacks("working_pre", smi.CheeringStart, null)
                .RegisterEmoteStepCallbacks("working_loop", smi.CheeringCheck, null)
                .RegisterEmoteStepCallbacks("working_pst", smi.CheeringEnd, null);
            smi.CheeringReactable
                .AddPrecondition(ReactorIsOnFloor)
                .AddPrecondition(smi.IsOnFloor)
                .AddPrecondition(smi.ReactorIsCloseEnough)
                .AddPrecondition(smi.ReactorIsNotStressed)
                .preventChoreInterruption = true;
            return smi.CheeringReactable;
        }

        private static Reactable CreateSelfReactable(Instance smi)
        {
            if (smi.CheeringReactable != null && smi.CheeringReactable.IsReacting)
            {
                smi.StressedReactable =
                    new EmoteReactable(
                        gameObject: smi.CheeringReactable.reactor,
                        id: "Stressed_Stressing",
                        chore_type: Db.Get().ChoreTypes.Emote,
                        range_width: 7,
                        range_height: 1,
                        globalCooldown: 5f,
                        localCooldown: 5f)
                    .SetEmote(MoreMinionEmotes.Instance.Stressed)
                    .RegisterEmoteStepCallbacks("working_pre", smi.StressingStart, null)
                    .RegisterEmoteStepCallbacks("working_loop", smi.StressingCheck, null)
                    .RegisterEmoteStepCallbacks("working_pst", null, smi.StressingEnd);
                smi.StressedReactable
                    .AddPrecondition(ReactorIsOnFloor)
                    .AddPrecondition(smi.ReactorIsMe)
                    .preventChoreInterruption = true;
            }
            else
                smi.StressedReactable = null;
            return smi.StressedReactable;
        }
    }
}
