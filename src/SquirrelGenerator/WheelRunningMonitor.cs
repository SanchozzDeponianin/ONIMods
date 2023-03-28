using System.Runtime.Serialization;
using UnityEngine;
using Klei.AI;
using KSerialization;

namespace SquirrelGenerator
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class WheelRunningMonitor : StateMachineComponent<WheelRunningMonitor.StatesInstance>, ISaveLoadable
    {
        public const int SEARCH_WHEEL_RADIUS = 25;
        public const int SEARCH_MIN_INTERVAL = 15;
        public const int SEARCH_MAX_INTERVAL = 30;
        private const int MAX_NAVIGATE_DISTANCE = 200;

        public class StatesInstance : GameStateMachine<States, StatesInstance, WheelRunningMonitor>.GameInstance
        {
            private float nextSearchTime;
            public GameObject TargetWheel { get; private set; }

            public StatesInstance(WheelRunningMonitor master) : base(master)
            {
                if (master.shouldResumeRun)
                    nextSearchTime = Time.time + 6 * UpdateManager.SecondsPerSimTick;
                else
                    RefreshSearchTime();
            }

            public void RefreshSearchTime()
            {
                nextSearchTime = Time.time + Mathf.Lerp(SquirrelGeneratorOptions.Instance.SearchMinInterval, SquirrelGeneratorOptions.Instance.SearchMaxInterval, Random.value);
            }

            public void SetSearchTimeImmediately()
            {
                nextSearchTime = Time.time;
            }

            public bool ShouldRunInWheel()
            {
                if (TargetWheel == null)
                {
                    if (Time.time < nextSearchTime)
                        return false;
                    RefreshSearchTime();
                    if (!HasTag(GameTags.Creatures.Hungry) && master.effects.HasEffect("Happy"))
                        FindWheel();
                }
                return TargetWheel != null;
            }

            private void FindWheel()
            {
                TargetWheel = null;
                var pooledList = ListPool<ScenePartitionerEntry, GameScenePartitioner>.Allocate();
                var extents = new Extents(Grid.PosToCell(master.transform.GetPosition()), SquirrelGeneratorOptions.Instance.SearchWheelRadius);
                GameScenePartitioner.Instance.GatherEntries(extents, GameScenePartitioner.Instance.completeBuildings, pooledList);
                int mincost = MAX_NAVIGATE_DISTANCE;
                foreach (ScenePartitionerEntry item in pooledList)
                {
                    if ((item.obj as KMonoBehaviour).TryGetComponent<SquirrelGenerator>(out var squirrelGenerator)
                        && squirrelGenerator.IsOperational && !squirrelGenerator.HasTag(GameTags.Creatures.ReservedByCreature))
                    {
                        int cost = master.navigator.GetNavigationCost(squirrelGenerator.RunningCell);
                        if (cost != -1 && cost < mincost)
                        {
                            mincost = cost;
                            TargetWheel = squirrelGenerator.gameObject;
                        }
                    }
                }
                pooledList.Recycle();
            }

            public void OnRunningComplete()
            {
                TargetWheel = null;
                RefreshSearchTime();
            }
        }

        public class States : GameStateMachine<States, StatesInstance, WheelRunningMonitor>
        {
            public override void InitializeStates(out BaseState default_state)
            {
                default_state = root;
                root.ToggleBehaviour(WheelRunningStates.WantsToWheelRunning, smi => smi.ShouldRunInWheel(), smi => smi.OnRunningComplete());
            }
        }

#pragma warning disable CS0649
        [MyCmpGet]
        Effects effects;

        [MyCmpGet]
        Navigator navigator;
#pragma warning restore CS0649

        [Serialize]
        private bool shouldResumeRun;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            GetComponent<KBatchedAnimController>().SetSceneLayer(Grid.SceneLayer.Creatures);
            smi.StartSM();
            if (shouldResumeRun && TryGetComponent<CreatureBrain>(out var brain))
                GameScheduler.Instance.Schedule(null, Random.Range(6f, 20f) * UpdateManager.SecondsPerSimTick, WheelRunningStates.ForceUpdateBrain, brain);
        }

        [OnSerializing]
        private void OnSerializing()
        {
            shouldResumeRun = this.HasTag(GameTags.PerformingWorkRequest);
        }
    }
}
