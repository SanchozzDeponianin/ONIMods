using UnityEngine;
using KSerialization;

namespace SquirrelGenerator
{
    public class SquirrelGenerator : Generator
    {
        public class GeneratePowerSM : GameStateMachine<GeneratePowerSM, GeneratePowerSM.Instance, SquirrelGenerator>
        {
            public class WorkingStates : State
            {
                public State pre;
                public State loop;
                public State pst;
            }

            public new class Instance : GameInstance
            {
                public Instance(SquirrelGenerator master) : base(master)
                {
                }
            }

            public State off;
            public State on;
            public WorkingStates working;

            public override void InitializeStates(out BaseState default_state)
            {
                default_state = off;
                serializable = true;
                off.EventTransition(GameHashes.OperationalChanged, on, (Instance smi) => smi.master.operational.IsOperational)
                    .PlayAnim("off");
                on.EventTransition(GameHashes.OperationalChanged, off, (Instance smi) => !smi.master.operational.IsOperational)
                    .EventTransition(GameHashes.ActiveChanged, working.pre, (Instance smi) => smi.master.operational.IsActive)
                    .PlayAnim("on");
                working.DefaultState(working.pre);
                working.pre.PlayAnim("working_pre")
                    .OnAnimQueueComplete(working.loop);
                working.loop.PlayAnim("working_loop", KAnim.PlayMode.Loop)
                    .EventTransition(GameHashes.ActiveChanged, working.pst, (Instance smi) => masterTarget.Get(smi) != null &&
                    !smi.master.operational.IsActive);
                working.pst.PlayAnim("working_pst")
                    .OnAnimQueueComplete(off);
            }
        }

        [Serialize]
        [SerializeField]
        private float productiveness = 0;

        private GeneratePowerSM.Instance smi;
        private MeterController meter;

        private static StatusItem activeWattageStatusItem;

        //public bool IsPowered => operational.IsActive;
        public bool IsOperational => operational.IsOperational;

        public new float WattageRating => base.WattageRating * productiveness;

        public int RunningCell { get => Grid.CellRight(Grid.PosToCell(this)); }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            activeWattageStatusItem = new StatusItem("WATTAGE", "BUILDING", "", StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.Power.ID)
            {
                resolveStringCallback = delegate (string str, object data)
                {
                    SquirrelGenerator generator = (SquirrelGenerator)data;
                    str = str.Replace("{Wattage}", GameUtil.GetFormattedWattage(generator.WattageRating, GameUtil.WattageFormatterUnit.Automatic, true));
                    return str;
                }
            };
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            CreateMeter();
            smi = new GeneratePowerSM.Instance(this);
            smi.StartSM();
        }

        protected override void OnCleanUp()
        {
            smi.StopSM("cleanup");
            base.OnCleanUp();
        }

        private void CreateMeter()
        {
            meter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_target", "meter", Meter.Offset.Infront, Grid.SceneLayer.NoLayer, new string[]
                {
                "meter_target",
                "meter_fill",
                "meter_frame",
                "meter_OL"
                });
        }

        public override void EnergySim200ms(float dt)
        {
            base.EnergySim200ms(dt);
            KSelectable component = GetComponent<KSelectable>();
            if (operational.IsActive)
            {
                GenerateJoules(WattageRating * dt, false);
                selectable.SetStatusItem(Db.Get().StatusItemCategories.Power, activeWattageStatusItem, this);
                meter.SetPositionPercent(productiveness);
            }
            else
            {
                ResetJoules();
                selectable.SetStatusItem(Db.Get().StatusItemCategories.Power, Db.Get().BuildingStatusItems.GeneratorOffline, null);
                meter.SetPositionPercent(0f);
            }
        }

        public void SetProductiveness(float value)
        {
            productiveness = IsOperational ? value : 0;
            operational.SetActive(productiveness > 0, false);
        }
    }
}
