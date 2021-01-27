using Klei.AI;

namespace BetterPlantTending
{
    public class TendedForestTree : TendedPlant
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
            GameScheduler.Instance.Schedule("ApplyModifier", 0.2f, ApplyModifierToAllBranches);
        }

        private void ApplyModifierToAllBranches(object callbackParam)
        {
            if (this != null && growing.IsGrown())
            {
                foreach (var effect_id in CropTendingEffects)
                {
                    var effectInstanceTrunk = effects.Get(effect_id);
                    if (effectInstanceTrunk != null)
                    {
                        for (int i = 0; i < ForestTreeConfig.NUM_BRANCHES; i++)
                        {
                            var effectInstanceBranch = buddingTrunk.GetBranchAtPosition(i)?.GetComponent<Effects>()?.Add(effect_id, false);
                            if (effectInstanceBranch != null)
                            {
                                effectInstanceBranch.timeRemaining = effectInstanceTrunk.timeRemaining;
                            }
                        }
                    }
                }
            }
        }

        public static void ApplyModifierToBranch(TreeBud branch, BuddingTrunk buddingTrunk)
        {
            var parentEffects = buddingTrunk?.GetComponent<Effects>();
            var branchEffects = branch?.GetComponent<Effects>();
            if (parentEffects != null && branchEffects != null)
            {
                foreach (var effect_id in CropTendingEffects)
                {
                    var effectInstanceTrunk = parentEffects.Get(effect_id);
                    if (effectInstanceTrunk != null)
                    {
                        var effectInstanceBranch = branchEffects.Add(effect_id, false);
                        if (effectInstanceBranch != null)
                        {
                            effectInstanceBranch.timeRemaining = effectInstanceTrunk.timeRemaining;
                        }
                    }
                }
            }
        }
    }
}
