using Klei.AI;
using UnityEngine;

namespace MoreEmotions
{
    public class ContusionFX : GameStateMachine<ContusionFX, ContusionFX.Instance>
    {
        public new class Instance : GameInstance
        {
            internal Effects effects;
            private KBatchedAnimController fx;

            public Instance(IStateMachineTarget master) : base(master)
            {
                gameObject.TryGetComponent(out effects);
            }

            public void CreateFx()
            {
                fx = FXHelpers.CreateEffect("contused_crew_fx_kanim", gameObject.transform.GetPosition() + new Vector3(0f, 0f, -0.1f),
                    gameObject.transform, true, Grid.SceneLayer.FXFront, false);
                fx.Play("working_loop", KAnim.PlayMode.Loop);
            }

            public void DestroyFx()
            {
                fx?.gameObject?.DeleteObject();
                fx = null;
            }
        }

        public State satisfied;
        public State contused;

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = satisfied;
            root.EnterTransition(contused, HasContusion);

            satisfied
                .EventTransition(GameHashes.EffectAdded, contused, HasContusion)
                .DoNothing();

            contused
                .EventTransition(GameHashes.EffectRemoved, satisfied, Not(HasContusion))
                .ToggleAnims("anim_idle_brainfreeze_kanim", 1f)
                .ToggleAnims("anim_loco_run_brainfreeze_kanim", 1f)
                .ToggleAnims("anim_loco_walk_brainfreeze_kanim", 1f)
                .Enter(smi => smi.CreateFx())
                .Exit(smi => smi.DestroyFx());
        }

        private static bool HasContusion(Instance smi) => smi.effects.HasEffect(MoreEmotionsEffects.Contusion);
    }
}
