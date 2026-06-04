namespace ButcherStation
{
    [SkipSaveFileSerialization]
    public class MakeFakeBaseSolid : KMonoBehaviour
    {
        // фейковый как бы твёрдый пол, не пропускающий падающие предметы, но водогазопроницаемый

#pragma warning disable CS0649
        [MyCmpReq]
        private Building building;
#pragma warning restore CS0649

        private HandleVector<int>.Handle solidEntry;

        public override void OnSpawn()
        {
            base.OnSpawn();
            SetFloor(true);
            var extents = building.GetExtents();
            extents.height = 1;
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
        // Grid.Foundation - чтобы отключить команду выкапывание

        private const Sim.Cell.Properties floorCellProperties = Sim.Cell.Properties.SolidImpermeable | Sim.Cell.Properties.Opaque;

        public void SetFloor(bool active)
        {
            var extents = building.GetExtents();
            for (int i = 0; i < extents.width; i++)
            {
                int cell = Grid.XYToCell(extents.x + i, extents.y);
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
            var extents = building.GetExtents();
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
                    Grid.SetSolid(cell, true, CellEventLogger.Instance.SimCellOccupierForceSolid);
                }
            }
        }
    }
}
