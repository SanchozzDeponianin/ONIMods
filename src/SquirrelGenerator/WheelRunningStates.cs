using UnityEngine;
using Klei.AI;

namespace SquirrelGenerator
{
    public class WheelRunningStates : GameStateMachine<WheelRunningStates, WheelRunningStates.Instance, IStateMachineTarget, WheelRunningStates.Def>
    {
        public static readonly Tag WantsToWheelRunning = TagManager.Create("WantsToWheelRunning");

        public const int HAPPINESS_BONUS = 1;
        public const int METABOLISM_BONUS = 100;

        public class Def : BaseDef
        {
            public Vector3 workAnimOffset = new Vector3(-0.5f, 0.4f);
            public float workAnimSpeedMultiplier = 1.8f;
        }

        public new class Instance : GameInstance
        {
            public float originalSpeed;
            public SquirrelGenerator targetWheel { get => sm.target.Get(this)?.GetComponent<SquirrelGenerator>(); }
            public int targetWheel_cell = Grid.InvalidCell;
            private AmountInstance calories;
            private AttributeInstance metabolism;
            private float metabolism_bonus;
            public Effects effects;
            public KBatchedAnimController kbac;

            public float Calories { get => calories.value / calories.GetMax(); }
            public float Productiveness { get => Calories * (metabolism.GetTotalValue() / metabolism_bonus); }

            public Instance(Chore<Instance> chore, Def def) : base(chore, def)
            {
                originalSpeed = GetComponent<Navigator>().defaultSpeed;
                chore.AddPrecondition(ChorePreconditions.instance.CheckBehaviourPrecondition, WantsToWheelRunning);
                calories = Db.Get().Amounts.Calories.Lookup(gameObject);
                metabolism = Db.Get().CritterAttributes.Metabolism.Lookup(gameObject);
                metabolism_bonus = METABOLISM_BONUS + SquirrelGeneratorOptions.Instance.MetabolismBonus;
                effects = GetComponent<Effects>();
                kbac = GetComponent<KBatchedAnimController>();
            }
        }

        public class MovingStates : State
        {
            public State cheer_pre;
            public State cheer;
            public State cheer_pst;
            public State moving;
        }

        public class RunningStates : State
        {
            public State pre;
            public State loop;
            public State pst;
        }

#pragma warning disable CS0649
        private MovingStates moving;
        private RunningStates running;
        private TargetParameter target;
#pragma warning restore CS0649

        public static Effect RunInWheelEffect;

