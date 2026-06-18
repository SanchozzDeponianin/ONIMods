using UnityEngine;

namespace ButcherStation
{
    [SkipSaveFileSerialization]
    public class MakeFakeBaseSolid : KMonoBehaviour
    {
        // фейковый как бы твёрдый пол, не пропускающий падающие предметы, но водогазопроницаемый

        [SerializeField]
        public bool makeCenterSolid;

        private Extents extents;

        private HandleVector<int>.Handle solidEntry;

        public override void OnSpawn()
        {
            base.OnSpawn();
            extents = GetComponent<Building>().GetExtents();
            extents.height = 1;
            SetFloor(true);
            solidEntry = GameScenePartitioner.Instance.Add("MakeFakeBaseSolid.OnSpawn", gameObject, extents,
                GameScenePartitioner.Instance.solidChangedLayer, RefreshFoundation);
        }

        public override void OnCleanUp()
        {
            GameScenePartitioner.Instance.Free(ref solidEntry);
            SetFloor(false);
            base.OnCleanUp();
        }

        // выставляем:
        // Grid.FakeFloor - чтобы точно ходить
        // Grid.SetSolid - делаем "твёрдым" поскольку именно сюда смотрит гравитация. флаг может перезаписаться например при замерзании / оттаивании
        // Grid.Foundation - чтобы отключить команду выкапывание и разделить комнаты

        private const Sim.Cell.Properties floorCellProperties
            = Sim.Cell.Properties.SolidImpermeable | Sim.Cell.Properties.Opaque | Sim.Cell.Properties.Transparent;
        private bool runSimCallback;

        public void SetFloor(bool active)
        {
            int center = Grid.PosToCell(this);
            for (int i = 0; i < extents.width; i++)
            {
                int cell = Grid.XYToCell(extents.x + i, extents.y);
                if (active)
                {
                    if (makeCenterSolid || cell != center)
                    {
                        Grid.FakeFloor.Add(cell);
                        Grid.SetSolid(cell, true, CellEventLogger.Instance.SimCellOccupierForceSolid);
                        runSimCallback = true;
                        var handle = Game.Instance.callbackManager.Add(new(OnCellPropertiesChanged, false));
                        SimMessages.SetCellProperties(cell, (byte)floorCellProperties, handle.index);
                        SimMessages.SetStrength(cell, 0, 1f);
                    }
                    Grid.Foundation[cell] = !Grid.Element[cell].IsSolid;
                }
                else
                {
                    if (makeCenterSolid || cell != center)
                    {
                        Grid.FakeFloor.Remove(cell);
                        Grid.SetSolid(cell, false, CellEventLogger.Instance.SimCellOccupierDestroy);
                        SimMessages.ClearCellProperties(cell, (byte)floorCellProperties);
                        SimMessages.SetStrength(cell, 1, 1f);
                    }
                    Grid.Foundation[cell] = false;
                    GameScenePartitioner.Instance.TriggerEvent(cell, GameScenePartitioner.Instance.solidChangedLayer, null);
                }
                World.Instance.OnSolidChanged(cell);
                Pathfinding.Instance.AddDirtyNavGridCell(cell);
            }
        }

        // если оказалось реально твёрдым - отменяем Grid.Foundation чтобы можно было выкопать
        // иначе отменяем выкапывание если было и восстанавливаем Grid.Foundation и Grid.SetSolid
        private void RefreshFoundation(object data)
        {
            int center = Grid.PosToCell(this);
            for (int i = 0; i < extents.width; i++)
            {
                int cell = Grid.XYToCell(extents.x + i, extents.y);
                if (Grid.Element[cell].IsSolid)
                    Grid.Foundation[cell] = false;
                else
                {
                    Grid.Foundation[cell] = true;
                    var diggable = Diggable.GetDiggable(cell);
                    if (diggable != null)
                        diggable.Trigger((int)GameHashes.Cancel);
                    if (makeCenterSolid || cell != center)
                        Grid.SetSolid(cell, true, CellEventLogger.Instance.SimCellOccupierForceSolid);
                }
            }
        }

        // по аналогии с SimCellOccupier, для починки прозрачности
        private void OnCellPropertiesChanged()
        {
            if (this == null || gameObject == null || !runSimCallback)
                return;
            for (int i = 0; i < extents.width; i++)
            {
                int cell = Grid.XYToCell(extents.x + i, extents.y);
                GameScenePartitioner.Instance.TriggerEvent(cell, GameScenePartitioner.Instance.solidChangedLayer, null);
            }
            runSimCallback = false;
        }
    }
}
