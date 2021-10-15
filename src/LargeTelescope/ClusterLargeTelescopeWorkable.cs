using UnityEngine;

namespace LargeTelescope
{
    public class ClusterLargeTelescopeWorkable : ClusterTelescope.ClusterTelescopeWorkable
    {
        [SerializeField]
        public float efficiencyMultiplier = 1.5f;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_telescope_kanim") };
            workLayer = Grid.SceneLayer.BuildingFront;
        }

        public override float GetEfficiencyMultiplier(Worker worker)
        {
            return efficiencyMultiplier * base.GetEfficiencyMultiplier(worker);
        }
    }
}