        public override void InitializeStates(out BaseState default_state)
        {
            RunInWheelEffect = new Effect("RunInWheel", STRINGS.CREATURES.MODIFIERS.RUN_IN_WHEEL.NAME, STRINGS.CREATURES.MODIFIERS.RUN_IN_WHEEL.TOOLTIP, 0, true, false, false);
            RunInWheelEffect.Add(new AttributeModifier(Db.Get().CritterAttributes.Metabolism.Id, SquirrelGeneratorOptions.Instance.MetabolismBonus, STRINGS.CREATURES.MODIFIERS.RUN_IN_WHEEL.NAME, false, false, true));
            RunInWheelEffect.Add(new AttributeModifier(Db.Get().CritterAttributes.Happiness.Id, SquirrelGeneratorOptions.Instance.HappinessBonus, STRINGS.CREATURES.MODIFIERS.RUN_IN_WHEEL.NAME, false, false, true));

            serializable = true;
            default_state = moving;

            root.Enter(delegate (Instance smi)
                {
                    if (!ReserveWheel(smi))
                    {
                        smi.GoTo((BaseState)null);
                    }
                })
                .Exit(UnreserveWheel)
                .BehaviourComplete(WantsToWheelRunning, true);

            moving
                .DefaultState(moving.cheer_pre)
                .OnTargetLost(target, null)
                .EventTransition(GameHashes.OperationalChanged, (Instance smi) => smi.targetWheel, null, (Instance smi) => !smi.targetWheel.IsOperational)
                .ToggleStatusItem(
                    name: STRINGS.CREATURES.STATUSITEMS.EXCITED_TO_RUN_IN_WHEEL.NAME,
                    tooltip: STRINGS.CREATURES.STATUSITEMS.EXCITED_TO_RUN_IN_WHEEL.TOOLTIP,
                    category: Db.Get().StatusItemCategories.Main);

            moving.cheer_pre
                .ScheduleGoTo(0.9f, moving.cheer);

            moving.cheer
                .Enter((Instance smi) => smi.GetComponent<Facing>().Face(Grid.CellToPos(smi.targetWheel_cell)))
                .PlayAnim("excited_loop")
                .OnAnimQueueComplete(moving.cheer_pst);

            moving.cheer_pst
                .ScheduleGoTo(0.2f, moving.moving);

            moving.moving
                .Enter("Speedup", (Instance smi) => smi.GetComponent<Navigator>().defaultSpeed = smi.originalSpeed * TUNING.DUPLICANTSTATS.MOVEMENT.BONUS_2)
                .MoveTo((Instance smi) => smi.targetWheel_cell, running, null, false)
                .Exit("RestoreSpeed", delegate (Instance smi)
                {
                    smi.GetComponent<Navigator>().defaultSpeed = smi.originalSpeed;
                });

            running
                .DefaultState(running.pre)
                .OnTargetLost(target, running.pst)
                .EventTransition(GameHashes.OperationalChanged, (Instance smi) => smi.targetWheel, running.pst, (Instance smi) => !smi.targetWheel.IsOperational)
                .ToggleEffect((Instance smi) => RunInWheelEffect)
                .Enter((Instance smi) => smi?.targetWheel?.SetProductiveness(smi.Productiveness))
                .Exit((Instance smi) => smi?.targetWheel?.SetProductiveness(0));

            running.pre
                .PlayAnim("rummage_pre")
                .OnAnimQueueComplete(running.loop);

            running.loop
                .Enter("apply offset anim", (Instance smi) =>
                    {
                        smi.Get<Facing>().SetFacing(false);
                        smi.kbac.Offset += smi.def.workAnimOffset;
                        smi.kbac.PlaySpeedMultiplier = smi.def.workAnimSpeedMultiplier;
                    })
                .QueueAnim("floor_floor_1_0_loop", true, null)
                .Transition(running.pst, Update, UpdateRate.SIM_1000ms)
                .Exit("restore offset anim", (Instance smi) =>
                    {
                        smi.kbac.Offset -= smi.def.workAnimOffset;
                        smi.kbac.PlaySpeedMultiplier = 1f;
                    });

            running.pst
                .QueueAnim("rummage_pst", false, null)
                .OnAnimQueueComplete(null);
        }

        private static bool ReserveWheel(Instance smi)
        {
            GameObject go = smi.GetSMI<WheelRunningMonitor.Instance>()?.targetWheel;
            if (go != null && !go.HasTag(GameTags.Creatures.ReservedByCreature))
            {
                var squirrelGenerator = go.GetComponent<SquirrelGenerator>();
                if (squirrelGenerator != null && squirrelGenerator.IsOperational)
                {
                    smi.sm.target.Set(go, smi);
                    go.AddTag(GameTags.Creatures.ReservedByCreature);
                    smi.targetWheel_cell = squirrelGenerator.RunningCell;
                    return true;
                }
            }
            return false;
        }

        private static void UnreserveWheel(Instance smi)
        {
            GameObject go = smi.sm.target.Get(smi);
            if (go != null)
            {
                go.RemoveTag(GameTags.Creatures.ReservedByCreature);
                go.GetComponent<SquirrelGenerator>()?.SetProductiveness(0);
            }
            smi.sm.target.Set(null, smi);
        }

        private static bool Update(Instance smi)
        {
            smi?.targetWheel?.SetProductiveness(smi.Productiveness);
            return smi.Productiveness <= 0f || smi.effects.HasEffect("Unhappy");
        }
    }
}
