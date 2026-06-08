using UnityEngine;

namespace ButcherStation
{
    [SkipSaveFileSerialization]
    public class MakeFakeBaseSolid : KMonoBehaviour
    {
        // фейковый как бы твёрдый пол, не пропускающий падающие предметы, но водогазопроницаемый

        [SerializeField]
        public CellOffset[] floorOffsets;

        private HandleVector<int>.Handle[] solidEntries;

        public override void OnSpawn()
        {
            base.OnSpawn();
            SetFloor(true);
            solidEntries = new HandleVector<int>.Handle[floorOffsets.Length];
            for (int i = 0; i < floorOffsets.Length; i++)
            {
                solidEntries[i] = GameScenePartitioner.Instance.Add("MakeFakeBaseSolid.OnSpawn", gameObject,
                    Extents.OneCell(Grid.OffsetCell(Grid.PosToCell(this), floorOffsets[i])),
                    GameScenePartitioner.Instance.solidChangedLayer, RefreshFoundation);
            }
        }

        public override void OnCleanUp()
        {
            for (int i = 0; i < floorOffsets.Length; i++)
                GameScenePartitioner.Instance.Free(ref solidEntries[i]);
            SetFloor(false);
            base.OnCleanUp();
        }

        // выставляем:
        // Grid.FakeFloor - чтобы точно ходить
        // Grid.SetSolid - делаем "твёрдым" поскольку именно сюда смотрит гравитация. флаг может перезаписаться например при замерзании / оттаивании
        // Grid.Foundation - чтобы отключить команду выкапывание

        private const Sim.Cell.Properties floorCellProperties = Sim.Cell.Properties.SolidImpermeable | Sim.Cell.Properties.Opaque;

        public void SetFloor(bool active)
        {
            for (int i = 0; i < floorOffsets.Length; i++)
            {
                int cell = Grid.OffsetCell(Grid.PosToCell(this), floorOffsets[i]);
                if (active)
                {
                    Grid.FakeFloor.Add(cell);
                    Grid.SetSolid(cell, true, CellEventLogger.Instance.SimCellOccupierForceSolid);
                    SimMessages.SetCellProperties(cell, (byte)floorCellProperties);
                    SimMessages.SetStrength(cell, 0, 1f);
                    //Game.Instance.AddSolidChangedFilter(cell);
                    Grid.Foundation[cell] = !Grid.Element[cell].IsSolid;
                    //Grid.RenderedByWorld[cell] = false;
                }
                else
                {
                    Grid.FakeFloor.Remove(cell);
                    Grid.SetSolid(cell, false, CellEventLogger.Instance.SimCellOccupierDestroy);
                    SimMessages.ClearCellProperties(cell, (byte)floorCellProperties);
                    SimMessages.SetStrength(cell, 1, 1f);
                    //Game.Instance.RemoveSolidChangedFilter(cell);
                    Grid.Foundation[cell] = false;
                    //Grid.RenderedByWorld[cell] = true;
                }
                World.Instance.OnSolidChanged(cell);
                GameScenePartitioner.Instance.TriggerEvent(cell, GameScenePartitioner.Instance.solidChangedLayer, null);
                Pathfinding.Instance.AddDirtyNavGridCell(cell);
            }
        }

        // если оказалось реально твёрдым - отменяем Grid.Foundation чтобы можно было выкопать
        // иначе отменяем выкапывание если было и восстанавливаем Grid.Foundation и Grid.SetSolid
        private void RefreshFoundation(object data)
        {
            for (int i = 0; i < floorOffsets.Length; i++)
            {
                int cell = Grid.OffsetCell(Grid.PosToCell(this), floorOffsets[i]);
                if (Grid.Element[cell].IsSolid)
                    Grid.Foundation[cell] = false;
                else
                {
                    Grid.Foundation[cell] = true;
                    var diggable = Diggable.GetDiggable(cell);
                    if (diggable != null)
                        diggable.Trigger((int)GameHashes.Cancel);
                    Grid.SetSolid(cell, true, CellEventLogger.Instance.SimCellOccupierForceSolid);
                }
            }
        }
    }
}
