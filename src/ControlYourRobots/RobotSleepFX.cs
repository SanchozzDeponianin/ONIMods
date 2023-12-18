using UnityEngine;

namespace ControlYourRobots
{
    public class RobotSleepFX : GameStateMachine<RobotSleepFX, RobotSleepFX.Instance>
    {
        public new class Instance : GameInstance
        {
            private KBatchedAnimController fx;
            public Instance(IStateMachineTarget master) : base(master) { }

            public void CreateFx()
            {
                fx = FXHelpers.CreateEffect("sleep_zzz_fx_kanim", gameObject.transform.GetPosition() + new Vector3(0f, 0f, -0.1f), gameObject.transform, true, Grid.SceneLayer.FXFront, false);
                fx.Play("working_loop", KAnim.PlayMode.Loop);
            }

            public void DestroyFx()
            {
                fx?.gameObject?.DeleteObject();
                fx = null;
            }
        }

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = root;
            root
                .Enter(smi => smi.CreateFx())
                .Exit(smi => smi.DestroyFx());
        }
    }
}
