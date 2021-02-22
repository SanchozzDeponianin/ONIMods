using UnityEngine;

namespace ButcherStation
{
    public class FishingStationGuide : KMonoBehaviour, IRenderEveryTick, ISim1000ms
    {
        static readonly int MinDepth = 1;
        static readonly int MaxDepth = 4;
        private int previousDepthAvailable = -1;
        private bool previousWaterFound = false;
        public GameObject parent;
        public bool occupyTiles = false;
        public bool isPreview = true;
        private KBatchedAnimController parentController;
        private KBatchedAnimController guideController;
        private static StatusItem statusItemNoDepth;
        private static StatusItem statusItemNoWater;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            if (statusItemNoDepth == null)
            {
                statusItemNoDepth = new StatusItem("NOTENOUGHDEPTH", "BUILDING", "status_item_no_fishable_water_below", StatusItem.IconType.Custom, NotificationType.BadMinor, false, OverlayModes.None.ID);
            }
            if (statusItemNoWater == null)
            {
                statusItemNoWater = new StatusItem("NOTENOUGHWATER", "BUILDING", "status_item_no_fishable_water_below", StatusItem.IconType.Custom, NotificationType.BadMinor, false, OverlayModes.None.ID);
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            parentController = parent.GetComponent<KBatchedAnimController>();
            guideController = GetComponent<KBatchedAnimController>();
            RefreshTint();
            RefreshDepthAvailable();
        }

        private void RefreshTint()
        {
            guideController.TintColour = parentController.TintColour;
        }

        private void RefreshDepthAvailable()
        {
            bool waterFound;
            int depthAvailable = GetDepthAvailable(parent, out waterFound);
            if (depthAvailable != previousDepthAvailable || waterFound != previousWaterFound)
            {
                var kBatchedAnimController = GetComponent<KBatchedAnimController>();
                if (depthAvailable == 0)
                {
                    kBatchedAnimController.enabled = false;
                }
                else
                {
                    kBatchedAnimController.enabled = true;
                    kBatchedAnimController.Offset = new Vector3(0, -depthAvailable + 0.35f);
                    for (int i = 1; i <= MaxDepth; i++)
                    {
                        kBatchedAnimController.SetSymbolVisiblity("line" + i.ToString(), i <= depthAvailable);
                        kBatchedAnimController.SetSymbolVisiblity("lineplace" + i.ToString(), i <= depthAvailable);
                    }
                    kBatchedAnimController.sceneLayer = Grid.SceneLayer.BuildingBack;
                    kBatchedAnimController.Play(kBatchedAnimController.initialAnim , KAnim.PlayMode.Loop, 1f, 0f);
                }
                if (occupyTiles)
                {
                    OccupyArea(parent, depthAvailable);
                }
                if (!isPreview)
                {
                    var kSelectable = parent.GetComponent<KSelectable>();
                    if (kSelectable != null)
                    {
                        kSelectable.ToggleStatusItem(statusItemNoDepth, depthAvailable == 0);
                        kSelectable.ToggleStatusItem(statusItemNoWater, depthAvailable > 0 && !waterFound);
                    }
                }
                previousDepthAvailable = depthAvailable;
                previousWaterFound = waterFound;
            }
        }

        public void RenderEveryTick(float dt)
        {
            RefreshTint();
            if (isPreview)
            {
                RefreshDepthAvailable();
            }
        }

        public void Sim1000ms(float dt)
        {
            if (!isPreview)
            {
                RefreshDepthAvailable();
            }
        }

        public static void OccupyArea(GameObject go, int depth_available)
        {
            int root_cell = Grid.CellBelow(Grid.PosToCell(go.transform.GetPosition()));
            for (int i = MinDepth; i <= MaxDepth; i++)
            {
                int cell = Grid.OffsetCell(root_cell, 0, -i);
                if (i <= depth_available)
                {
                    Grid.ObjectLayers[1][cell] = go;
                }
                else if (Grid.ObjectLayers[1].ContainsKey(cell) && Grid.ObjectLayers[1][cell] == go)
                {
                    Grid.ObjectLayers[1][cell] = null;
                }
            }
        }

        public static int GetDepthAvailable(GameObject go, out bool waterFound)
        {
            int root_cell = Grid.CellBelow(Grid.PosToCell(go));
            int result = 0;
            waterFound = false;
            for (int i = MinDepth; i <= MaxDepth; i++)
            {
                int cell = Grid.OffsetCell(root_cell, 0, -i);
                if (!Grid.IsValidCell(cell) || Grid.Solid[cell] || (Grid.ObjectLayers[1].ContainsKey(cell) && !(Grid.ObjectLayers[1][cell] == null) && !(Grid.ObjectLayers[1][cell] == go)) )
                {
                    break;
                }
                result = i;
                if (result > MinDepth && Grid.IsSubstantialLiquid(cell))
                {
                    waterFound = true;
                    break;
                }
            }
            if (result <= MinDepth)
            {
                result = 0;
            }
            return result;
        }
    }
}
