using Klei.AI;

namespace BetterPlantTending
{
    public class TendedPlant : KMonoBehaviour
    {
        public const string FARM_TINKER_EFFECT_ID = "FarmTinker";
        public const string DIVERGENT_CROP_TENDED_EFFECT_ID = "DivergentCropTended";
        public const string DIVERGENT_CROP_TENDED_WORM_EFFECT_ID = "DivergentCropTendedWorm";

        protected static readonly string[] CropTendingEffects = new string[] {
            FARM_TINKER_EFFECT_ID,
#if EXPANSION1
            DIVERGENT_CROP_TENDED_EFFECT_ID,
            DIVERGENT_CROP_TENDED_WORM_EFFECT_ID,
#endif
        };

        [MyCmpReq]
        protected Effects effects;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.EffectAdded, OnEffectChanged);
            Subscribe((int)GameHashes.EffectRemoved, OnEffectChanged);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.EffectAdded, OnEffectChanged);
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
