using Klei.AI;
using STRINGS;

namespace BetterPlantTending
{
    internal static class BetterPlantTendingAssets
    {
        internal const string FARM_TINKER_EFFECT_ID = "FarmTinker";
        internal const string DIVERGENT_CROP_TENDED_EFFECT_ID = "DivergentCropTended";
        internal const string DIVERGENT_CROP_TENDED_WORM_EFFECT_ID = "DivergentCropTendedWorm";
        internal const float FARM_TINKER_BONUS_DECOR = 0.5f;

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

        internal static void Init()
        {
            var db = Db.Get();
            var effectFarmTinker = db.effects.Get(FARM_TINKER_EFFECT_ID);
            var toPercent = new ToPercentAttributeFormatter(1f);

            fakeGrowingRate = new AttributeModifier(
                attribute_id: db.Amounts.Maturity.deltaAttribute.Id,
                value: TUNING.CROPS.GROWTH_RATE,
                description: CREATURES.STATS.MATURITY.GROWING,
                is_multiplier: false,
                is_readonly: true);

            FarmTinkerBonusDecor = new AttributeModifier(
                attribute_id: db.BuildingAttributes.Decor.Id,
                value: FARM_TINKER_BONUS_DECOR,
                description: DUPLICANTS.MODIFIERS.FARMTINKER.NAME,
                is_multiplier: true,
                is_readonly: false);
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
                value: EXTRA_SEED_CHANCE_BASE_VALUE_DECORATIVE,
                is_readonly: false);

            ExtraSeedChanceNotDecorativeBaseValue = new AttributeModifier(
                attribute_id: ExtraSeedChance.Id,
                value: EXTRA_SEED_CHANCE_BASE_VALUE_NOT_DECORATIVE,
                is_readonly: false);

            // модификаторы для жучинкусов
            var effectDivergentCropTended = db.effects.Get(DIVERGENT_CROP_TENDED_EFFECT_ID);
            var effectWormCropTended = db.effects.Get(DIVERGENT_CROP_TENDED_WORM_EFFECT_ID);

            ExtraSeedChanceDivergentModifier = new AttributeModifier(
                attribute_id: ExtraSeedChance.Id,
                value: EXTRA_SEED_CHANCE_MODIFIER_DIVERGENT,
                is_readonly: false);
            effectDivergentCropTended.Add(ExtraSeedChanceDivergentModifier);

            ExtraSeedChanceWormModifier = new AttributeModifier(
                attribute_id: ExtraSeedChance.Id,
                value: EXTRA_SEED_CHANCE_MODIFIER_WORM,
                is_readonly: false);
            effectWormCropTended.Add(ExtraSeedChanceWormModifier);
        }

        internal static void LoadOptions()
        {
            BetterPlantTendingOptions.Reload();
            var options = BetterPlantTendingOptions.Instance;
            FarmTinkerBonusDecor.SetValue(options.farm_tinker_bonus_decor);
            ExtraSeedChanceDecorativeBaseValue.SetValue(options.extra_seeds.base_chance_decorative);
            ExtraSeedChanceNotDecorativeBaseValue.SetValue(options.extra_seeds.base_chance_not_decorative);
            ExtraSeedChanceDivergentModifier.SetValue(options.extra_seeds.modifier_divergent);
            ExtraSeedChanceWormModifier.SetValue(options.extra_seeds.modifier_worm);
        }
    }
}
