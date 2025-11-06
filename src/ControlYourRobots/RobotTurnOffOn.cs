namespace ControlYourRobots
{
    public class RobotTurnOffOn : Switch
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private KPrefabID kPrefabID;

        [MySmiReq]
        private RobotAi.Instance robotAi;
#pragma warning restore CS0649
        protected override void OnSpawn()
        {
            OnToggle += OnSwitchToggled;
            base.OnSpawn();
        }

        private void OnSwitchToggled(bool toggled_on)
        {
            kPrefabID.SetTag(Patches.RobotSuspend, !toggled_on);
        }

        protected override void OnRefreshUserMenu(object data)
        {
            if (!kPrefabID.HasTag(GameTags.Stored) && !robotAi.IsNullOrStopped() && robotAi.IsInsideState(robotAi.sm.alive))
                base.OnRefreshUserMenu(data);
        }

        protected override void UpdateSwitchStatus() { }
    }
}
