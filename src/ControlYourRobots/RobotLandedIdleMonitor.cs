namespace ControlYourRobots
{
    public class RobotLandedIdleMonitor : GameStateMachine<RobotLandedIdleMonitor, RobotLandedIdleMonitor.Instance, IStateMachineTarget, RobotLandedIdleMonitor.Def>
    {
        public class Def : BaseDef
        {
            public float timeout;
        }

        public new class Instance : GameInstance
        {
            public Instance(IStateMachineTarget master, Def def) : base(master, def) { }
        }

        public State satisfied;
        public State idle;
        public State landed;

        public override void InitializeStates(out BaseState default_state)
        {
            serializable = SerializeType.Never;
            default_state = satisfied;
            satisfied
                .TagTransition(GameTags.Idle, idle, false);
            idle
                .TagTransition(GameTags.Idle, satisfied, true)
                .EventHandlerTransition(GameHashes.DestinationReached, landed, CanLanded);
            landed
                .ToggleBehaviour(Patches.RobotLandedIdleBehaviour, smi => true, smi => smi.GoTo(satisfied));
        }

        private static bool CanLanded(Instance smi, object _)
        {
            if (smi.timeinstate < smi.def.timeout)
                return false;
            int cell = Grid.PosToCell(smi);
            int world = smi.GetMyWorldId();
            while (true)
            {
                if (!Grid.IsValidCellInWorld(cell, world) || Grid.HasDoor[cell])
                    return false;
                if (Grid.IsSolidCell(cell))
                    return true;
                if (Grid.IsLiquid(cell))
                    return false;
                cell = Grid.CellBelow(cell);
            }
        }
    }
}
