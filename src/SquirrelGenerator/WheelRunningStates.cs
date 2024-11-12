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
            public int targetWheel_cell = Grid.InvalidCell;
            private AmountInstance calories;
            private AttributeInstance metabolism;
            private float metabolism_bonus;
            public WheelRunningMonitor monitor;
            public KBatchedAnimController kbac;
            public Navigator navigator;
            public float originalSpeed;
            public SquirrelGenerator TargetWheel
            {
                get
                {
                    var go = sm.target.Get(this);
                    if (go != null && go.TryGetComponent<SquirrelGenerator>(out var generator))
                        return generator;
                    return null;
                }
            }
            public float Calories { get => calories.value / calories.GetMax(); }
            public float Productiveness { get => Calories * (metabolism.GetTotalValue() / metabolism_bonus); }
            public Instance(Chore<Instance> chore, Def def) : base(chore, def)
            {
                chore.AddPrecondition(ChorePreconditions.instance.CheckBehaviourPrecondition, WantsToWheelRunning);
                calories = Db.Get().Amounts.Calories.Lookup(gameObject);
                metabolism = Db.Get().CritterAttributes.Metabolism.Lookup(gameObject);
                metabolism_bonus = METABOLISM_BONUS + SquirrelGeneratorOptions.Instance.MetabolismBonus;
                gameObject.TryGetComponent(out monitor);
                gameObject.TryGetComponent(out kbac);
                gameObject.TryGetComponent(out navigator);
                originalSpeed = navigator.defaultSpeed;
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
            public State pst_interrupt;
        }

#pragma warning disable CS0649
        private MovingStates moving;
        private RunningStates running;
        private TargetParameter target;
