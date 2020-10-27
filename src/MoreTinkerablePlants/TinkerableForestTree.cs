using Klei.AI;

namespace MoreTinkerablePlants
{
    public class TinkerableForestTree : TinkerableEffectMonitor
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Growing growing;

        [MyCmpReq]
        private BuddingTrunk buddingTrunk;
#pragma warning restore CS0649

        public override void ApplyEffect()
        {
            base.ApplyEffect();
            if (growing.IsGrown() && effects.HasEffect(FARMTINKEREFFECTID))
            {
                for (int i = 0; i < ForestTreeConfig.NUM_BRANCHES; i++)
                {
                    buddingTrunk.GetBranchAtPosition(i)?.GetComponent<Effects>()?.Add(FARMTINKEREFFECTID, false);
                }
            }
        }
    }
}
