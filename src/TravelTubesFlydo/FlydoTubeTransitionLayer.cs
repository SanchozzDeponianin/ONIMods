using TUNING;

namespace TravelTubesFlydo
{
    public class FlydoTubeTransitionLayer : TransitionDriver.OverrideLayer
    {
        public FlydoTubeTransitionLayer(Navigator navigator) : base(navigator){ }

        public override void BeginTransition(Navigator navigator, Navigator.ActiveTransition transition)
        {
            base.BeginTransition(navigator, transition);
            if (transition.navGridTransition.start == NavType.Tube || transition.navGridTransition.end == NavType.Tube)
                navigator.animController.SetSceneLayer(Grid.SceneLayer.BuildingUse);
            if (transition.navGridTransition.start == NavType.Tube && transition.navGridTransition.end == NavType.Tube && transition.isLooping)
                transition.speed = DUPLICANTSTATS.STANDARD.BaseStats.TRANSIT_TUBE_TRAVEL_SPEED;
        }
    }
}
