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

        public override void ApplyModifier()
        {
            base.ApplyModifier();
            if (growing.IsGrown() && effects.HasEffect(FARMTINKEREFFECTID))
            {
                for (int i = 0; i < ForestTreeConfig.NUM_BRANCHES; i++)
                {
                    buddingTrunk.GetBranchAtPosition(i)?.GetComponent<Effects>()?.Add(FARMTINKEREFFECTID, false);
                }
            }
        }

        public static void ApplyModifierToBranch(TreeBud branch, BuddingTrunk buddingTrunk)
        {
            var parentEffects = buddingTrunk?.GetComponent<Effects>();
            var branchEffects = branch?.GetComponent<Effects>();
            if (parentEffects != null && branchEffects != null && parentEffects.HasEffect(FARMTINKEREFFECTID))
            {
                branchEffects.Add(FARMTINKEREFFECTID, false).timeRemaining = parentEffects.Get(FARMTINKEREFFECTID).timeRemaining;
            }
        }
    }
}
