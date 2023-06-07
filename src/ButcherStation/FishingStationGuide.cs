using UnityEngine;

namespace ButcherStation
{
    [SkipSaveFileSerialization]
    public class FishingStationGuide : KMonoBehaviour, /*IRenderEveryTick,*/ ISim1000ms
    {
        private static readonly EventSystem.IntraObjectHandler<FishingStationGuide> OnOperationalChangedDelegate =
            new EventSystem.IntraObjectHandler<FishingStationGuide>((component, data) => component.OnOperationalChanged(data));

        public const int MinDepth = 1;
        public const int MaxDepth = 5;
        private int previousDepth = -1;
        private bool previousWaterFound = false;
        public int TargetRanchCell { get; private set; } = Grid.InvalidCell;

        public enum GuideType { Preview, UnderConstruction, Complete }
        public GuideType type;

        private string lineAnim;
        private KAnim.PlayMode playMode;
        private KBatchedAnimController line;
        private KBatchedAnimController hook;

#pragma warning disable CS0649
        [MyCmpGet]
        private KSelectable kSelectable;

        [MyCmpReq]
        private KBatchedAnimController kbac;
#pragma warning restore CS0649

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
            var kanim = Assets.GetAnim("fishingline_kanim");
            string snapto = "snapto_pivot";
            if (type == GuideType.Complete)
            {
                Subscribe((int)GameHashes.OperationalChanged, OnOperationalChangedDelegate);
                lineAnim = "line";
                string hookAnim = "hook";
                playMode = KAnim.PlayMode.Loop;
                line = AddGuide(kbac, snapto, kanim, lineAnim, true);
                hook = AddGuide(line, snapto, kanim, hookAnim, true);
                hook.Play(hookAnim, playMode);
            }
            else
            {
                lineAnim = "line_place";
                playMode = KAnim.PlayMode.Once;
                line = AddGuide(kbac, snapto, kanim, lineAnim, false);
            }
            RefreshDepth();
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.OperationalChanged, OnOperationalChangedDelegate, true);
            base.OnCleanUp();
        }

        private void OnOperationalChanged(object data)
        {
            if ((bool)data)
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

        private KBatchedAnimController AddGuide(KBatchedAnimController parent, string target_symbol, KAnimFile kanim, string animation, bool create_meter)
        {
            var go = new GameObject { name = parent.name + "." + animation };
            go.SetActive(false);
            go.transform.parent = parent.transform;
            var position = parent.transform.GetPosition();
            position.z = parent.transform.GetPosition().z + 0.1f; // Meter.Offset.Behind
            go.transform.SetPosition(position);
            var kbak = go.AddOrGet<KBatchedAnimController>();
            kbak.AnimFiles = new KAnimFile[] { kanim ?? parent.AnimFiles[0] };
            kbak.initialAnim = animation;
            kbak.fgLayer = Grid.SceneLayer.NoLayer;
            kbak.initialMode = KAnim.PlayMode.Paused;
            kbak.isMovable = true;
            kbak.visibilityType = KAnimControllerBase.VisibilityType.OffscreenUpdate;
            kbak.FlipX = parent.FlipX;
            kbak.FlipY = parent.FlipY;
            kbak.TintColour = parent.TintColour;
            kbak.HighlightColour = parent.HighlightColour;
            if (create_meter)
            {
                var tracker = go.AddOrGet<KBatchedAnimTracker>();
                tracker.symbol = new HashedString(target_symbol);
                tracker.matchParentOffset = true;
                _ = new MeterController(parent, kbak, target_symbol) { gameObject = go };
            }
            parent.SetSymbolVisiblity(target_symbol, false);
            go.SetActive(true);
            return kbak;
        }

        private void RefreshDepth()
        {
            int depth = GetDepthAvailable(gameObject, out bool waterFound);
            if (depth != previousDepth || waterFound != previousWaterFound)
            {
                if (depth <= 0)
                {
                    line.SetVisiblity(false);
                    hook?.SetVisiblity(false);
                }
                else
                {
                    line.SetVisiblity(true);
                    line.Play(lineAnim + depth, playMode);
                    hook?.SetVisiblity(true);
                }
                if (type != GuideType.Preview)
                {
                    if (kSelectable != null)
                    {
                        kSelectable.ToggleStatusItem(statusItemNoDepth, depth <= 0);
                        kSelectable.ToggleStatusItem(statusItemNoWater, depth > 0 && !waterFound);
                    }
                }
                previousDepth = depth;
                previousWaterFound = waterFound;
            }
            if (type == GuideType.Complete)
            {
                int cell = (depth > 0) ? Grid.OffsetCell(Grid.CellBelow(Grid.PosToCell(this)), 0, -depth) : Grid.InvalidCell;
                TargetRanchCell = Grid.IsValidCell(cell) ? cell : Grid.InvalidCell;
            }
        }

        public void RenderEveryTick(float dt)
        {
            if (type == GuideType.Preview)
                RefreshDepth();
        }

        public void Sim1000ms(float dt)
        {
            if (type != GuideType.Preview)
                RefreshDepth();
        }

        private static int GetDepthAvailable(GameObject go, out bool waterFound)
        {
            int root_cell = Grid.CellBelow(Grid.PosToCell(go));
            int depth = 0;
            int depthWithWater = 0;
            waterFound = false;
            for (int i = MinDepth; i <= MaxDepth; i++)
            {
                int cell = Grid.OffsetCell(root_cell, 0, -i);
                if (IsCellBlockedCB(cell))
                    break;
                depth = i;
                if (depth > MinDepth && Grid.IsSubstantialLiquid(cell))
                {
                    waterFound = true;
                    depthWithWater = depth;
                }
            }
            if (depth <= MinDepth)
                depth = 0;
            return waterFound ? depthWithWater : depth;
        }

        public static bool IsCellBlockedCB(int cell)
        {
            return !Grid.IsValidCell(cell) || Grid.Solid[cell] || Grid.Objects[cell, (int)ObjectLayer.Building] != null;
        }
    }
}
