namespace ButcherStation
{
    [SkipSaveFileSerialization]
    public class FishingStation : KMonoBehaviour, ISim4000ms
    {
        private static readonly EventSystem.IntraObjectHandler<FishingStation> OnOperationalChangedDelegate =
            new((component, data) => component.OnOperationalChanged(data));

        public const int MinDepth = 1;
        public const int MaxDepth = 5;

        public int TargetRanchCell { get; private set; } = Grid.InvalidCell;

#pragma warning disable CS0649
        [MyCmpReq]
        private KSelectable selectable;

        [MyCmpReq]
        private KBatchedAnimController kbac;
#pragma warning restore CS0649

        private static StatusItem statusItemNoDepth;
        private static StatusItem statusItemNoWater;

        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            if (statusItemNoDepth == null)
            {
                statusItemNoDepth = new StatusItem("NOTENOUGHDEPTH", "BUILDING", "status_item_no_fishable_water_below",
                    StatusItem.IconType.Custom, NotificationType.BadMinor, false, OverlayModes.None.ID);
                statusItemNoWater = new StatusItem("NOTENOUGHWATER", "BUILDING", "status_item_no_fishable_water_below",
                    StatusItem.IconType.Custom, NotificationType.BadMinor, false, OverlayModes.None.ID);
            }
        }

        private HandleVector<int>.Handle solidEntry;
        private HandleVector<int>.Handle liquidEntry;
        private HandleVector<int>.Handle buildingsEntry;

        public override void OnSpawn()
        {
            base.OnSpawn();
            kbac.SetSymbolVisiblity("snapto_pivot", false);
            Subscribe((int)GameHashes.OperationalChanged, OnOperationalChangedDelegate);
            var extents = Extents.OneCell(Grid.OffsetCell(Grid.PosToCell(this), 0, MaxDepth));
            extents.height = MaxDepth;
            solidEntry = GameScenePartitioner.Instance.Add("FishingStation.OnSpawn", gameObject, extents,
                GameScenePartitioner.Instance.solidChangedLayer, RefreshDepth);
            liquidEntry = GameScenePartitioner.Instance.Add("FishingStation.OnSpawn", gameObject, extents,
                GameScenePartitioner.Instance.liquidChangedLayer, RefreshDepth);
            buildingsEntry = GameScenePartitioner.Instance.Add("FishingStation.OnSpawn", gameObject, extents,
                GameScenePartitioner.Instance.objectLayers[(int)ObjectLayer.Building], RefreshDepth);
            RefreshDepth();
        }

        public override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.OperationalChanged, OnOperationalChangedDelegate, true);
            GameScenePartitioner.Instance.Free(ref solidEntry);
            GameScenePartitioner.Instance.Free(ref liquidEntry);
            GameScenePartitioner.Instance.Free(ref buildingsEntry);
            base.OnCleanUp();
        }

        private void OnOperationalChanged(object data)
        {
            if (((Boxed<bool>)data).value)
            {
                kbac.Queue("on_pre");
                kbac.Queue("on");
            }
            else
            {
                kbac.Queue("on_pst");
                kbac.Queue("off");
            }
        }

        public void Sim4000ms(float dt) => RefreshDepth();

        private int previousDepth = -1;
        private int previousWaterDepth = -1;

        private void RefreshDepth(object data = null)
        {
            int depth = GetDepthAvailable(Grid.PosToCell(this), out int water_depth);
            if (depth != previousDepth || water_depth != previousWaterDepth)
            {
                selectable.ToggleStatusItem(statusItemNoDepth, depth <= 0);
                selectable.ToggleStatusItem(statusItemNoWater, water_depth <= 0);
                previousDepth = depth;
                previousWaterDepth = water_depth;
                TargetRanchCell = (water_depth > 0) ? Grid.OffsetCell(Grid.PosToCell(this), 0, -depth) : Grid.InvalidCell;
            }
        }

        private static int GetDepthAvailable(int root_cell, out int water_depth)
        {
            int depth = 0;
            water_depth = 0;
            for (int i = 1; i <= MaxDepth; i++)
            {
                int cell = Grid.OffsetCell(root_cell, 0, -i);
                if (IsCellBlockedCB(cell))
                    break;
                depth = i;
                if (depth >= MinDepth && Grid.IsSubstantialLiquid(cell))
                    water_depth = depth;
            }
            if (depth < MinDepth)
                depth = 0;
            return depth;
        }

        public static bool IsCellBlockedCB(int cell)
        {
            return !Grid.IsValidCell(cell) || Grid.Solid[cell] || Grid.Objects[cell, (int)ObjectLayer.Building] != null;
        }
    }
}
