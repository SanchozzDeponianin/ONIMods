using STRINGS;
using UnityEngine;

namespace ControlYourRobots
{
    using static Patches;

    public class RobotLandedIdleStates : GameStateMachine<RobotLandedIdleStates, RobotLandedIdleStates.Instance, IStateMachineTarget, RobotLandedIdleStates.Def>
    {
        public class Def : BaseDef { }

        public new class Instance : GameInstance
        {
            public Instance(Chore<Instance> chore, Def def) : base(chore, def)
            {
                chore.masterPriority.priority_class = PriorityScreen.PriorityClass.idle;
                chore.AddPrecondition(ChorePreconditions.instance.CheckBehaviourPrecondition, RobotLandedIdleBehaviour);
            }
        }

        public class Landed : State
        {
            public State fall_pre;
            public State fall;
            public State fall_pst;
            public State idle;
        }

        public Landed landed;
        public State takeoff;
        public State behaviourcomplete;

        public override void InitializeStates(out BaseState default_state)
        {
            root.ToggleStatusItem(
                name: CREATURES.STATUSITEMS.IDLE.NAME,
                tooltip: CREATURES.STATUSITEMS.IDLE.TOOLTIP,
                category: Db.Get().StatusItemCategories.Main);

            default_state = landed;
            landed
                .DefaultState(landed.fall_pre)
                .ToggleTag(GameTags.PerformingWorkRequest)
                .ToggleTag(GameTags.Idle)
                .Exit(smi =>
                {
                    if (GameComps.Fallers.Has(smi.gameObject))
                        GameComps.Fallers.Remove(smi.gameObject);
                });

            landed.fall_pre
                .PlayAnim("power_down_pre")
                .OnAnimQueueComplete(landed.fall);

            landed.fall
                .PlayAnim("power_down_loop", KAnim.PlayMode.Loop)
                .Enter(smi =>
                {
                    if (!GameComps.Fallers.Has(smi.gameObject))
                        GameComps.Fallers.Add(smi.gameObject, Vector2.zero);
                })
                .Update((smi, dt) =>
                {
                    if (!GameComps.Gravities.Has(smi.gameObject))
                        smi.GoTo(landed.fall_pst);
                }, UpdateRate.SIM_200ms, false)
                .EventTransition(GameHashes.Landed, landed.fall_pst, null);

            landed.fall_pst
                .PlayAnim("power_down_pst")
                .Enter(smi => smi.GetComponent<LoopingSounds>().PauseSound(GlobalAssets.GetSound("Flydo_flying_LP", false), true))
                .OnAnimQueueComplete(landed.idle);

            landed.idle
                .PlayAnim("dead_battery")
                .ToggleAttributeModifier("low power mode",
                        smi => LandedIdleBatteryModifiers[smi.PrefabID()],
                        smi => LandedIdleBatteryModifiers.ContainsKey(smi.PrefabID()))
                .EventTransition(GameHashes.ChoreInterrupt, takeoff);

            takeoff
                .ToggleTag(GameTags.PreventChoreInterruption)
                .PlayAnim("power_up")
                .OnAnimQueueComplete(behaviourcomplete)
                .Exit(smi => smi.GetComponent<LoopingSounds>().PauseSound(GlobalAssets.GetSound("Flydo_flying_LP", false), false));

            behaviourcomplete.BehaviourComplete(RobotLandedIdleBehaviour);
        }
    }
}
