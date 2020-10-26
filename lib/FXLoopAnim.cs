using UnityEngine;

namespace SanchozzONIMods.Lib
{
    // копия FXAnim переработанная для циклического отображения нескольких анимаций
    public class FXLoopAnim : GameStateMachine<FXLoopAnim, FXLoopAnim.Instance>
    {
        public new class Instance : GameInstance
        {
            private HashedString[] anims;
            private KAnim.PlayMode mode;
            private KBatchedAnimController animController;

            public Instance(IStateMachineTarget master, string kanim_file, HashedString[] anims, KAnim.PlayMode mode, Vector3 offset, Color32 tint_colour)
                : base(master)
            {
                animController = FXHelpers.CreateEffect(kanim_file, smi.master.transform.GetPosition() + offset, smi.master.transform);
                //animController.gameObject.Subscribe(-1061186183, OnAnimQueueComplete);
                animController.TintColour = tint_colour;
                sm.fx.Set(animController.gameObject, smi);
                this.anims = anims;
                this.mode = mode;
            }

            public void Play()
            {
                //animController.Play(anim, mode, 1f, 0f);
                animController.Play(anims, mode);
            }

            public void DestroyFX()
            {
                Util.KDestroyGameObject(sm.fx.Get(smi));
            }
        }

        public TargetParameter fx;
        public State loop;

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = loop;
            Target(fx);
            loop
                .Enter((Instance smi) => smi.Play())
                .Update((Instance smi, float dt) => smi.Play(), UpdateRate.SIM_4000ms)
                //.EventTransition(GameHashes.AnimQueueComplete, loop, null)
                .Exit((Instance smi) => smi.DestroyFX());
        }
    }
}
