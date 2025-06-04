using Klei.AI;
using STRINGS;

namespace BetterPlantTending
{
    internal static class ModAssets
    {
        internal const string FARM_TINKER_EFFECT_ID = "FarmTinker";
        internal const string DIVERGENT_CROP_TENDED_EFFECT_ID = "DivergentCropTended";
        internal const string DIVERGENT_CROP_TENDED_WORM_EFFECT_ID = "DivergentCropTendedWorm";
        internal const float FARM_TINKER_BONUS_DECOR = 0.3f;

        internal const float EXTRA_SEED_CHANCE_BASE_VALUE_DECORATIVE = 1.5f * TUNING.CROPS.BASE_BONUS_SEED_PROBABILITY;
        internal const float EXTRA_SEED_CHANCE_BASE_VALUE_NOT_DECORATIVE = 0;
        internal const float EXTRA_SEED_CHANCE_MODIFIER_DIVERGENT = 0.5f * TUNING.CROPS.BASE_BONUS_SEED_PROBABILITY;
        internal const float EXTRA_SEED_CHANCE_MODIFIER_WORM = 1.5f * TUNING.CROPS.BASE_BONUS_SEED_PROBABILITY;

        internal static AttributeModifier fakeGrowingRate;
        private static AttributeModifier FarmTinkerBonusDecor;
        internal static Attribute ExtraSeedChance;
        internal static AttributeModifier ExtraSeedChanceDecorativeBaseValue;
        internal static AttributeModifier ExtraSeedChanceNotDecorativeBaseValue;
        private static AttributeModifier ExtraSeedChanceDivergentModifier;
        private static AttributeModifier ExtraSeedChanceWormModifier;
        // todo: mimika

        internal static void Init()
        {
            var db = Db.Get();
            var effectFarmTinker = db.effects.Get(FARM_TINKER_EFFECT_ID);
            var toPercent = new ToPercentAttributeFormatter(1f);
            var options = ModOptions.Instance;

            fakeGrowingRate = new AttributeModifier(
                attribute_id: db.Amounts.Maturity.deltaAttribute.Id,
                value: TUNING.CROPS.GROWTH_RATE,
                description: CREATURES.STATS.MATURITY.GROWING,
                is_multiplier: false);

            FarmTinkerBonusDecor = new AttributeModifier(
                attribute_id: db.BuildingAttributes.Decor.Id,
                value: options.farm_tinker_bonus_decor,
                description: DUPLICANTS.MODIFIERS.FARMTINKER.NAME,
                is_multiplier: true);
            effectFarmTinker.Add(FarmTinkerBonusDecor);

            ExtraSeedChance = new Attribute(
                id: nameof(ExtraSeedChance),
                is_trainable: false,
                show_in_ui: Attribute.Display.General,
                is_profession: false,
                base_value: 0);
            ExtraSeedChance.SetFormatter(toPercent);
            db.Attributes.Add(ExtraSeedChance);

            ExtraSeedChanceDecorativeBaseValue = new AttributeModifier(
                attribute_id: ExtraSeedChance.Id,
                value: options.extra_seeds.base_chance_decorative);

            ExtraSeedChanceNotDecorativeBaseValue = new AttributeModifier(
                attribute_id: ExtraSeedChance.Id,
                value: options.extra_seeds.base_chance_not_decorative);

            // модификаторы для жучинкусов
            var effectDivergentCropTended = db.effects.Get(DIVERGENT_CROP_TENDED_EFFECT_ID);
            var effectWormCropTended = db.effects.Get(DIVERGENT_CROP_TENDED_WORM_EFFECT_ID);

            ExtraSeedChanceDivergentModifier = new AttributeModifier(
                attribute_id: ExtraSeedChance.Id,
                value: options.extra_seeds.modifier_divergent);
            effectDivergentCropTended.Add(ExtraSeedChanceDivergentModifier);

            ExtraSeedChanceWormModifier = new AttributeModifier(
                attribute_id: ExtraSeedChance.Id,
                value: options.extra_seeds.modifier_worm);
            effectWormCropTended.Add(ExtraSeedChanceWormModifier);
        }
    }
}
