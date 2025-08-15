using UnityEngine;

namespace TravelTubesExpanded
{
    // для прохождения насквозь через пускачь, эмулируем наличие куска трубы в средних клетках
    // а также мониторим подключения мостов в клетке над пускачом, для входа через мост
    public class EntranceFakeTubes : KMonoBehaviour, ITravelTubePiece
    {
        public Vector3 Position => transform.position;
        private HandleVector<int>.Handle bridgeChangedEntry;
        //private SchedulerHandle updateHandle;

#pragma warning disable CS0649
        [MyCmpReq]
        private TravelTubeEntrance entrance;
#pragma warning restore CS0649

        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            int cell = Grid.PosToCell(this);
            int above = Grid.CellAbove(cell);
            Grid.HasTube[cell] = true;
            Grid.HasTube[above] = true;
            Components.ITravelTubePieces.Add(this);
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            int cell = Grid.PosToCell(this);
            int above = Grid.CellAbove(cell);
            Grid.Objects[cell, (int)ObjectLayer.TravelTubeConnection] = gameObject;
            Grid.Objects[above, (int)ObjectLayer.TravelTubeConnection] = gameObject;
            Pathfinding.Instance.AddDirtyNavGridCell(above);
            int x = (int)transform.GetPosition().x;
            int y = (int)transform.GetPosition().y;
            var extents = new Extents(x, y + 2, 1, 1);
            bridgeChangedEntry = GameScenePartitioner.Instance.Add("TravelTubeEntrance.TubeListener", gameObject, extents,
                GameScenePartitioner.Instance.objectLayers[(int)ObjectLayer.TravelTubeConnection], TubeChanged);
            //updateHandle = GameScheduler.Instance.Schedule("TravelTubeEntrance.TubeListener", 0.4f, TubeChanged);
        }

        public override void OnCleanUp()
        {
            int cell = Grid.PosToCell(this);
            int above = Grid.CellAbove(cell);
            Grid.Objects[cell, (int)ObjectLayer.TravelTubeConnection] = null;
            Grid.Objects[above, (int)ObjectLayer.TravelTubeConnection] = null;
            Grid.HasTube[cell] = false;
            Grid.HasTube[above] = false;
            Components.ITravelTubePieces.Remove(this);
            Pathfinding.Instance.AddDirtyNavGridCell(above);
            GameScenePartitioner.Instance.Free(ref bridgeChangedEntry);
            //updateHandle.ClearScheduler();
            base.OnCleanUp();
        }

        private void TubeChanged(object data)
        {
            if (entrance.travelTube == null)
                entrance.TubeConnectionsChanged((UtilityConnections)0);
        }
    }
}
