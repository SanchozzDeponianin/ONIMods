using System;

namespace EndlessTelescope
{
    [SkipSaveFileSerialization]
    public class DeepSpaceTelescope : KMonoBehaviour
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private KSelectable selectable;

        [MyCmpGet]
        private ClusterTelescope.ClusterTelescopeWorkable workable;

        [MySmiGet]
        private ClusterTelescope.Instance smi;
#pragma warning restore CS0649

        private int currentDistance;
        public float EfficiencyMultiplier { get; private set; } = 1f;
        private bool IsDeepSpace => EfficiencyMultiplier < 1f;
        private static StatusItem statusItem;
        private Guid guid;

        protected override void OnPrefabInit()
        {
            if (statusItem == null)
            {
                statusItem = new StatusItem(
                    id: "TELESCOPE_DEEP_SPACE",
                    name: STRINGS.BUILDING.STATUSITEMS.TELESCOPE_DEEP_SPACE.NAME,
                    tooltip: STRINGS.BUILDING.STATUSITEMS.TELESCOPE_DEEP_SPACE.TOOLTIP,
                    icon: null,
                    icon_type: StatusItem.IconType.Info,
                    notification_type: NotificationType.BadMinor,
                    allow_multiples: false,
                    render_overlay: OverlayModes.None.ID,
                    showWorldIcon: false);
                statusItem.resolveTooltipCallback = (str, obj) =>
                    {
                        var dst = obj as DeepSpaceTelescope;
                        if (dst != null)
                            return string.Format(str, dst.smi.def.analyzeClusterRadius, dst.currentDistance, GameUtil.GetFormattedPercent(dst.EfficiencyMultiplier * 100f));
                        return str;
                    };
            }
            base.OnPrefabInit();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            workable.OnWorkableEventCB += OnWorkableEvent;
        }

        public void UpdateEfficiencyMultiplier(bool has_target)
        {
            if (has_target)
            {
                currentDistance = AxialUtil.GetDistance(this.GetMyWorldLocation(), smi.GetAnalyzeTarget());
                EfficiencyMultiplier = 1f / (1 + Math.Max(0, currentDistance - smi.def.analyzeClusterRadius));
            }
            else
            {
                currentDistance = 0;
                EfficiencyMultiplier = 1f;
            }
            guid = selectable.ToggleStatusItem(statusItem, guid, IsDeepSpace && workable.worker != null, this);
        }

        private void OnWorkableEvent(Workable workable, Workable.WorkableEvent ev)
        {
            if (ev == Workable.WorkableEvent.WorkStarted)
            {
                guid = selectable.ToggleStatusItem(statusItem, guid, IsDeepSpace, this);
            }
            else if (ev == Workable.WorkableEvent.WorkStopped)
            {
                guid = selectable.RemoveStatusItem(guid);
            }
        }
    }
}
