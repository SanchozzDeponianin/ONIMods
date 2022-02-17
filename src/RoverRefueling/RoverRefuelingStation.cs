namespace RoverRefueling
{
    public class RoverRefuelingStation : StateMachineComponent<RoverRefuelingStation.StatesInstance>
    {
        public class StatesInstance : GameStateMachine<States, StatesInstance, RoverRefuelingStation, object>.GameInstance
        {
            public StatesInstance(RoverRefuelingStation master) : base(master) { }
        }

        public class States : GameStateMachine<States, StatesInstance, RoverRefuelingStation>
        {
#pragma warning disable CS0649
            State operational;
            State notoperational;
#pragma warning restore CS0649

            // todo: труба сбрасывает оператиональ. зделать общий компонент с зарядником
            // todo: проверка наличия топлива для заправки
            // todo: пошаманить с анимацией, заменить маску на горловину топливного бака
            // todo: оформить статысы, чтобы была норм анимация

            public override void InitializeStates(out BaseState default_state)
            {
                default_state = operational;
                notoperational
                    .EventTransition(GameHashes.OperationalChanged, operational, smi => smi.master.operational.IsOperational);
                operational
                    .EventTransition(GameHashes.OperationalChanged, notoperational, smi => !smi.master.operational.IsOperational)
                    .ToggleRecurringChore(CreateChore);
            }

            private Chore CreateChore(StatesInstance smi)
            {
                var chore = new WorkChore<RoverRefuelingWorkable>(
                        chore_type: Db.Get().ChoreTypes.Recharge,
                        target: smi.master.workable,
                        ignore_schedule_block: true,
                        only_when_operational: false,
                        allow_prioritization: false,
                        priority_class: PriorityScreen.PriorityClass.personalNeeds,
                        priority_class_value: Chore.DEFAULT_BASIC_PRIORITY,
                        add_to_daily_report: false);
                chore.AddPrecondition(ChorePreconditions.instance.HasTag, ScoutRoverConfig.ID.ToTag());
                return chore;
            }
        }

#pragma warning disable CS0649
        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private Storage storage;

        [MyCmpAdd]
        private RoverRefuelingWorkable workable;
#pragma warning restore CS0649

        protected override void OnSpawn()
        {
            base.OnSpawn();
            smi.StartSM();
        }
    }
}
