using UnityEngine;

namespace RoverRefueling
{
    public class RoverRefuelingStation : StateMachineComponent<RoverRefuelingStation.StatesInstance>
    {
        private static readonly EventSystem.IntraObjectHandler<RoverRefuelingStation> CheckPipeDelegate =
            new EventSystem.IntraObjectHandler<RoverRefuelingStation>((component, data) => component.CheckPipe());

        private static readonly EventSystem.IntraObjectHandler<RoverRefuelingStation> OnStorageChangeDelegate =
            new EventSystem.IntraObjectHandler<RoverRefuelingStation>((component, data) => component.RefreshMeter());

        private static readonly Chore.Precondition IsRover = new Chore.Precondition
        {
            id = nameof(IsRover),
            description = STRINGS.DUPLICANTS.CHORES.PRECONDITIONS.IS_ROVER,
            sortOrder = -2,
            fn = (ref Chore.Precondition.Context context, object data) =>
                context.consumerState.prefabid.PrefabTag == GameTags.Robots.Models.ScoutRover
        };

        private static readonly Chore.Precondition RoverNeedRefueling = new Chore.Precondition
        {
            id = nameof(RoverNeedRefueling),
            description = global::STRINGS.DUPLICANTS.CHORES.PRECONDITIONS.HAS_URGE,
            sortOrder = -1,
            fn = (ref Chore.Precondition.Context context, object data) =>
                context.consumerState.prefabid.HasTag(RoverRefuelingPatches.RoverNeedRefueling)
        };

        public class StatesInstance : GameStateMachine<States, StatesInstance, RoverRefuelingStation, object>.GameInstance
        {
            private float minimum_fuel_mass;
            public StatesInstance(RoverRefuelingStation master) : base(master)
            {
                minimum_fuel_mass = 0.1f * RoverRefuelingOptions.Instance.fuel_mass_per_charge;
            }

            public bool IsReady()
            {
                return master.operational.IsOperational && master.storage.GetMassAvailable(RoverRefuelingStationConfig.fuelTag) >= minimum_fuel_mass;
            }
        }

        // выглядит страшно. чтобы задействовать эту старую неиспользованную анимацию, и при этом выглядело прилично.
        public class States : GameStateMachine<States, StatesInstance, RoverRefuelingStation>
        {
            public class OffStates : State
            {
                public State interrupt;
                public State closing;
                public State idle;
            }
            public class IdleStates : State
            {
                public State opening;
                public State idle;
            }
            public class WorkingStates : State
            {
                public State loop;
                public State pst;
            }
            public class OnStates : State
            {
                public IdleStates waiting;
                public WorkingStates working;
            }

#pragma warning disable CS0649
            OffStates off;
            OnStates on;
#pragma warning restore CS0649

            public override void InitializeStates(out BaseState default_state)
            {
                default_state = root;
                root
                    .EnterTransition(off.idle, smi => !smi.IsReady())
                    .EnterTransition(on.waiting.idle, smi => smi.IsReady());
                off
                    .EventTransition(GameHashes.OperationalChanged, on.waiting.opening, smi => smi.IsReady())
                    .EventTransition(GameHashes.OnStorageChange, on.waiting.opening, smi => smi.IsReady());
                off.interrupt
                    .QueueAnim("closing_charging_loop")
                    .QueueAnim("closed_charging_pst")
                    .OnAnimQueueComplete(off.idle);
                off.closing
                    .QueueAnim("closing_not_charging_loop")
                    .OnAnimQueueComplete(off.idle);
                off.idle
                    .PlayAnim("off");
                on
                    .ToggleRecurringChore(CreateChore);
                on.waiting
                    .EnterTransition(off.closing, smi => !smi.IsReady())
                    .EventTransition(GameHashes.OperationalChanged, off.closing, smi => !smi.master.operational.IsOperational)
                    .WorkableStartTransition(smi => smi.master.workable, on.working.loop);
                on.waiting.opening
                    .QueueAnim("opening_not_charging_loop")
                    .OnAnimQueueComplete(on.waiting.idle);
                on.waiting.idle
                    .PlayAnim("on");
                on.working
                    .DoNothing();
                on.working.loop
                    .QueueAnim("open_charging_pre")
                    .QueueAnim("open_charging_loop", true)
                    .EventTransition(GameHashes.OperationalChanged, off.interrupt, smi => !smi.master.operational.IsOperational)
                    .WorkableStopTransition(smi => smi.master.workable, on.working.pst);
                on.working.pst
                    .QueueAnim("open_charging_pst")
                    .OnAnimQueueComplete(on.waiting.idle);
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
                chore.AddPrecondition(IsRover);
                chore.AddPrecondition(RoverNeedRefueling);
                return chore;
            }
        }

#pragma warning disable CS0649
        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private ManualDeliveryKG manualDelivery;

        [MyCmpReq]
        private Storage storage;

        [MyCmpReq]
        private ConduitConsumer consumer;

        [MyCmpAdd]
        private RoverRefuelingWorkable workable;
#pragma warning restore CS0649

        private MeterController fuel_meter;
        private MeterController progress_meter;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.ConduitConnectionChanged, CheckPipeDelegate);
            Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            smi.StartSM();
            var kbac = GetComponent<KBatchedAnimController>();
            fuel_meter = new MeterController(kbac, "meter_oxygen_target", "meter_oxygen", Meter.Offset.Infront, Grid.SceneLayer.BuildingFront, new string[] { "meter_oxygen_target" });
            progress_meter = new MeterController(kbac, "meter_resources_target", "meter_resources", Meter.Offset.Behind, Grid.SceneLayer.BuildingBack, new string[] { "meter_resources_target" });
            RefreshMeter();
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.ConduitConnectionChanged, CheckPipeDelegate);
            Unsubscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            base.OnCleanUp();
        }

        private void CheckPipe()
        {
            manualDelivery.Pause(consumer.IsConnected, "pipe connected");
        }

        private void RefreshMeter()
        {
            fuel_meter.SetPositionPercent(Mathf.Clamp01(storage.GetMassAvailable(RoverRefuelingStationConfig.fuelTag) / storage.capacityKg));
            if (workable.battery != null)
                progress_meter.SetPositionPercent(Mathf.Clamp01(1f - workable.battery.value / workable.battery.GetMax()));
            else
                progress_meter.SetPositionPercent(0f);
        }
    }
}