#pragma warning restore CS0649

        public static Effect RunInWheelEffect;

        public override void InitializeStates(out BaseState default_state)
        {
            if (RunInWheelEffect == null)
            {
                RunInWheelEffect = new Effect(
                    id: "RunInWheel",
                    name: STRINGS.CREATURES.MODIFIERS.RUN_IN_WHEEL.NAME,
                    description: STRINGS.CREATURES.MODIFIERS.RUN_IN_WHEEL.TOOLTIP,
                    duration: 0,
                    show_in_ui: true,
                    trigger_floating_text: false,
                    is_bad: false);
                RunInWheelEffect.Add(new AttributeModifier(
                    attribute_id: Db.Get().CritterAttributes.Metabolism.Id,
                    value: SquirrelGeneratorOptions.Instance.MetabolismBonus,
                    description: STRINGS.CREATURES.MODIFIERS.RUN_IN_WHEEL.NAME,
                    is_multiplier: false,
                    uiOnly: false,
                    is_readonly: true));
                RunInWheelEffect.Add(new AttributeModifier(
                    attribute_id: Db.Get().CritterAttributes.Happiness.Id,
                    value: SquirrelGeneratorOptions.Instance.HappinessBonus,
                    description: STRINGS.CREATURES.MODIFIERS.RUN_IN_WHEEL.NAME,
                    is_multiplier: false,
                    uiOnly: false,
                    is_readonly: true));
            }
            default_state = moving;

            root
                .Enter(smi =>
                {
                    if (!ReserveWheel(smi))
                        smi.GoTo((BaseState)null);
                })
                .Exit(UnreserveWheel)
                .BehaviourComplete(WantsToWheelRunning, true);

            moving
                .DefaultState(moving.cheer_pre)
                .OnTargetLost(target, null)
                .EventTransition(GameHashes.OperationalChanged, smi => smi.TargetWheel, null, smi => !smi.TargetWheel.IsOperational)
                .ToggleStatusItem(
                    name: STRINGS.CREATURES.STATUSITEMS.EXCITED_TO_RUN_IN_WHEEL.NAME,
                    tooltip: STRINGS.CREATURES.STATUSITEMS.EXCITED_TO_RUN_IN_WHEEL.TOOLTIP,
                    category: Db.Get().StatusItemCategories.Main);

            moving.cheer_pre
                .ScheduleGoTo(0.9f, moving.cheer);

            moving.cheer
                .Enter(smi => smi.GetComponent<Facing>().Face(Grid.CellToPos(smi.targetWheel_cell)))
                .PlayAnim("excited_loop")
                .OnAnimQueueComplete(moving.cheer_pst);

            moving.cheer_pst
                .ScheduleGoTo(0.2f, moving.moving);

            moving.moving
                .Enter("Speedup", smi => smi.navigator.defaultSpeed = smi.originalSpeed * TUNING.DUPLICANTSTATS.MOVEMENT_MODIFIERS.BONUS_2)
                .MoveTo(smi => smi.targetWheel_cell, running, null, false)
                .Exit("RestoreSpeed", smi => smi.navigator.defaultSpeed = smi.originalSpeed);

            running
                .DefaultState(running.pre)
                .OnTargetLost(target, running.pst)
                .EventTransition(GameHashes.OperationalChanged, smi => smi.TargetWheel, running.pst, smi => !smi.TargetWheel.IsOperational)
                // чтобы отыграть завершающую анимацию когда чору хочет прервать другая чора
                .ToggleTag(GameTags.PerformingWorkRequest)
                .EventTransition(GameHashes.ChoreInterrupt, running.pst_interrupt)
                .ToggleEffect(smi => RunInWheelEffect)
                .Enter(smi => smi.TargetWheel?.SetProductiveness(smi.Productiveness))
                .Exit(smi => smi.TargetWheel?.SetProductiveness(0));

            running.pre
                .PlayAnim("rummage_pre")
                .OnAnimQueueComplete(running.loop);

            running.loop
                .Enter(StartRunning)
                .QueueAnim("floor_floor_1_0_loop", true, null)
                .Transition(running.pst, Update, UpdateRate.SIM_4000ms)
                .Exit(StopRunning);

            // если бег завершается штатным образом - роллим время следующего бега
            running.pst
                .PlayAnim("rummage_pst")
                .QueueAnim("queue_loop")
                .OnAnimQueueComplete(null)
                .Exit(smi => smi.monitor.smi?.RefreshSearchTime());

            // если мы препятствовали прерыванию другой чорой
            // в конце надо обновить моск
            running.pst_interrupt
                .ToggleTag(GameTags.PreventChoreInterruption)
                .PlayAnim("rummage_pst")
                .OnAnimQueueComplete(null)
                .Exit(PrioritizeUpdateBrain);
        }

        private static bool ReserveWheel(Instance smi)
        {
            var go = smi.monitor.smi?.TargetWheel;
            if (go != null && !go.HasTag(GameTags.Creatures.ReservedByCreature))
            {
                if (go.TryGetComponent<SquirrelGenerator>(out var squirrelGenerator) && squirrelGenerator.IsOperational)
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
            var go = smi.sm.target.Get(smi);
            if (go != null)
            {
                go.RemoveTag(GameTags.Creatures.ReservedByCreature);
                if (go.TryGetComponent<SquirrelGenerator>(out var squirrelGenerator))
                    squirrelGenerator.SetProductiveness(0);
            }
            smi.sm.target.Set(null, smi);
        }

        private static bool Update(Instance smi)
        {
            smi.TargetWheel?.SetProductiveness(smi.Productiveness);
            return smi.Productiveness <= 0f || !smi.monitor.IsHappy();
        }

        private static void StartRunning(Instance smi)
        {
            smi.Get<Facing>().SetFacing(false);
            smi.kbac.Offset += smi.def.workAnimOffset;
            smi.kbac.PlaySpeedMultiplier = smi.def.workAnimSpeedMultiplier;
            smi.kbac.SetSceneLayer(Grid.SceneLayer.BuildingUse);
        }

        private static void StopRunning(Instance smi)
        {
            smi.kbac.Offset -= smi.def.workAnimOffset;
            smi.kbac.PlaySpeedMultiplier = 1f;
            smi.kbac.SetSceneLayer(Grid.SceneLayer.Creatures);
        }

        internal static void PrioritizeUpdateBrain(StateMachine.Instance smi)
        {
            PrioritizeUpdateBrain(smi.gameObject);
        }

        internal static void PrioritizeUpdateBrain(GameObject go)
        {
            if (go != null && go.TryGetComponent<CreatureBrain>(out var brain))
                Game.BrainScheduler.PrioritizeBrain(brain);
        }
    }
}
