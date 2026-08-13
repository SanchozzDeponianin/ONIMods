using System;

namespace ControlYourRobots
{
    public class DefaultFilterable : KMonoBehaviour, ISidescreenButtonControl
    {
#pragma warning disable CS0649
        [MyCmpReq]
        TreeFilterable filterable;
#pragma warning restore CS0649

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            if (DefaultFilterableManager.Instance != null && DefaultFilterableManager.Instance.TryGet(this.PrefabID(), out var tags))
                filterable.UpdateFilters(tags);
        }
        public void OnSidescreenButtonPressed() => DefaultFilterableManager.Instance.Set(this.PrefabID(), filterable.AcceptedTags);
        public string SidescreenButtonText => STRINGS.UI.USERMENUACTIONS.DEFAULTFILTERABLE.NAME;
        public string SidescreenButtonTooltip => string.Format(STRINGS.UI.USERMENUACTIONS.DEFAULTFILTERABLE.TOOLTIP, this.GetProperName());
        public int ButtonSideScreenSortOrder() => -10;
        public int HorizontalGroupID() => -1;
        public void SetButtonTextOverride(ButtonMenuTextOverride textOverride) => throw new NotImplementedException();
        public bool SidescreenButtonInteractable() => true;
        public bool SidescreenEnabled() => true;
    }
}
