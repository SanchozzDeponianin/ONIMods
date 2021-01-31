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

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.EffectAdded, OnEffectChanged);
            if (ApplyModifierOnEffectRemoved)
                Subscribe((int)GameHashes.EffectRemoved, OnEffectChanged);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.EffectAdded, OnEffectChanged);
            if (ApplyModifierOnEffectRemoved)
                Unsubscribe((int)GameHashes.EffectRemoved, OnEffectChanged);
            base.OnCleanUp();
        }

        private void OnEffectChanged(object data)
        {
            ApplyModifier();
        }

        public virtual void ApplyModifier()
        {
        }
    }
}
