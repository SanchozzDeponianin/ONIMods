using UnityEngine;
using Klei.AI;

namespace SquirrelGenerator
{
    public class WheelRunningMonitor : GameStateMachine<WheelRunningMonitor, WheelRunningMonitor.Instance, IStateMachineTarget, WheelRunningMonitor.Def>
    {
        public const int SEARCHWHEELRADIUS = 15;
        public const int SEARCHMININTERVAL = 15;
        public const int SEARCHMAXINTERVAL = 60;

        public class Def : BaseDef
        {
            public float searchMinInterval = SEARCHMININTERVAL;
            public float searchMaxInterval = SEARCHMAXINTERVAL;
        }

        public new class Instance : GameInstance
        {
            public float nextSearchTime;
            public GameObject targetWheel;

            public Instance(IStateMachineTarget master, Def def) : base(master, def)
            {
                RefreshSearchTime();
            }

            public void RefreshSearchTime()
            {
                nextSearchTime = Time.time + Mathf.Lerp(def.searchMinInterval, def.searchMaxInterval, Random.value);
            }

            public bool ShouldRunInWheel()
            {
                if (targetWheel == null)
                {
                    if (Time.time < nextSearchTime)
                    {
                        return false;
                    }
                    RefreshSearchTime();
                    if (!HasTag(GameTags.Creatures.Hungry) && gameObject.GetComponent<Effects>().HasEffect("Happy"))
                    {
                        FindWheel();
                    }
                }
                return targetWheel != null;
            }

            private void FindWheel()
            {
                targetWheel = null;
                var pooledList = ListPool<ScenePartitionerEntry, GameScenePartitioner>.Allocate();
                var pooledList2 = ListPool<GameObject, WheelRunningMonitor>.Allocate();

                var extents = new Extents(Grid.PosToCell(master.transform.GetPosition()), SEARCHWHEELRADIUS);
                GameScenePartitioner.Instance.GatherEntries(extents, GameScenePartitioner.Instance.completeBuildings, pooledList);

                foreach (ScenePartitionerEntry item in pooledList)
                {
                    var squirrelGenerator = (item.obj as KMonoBehaviour).GetComponent<SquirrelGenerator>();
                    if (squirrelGenerator != null && squirrelGenerator.IsOperational
                        && !squirrelGenerator.HasTag(GameTags.Creatures.ReservedByCreature)
                        && GetComponent<Navigator>().CanReach(Grid.PosToCell(squirrelGenerator)))
                    {
                        pooledList2.Add(squirrelGenerator.gameObject);
                    }
                }
                if (pooledList2.Count > 0)
                {
                    int index = Random.Range(0, pooledList2.Count);
                    targetWheel = pooledList2[index];
                }
                pooledList.Recycle();
                pooledList2.Recycle();
            }

            public void OnRunningComplete()
            {
                targetWheel = null;
                RefreshSearchTime();
            }
        }

        public override void InitializeStates(out BaseState default_state)
        {
            serializable = true;
            default_state = root;
            root.ToggleBehaviour(WheelRunningStates.WantsToWheelRunning, (Instance smi) => smi.ShouldRunInWheel(), (Instance smi) => smi.OnRunningComplete());
        }
    }
}
