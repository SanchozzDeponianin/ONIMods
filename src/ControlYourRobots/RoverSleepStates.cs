namespace ControlYourRobots
{
    using static Patches;

    // в некотором роде мимикрируем под RobotElectroBankDeadStates, упрощённо
    public class RoverSleepStates : GameStateMachine<RoverSleepStates, RoverSleepStates.Instance, IStateMachineTarget, RoverSleepStates.Def>
    {
        public class Def : BaseDef { }

        public new class Instance : GameInstance
        {
            public Instance(Chore<Instance> chore, Def def) : base(chore, def)
            {
                chore.choreType.interruptPriority = Db.Get().ChoreTypes.Die.interruptPriority;
                chore.masterPriority.priority_class = PriorityScreen.PriorityClass.compulsory;
                chore.AddPrecondition(ChorePreconditions.instance.CheckBehaviourPrecondition, RobotSuspendBehaviour);
            }
        }

        public class PowerDown : State
        {
            public State grounded;
            public State carried;
        }

        public PowerDown powerdown;
        public State behaviourcomplete;

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = powerdown;

            powerdown
                .DefaultState(powerdown.grounded)
                .ToggleTag(GameTags.Creatures.Deliverable)
                .TagTransition(RobotSuspend, behaviourcomplete, true)
                .ToggleStateMachine(smi => new FallWhenDeadMonitor.Instance(smi.master))
                .PlayAnim("in_storage");

            powerdown.grounded
                .TagTransition(GameTags.Stored, powerdown.carried, false)
                .Enter(smi =>
                {
                    // принудительно "роняем" робота чтобы он не зависал в воздухе после перемещения
                    var fall_smi = smi.GetSMI<FallWhenDeadMonitor.Instance>();
                    if (!fall_smi.IsNullOrStopped())
                        fall_smi.GoTo(fall_smi.sm.falling);
                });

            powerdown.carried
                .TagTransition(GameTags.Stored, powerdown.grounded, true);

            behaviourcomplete
                .BehaviourComplete(RobotSuspendBehaviour, false);
        }
    }
}