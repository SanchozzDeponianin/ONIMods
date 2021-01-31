using Klei.AI;
using static BetterPlantTending.BetterPlantTendingAssets;

namespace BetterPlantTending
{
    public abstract class TendedPlant : KMonoBehaviour
    {
        protected static readonly string[] CropTendingEffects = new string[] {
            FARM_TINKER_EFFECT_ID,
#if EXPANSION1
            DIVERGENT_CROP_TENDED_EFFECT_ID,
            DIVERGENT_CROP_TENDED_WORM_EFFECT_ID,
#endif
        };

        [MyCmpReq]
        protected Effects effects;

        protected virtual bool ApplyModifierOnEffectRemoved => true;

        private static readonly EventSystem.IntraObjectHandler<TendedPlant> OnEffectChangedDelegate = new EventSystem.IntraObjectHandler<TendedPlant>(delegate (TendedPlant component, object data)
        {
            component.ApplyModifier();
        });

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.EffectAdded, OnEffectChangedDelegate);
            if (ApplyModifierOnEffectRemoved)
                Subscribe((int)GameHashes.EffectRemoved, OnEffectChangedDelegate);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.EffectAdded, OnEffectChangedDelegate);
            if (ApplyModifierOnEffectRemoved)
                Unsubscribe((int)GameHashes.EffectRemoved, OnEffectChangedDelegate);
            base.OnCleanUp();
        }

        public virtual void ApplyModifier()
        {
        }
    }
}
