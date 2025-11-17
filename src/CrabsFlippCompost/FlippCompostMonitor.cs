using UnityEngine;
using static STRINGS.DUPLICANTS.CHORES;

namespace CrabsFlippCompost
{
    public class FlippCompostMonitor : GameStateMachine<FlippCompostMonitor, FlippCompostMonitor.Instance, IStateMachineTarget, FlippCompostMonitor.Def>
    {
        public static readonly Tag ID = new(nameof(FlippCompostMonitor));
        public static readonly Tag COMPOST_ID = new(CompostConfig.ID);
        public static readonly Tag BEHAVIOUR_TAG = new("WantsToFlippCompost");

        public class Def : BaseDef
        {
            public int radius = 25;
            public float minCooldown = 20f;
            public float maxCooldown = 60f;
            public CellOffset[] approachOffsets = new[] { new CellOffset(-1, 0), new CellOffset(2, 0) }; // справа и слева от компоста
            private Navigator.Scanner<BuildingComplete> compostSeeker;
            public Navigator.Scanner<BuildingComplete> CompostSeeker
            {
                get
                {
                    if (compostSeeker == null)
                    {
                        compostSeeker = new(radius, GameScenePartitioner.Instance.completeBuildings, IsCompostReady);
                        compostSeeker.SetConstantOffsets(approachOffsets);
                        compostSeeker.SetEarlyOutThreshold(8);
                    }
                    return compostSeeker;
                }
            }

            private static bool IsCompostReady(BuildingComplete building)
            {
                bool result = building != null && building.prefabid.IsPrefabID(COMPOST_ID)
                    && !building.prefabid.HasTag(GameTags.Creatures.ReservedByCreature)
                    && building.TryGetComponent(out Compost compost) && compost.smi.IsInsideState(compost.smi.sm.inert)
                    && building.TryGetComponent(out CompostWorkable workable) && workable.worker == null;
                return result;
            }
        }

        public new class Instance : GameInstance, IApproachableBehaviour, ICreatureMonitor
        {
            private KPrefabID kprefabid;
            public Navigator navigator;
            private Facing facing;
            public GameObject target;
            public Compost compost;
            public CompostWorkable workable;
            public int targetCell;

            public Instance(IStateMachineTarget master, Def def) : base(master, def)
            {
                kprefabid = master.GetComponent<KPrefabID>();
                navigator = master.GetComponent<Navigator>();
                facing = master.GetComponent<Facing>();
            }

            public Tag Id => ID;
            public bool IsValidTarget()
            {
                return !target.IsNullOrDestroyed() && navigator.GetNavigationCost(targetCell, GetApproachOffsets()) != -1 && workable.worker == null
                    && (compost.smi.IsInsideState(compost.smi.sm.inert) || compost.smi.IsInsideState(Patches.Compost_States.waiting));
            }
            public GameObject GetTarget() => target;
            public StatusItem GetApproachStatusItem() => smi.sm.TravelingToCompost;
            public StatusItem GetBehaviourStatusItem() => smi.sm.Composting;
            public CellOffset[] GetApproachOffsets() => def.approachOffsets;
            public void OnArrive()
            {
                kprefabid.AddTag(GameTags.PerformingWorkRequest);
                facing.Face(Grid.CellColumn(targetCell));
                if (!target.IsNullOrDestroyed())
                {
                    bool fa = facing.GetFacing();
                    var fx = FXHelpers.CreateEffect("flipp_compost_fx_kanim", target.transform.position + (fa ? Vector3.right : Vector3.zero),
                        null, false, Grid.SceneLayer.BuildingFront, true);
                    fx.initialAnim = fa ? "loop_r" : "loop_l";
                    fx.destroyOnAnimComplete = true;
                    fx.gameObject.SetActive(true);
                }
            }
            public void OnSuccess()
            {
                if (!compost.IsNullOrDestroyed())
                    compost.smi.GoTo(compost.smi.sm.composting);
                target = null;
                compost = null;
                workable = null;
                kprefabid.RemoveTag(GameTags.PerformingWorkRequest);
            }
            public void OnFailure() => kprefabid.RemoveTag(GameTags.PerformingWorkRequest);
        }

        private StatusItem TravelingToCompost;
        private StatusItem Composting;

        public State lookingForCompost;
        public State satisfied;
        public FloatParameter cooldown;

        public override void InitializeStates(out BaseState default_state)
        {
            const string icon = "status_item_pending_compost";
            TravelingToCompost = new(nameof(TravelingToCompost), FLIPCOMPOST.STATUS.text, FLIPCOMPOST.TOOLTIP.text,
                icon, StatusItem.IconType.Custom, NotificationType.Neutral, false, OverlayModes.None.ID, showWorldIcon: false);
            Composting = new(nameof(Composting), FLIPCOMPOST.NAME.text, FLIPCOMPOST.TOOLTIP.text,
                icon, StatusItem.IconType.Custom, NotificationType.Neutral, false, OverlayModes.None.ID, showWorldIcon: false);

            default_state = satisfied;
            serializable = SerializeType.Never;
            root.Enter(SetRandomCooldown);

            lookingForCompost
                .Enter(SetRandomCooldown)
                .PreBrainUpdate(FindCompostTarget)
                .ToggleBehaviour(BEHAVIOUR_TAG, smi => smi.IsValidTarget(), smi => smi.GoTo(satisfied));

            satisfied
                .ScheduleGoTo(smi => smi.sm.cooldown.Get(smi), lookingForCompost);
        }

        private static void SetMinCooldown(Instance smi)
        {
            smi.sm.cooldown.Set(smi.def.minCooldown, smi);
        }
        private static void SetRandomCooldown(Instance smi)
        {
            smi.sm.cooldown.Set(Random.Range(smi.def.minCooldown, smi.def.maxCooldown), smi);
        }
        private static void FindCompostTarget(Instance smi)
        {
            if (!smi.IsValidTarget())
            {
                var building = smi.def.CompostSeeker.Scan(Grid.PosToXY(smi.transform.GetPosition()), smi.navigator);
                var go = building?.gameObject;
                if (go != smi.target)
                {
                    if (go != null)
                    {
                        smi.target = go;
                        go.TryGetComponent(out smi.compost);
                        go.TryGetComponent(out smi.workable);
                        smi.targetCell = Grid.PosToCell(smi.target);
                    }
                    else
                    {
                        smi.target = null;
                        smi.compost = null;
                        smi.workable = null;
                        smi.targetCell = Grid.InvalidCell;
                    }
                    smi.Trigger((int)GameHashes.ApproachableTargetChanged, null);
                }
                if (smi.target == null)
                {
                    SetMinCooldown(smi);
                    smi.GoTo(smi.sm.satisfied);
                }
            }
        }
    }
}
