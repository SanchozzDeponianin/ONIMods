using Klei.AI;
using STRINGS;
using TUNING;

namespace BetterPlantTending
{
    internal static class BetterPlantTendingAssets
    {
        internal const string FARM_TINKER_EFFECT_ID = "FarmTinker";
        internal const string DIVERGENT_CROP_TENDED_EFFECT_ID = "DivergentCropTended";
        internal const string DIVERGENT_CROP_TENDED_WORM_EFFECT_ID = "DivergentCropTendedWorm";

        internal const float THROUGHPUT_BASE_VALUE = 1;
        internal const float THROUGHPUT_MODIFIER_FARMTINKER = 2;
        // todo: поразмыслить над шансами семян
        internal const float EXTRA_SEED_CHANCE_BASE_VALUE_DECORATIVE = CROPS.BASE_BONUS_SEED_PROBABILITY;
        internal const float EXTRA_SEED_CHANCE_BASE_VALUE_NOT_DECORATIVE = 0;
        internal const float EXTRA_SEED_CHANCE_PER_BOTANIST_SKILL = 0.02f;
#if EXPANSION1
        internal const float THROUGHPUT_MODIFIER_DIVERGENT = 0.2f;
        internal const float THROUGHPUT_MODIFIER_WORM = 1;

        internal const float EXTRA_SEED_CHANCE_MODIFIER_DIVERGENT = 0.02f;
        internal const float EXTRA_SEED_CHANCE_MODIFIER_WORM = 0.1f;
#endif
        internal static Attribute ColdBreatherThroughput;
        internal static AttributeModifier ColdBreatherThroughputBaseValue;
        private static AttributeModifier ColdBreatherThroughputFarmTinkerModifier;
#if EXPANSION1
        private static AttributeModifier ColdBreatherThroughputDivergentModifier;
        private static AttributeModifier ColdBreatherThroughputWormModifier;
#endif
        internal static Attribute OxyfernThroughput;
        internal static AttributeModifier OxyfernThroughputBaseValue;
        private static AttributeModifier OxyfernThroughputFarmTinkerModifier;
#if EXPANSION1
        private static AttributeModifier OxyfernThroughputDivergentModifier;
        private static AttributeModifier OxyfernThroughputWormModifier;
#endif
        internal static Attribute ExtraSeedChance;
        internal static AttributeModifier ExtraSeedChanceDecorativeBaseValue;
        internal static AttributeModifier ExtraSeedChanceNotDecorativeBaseValue;
        internal static AttributeConverter ExtraSeedTendingChance;
#if EXPANSION1
        private static AttributeModifier ExtraSeedChanceDivergentModifier;
        private static AttributeModifier ExtraSeedChanceWormModifier;
#endif

        internal static void Init()
        {
            var db = Db.Get();
            var effectFarmTinker = db.effects.Get(FARM_TINKER_EFFECT_ID);
            var toPercent = new ToPercentAttributeFormatter(1f);

            ColdBreatherThroughput = new Attribute(
                id: nameof(ColdBreatherThroughput),
                is_trainable: false,
                show_in_ui: Attribute.Display.General,
                is_profession: false,
                base_value: 0);
            ColdBreatherThroughput.SetFormatter(toPercent);
            db.Attributes.Add(ColdBreatherThroughput);

            ColdBreatherThroughputBaseValue = new AttributeModifier(
                attribute_id: ColdBreatherThroughput.Id,
                value: THROUGHPUT_BASE_VALUE);

            ColdBreatherThroughputFarmTinkerModifier = new AttributeModifier(
                attribute_id: ColdBreatherThroughput.Id,
                value: THROUGHPUT_MODIFIER_FARMTINKER,
                is_multiplier: true,
                is_readonly: false);
            effectFarmTinker.Add(ColdBreatherThroughputFarmTinkerModifier);

            OxyfernThroughput = new Attribute(
                id: nameof(OxyfernThroughput),
                is_trainable: false,
                show_in_ui: Attribute.Display.General,
                is_profession: false,
                base_value: 0);
            OxyfernThroughput.SetFormatter(toPercent);
            db.Attributes.Add(OxyfernThroughput);

            OxyfernThroughputBaseValue = new AttributeModifier(
                attribute_id: OxyfernThroughput.Id,
                value: THROUGHPUT_BASE_VALUE);

            OxyfernThroughputFarmTinkerModifier = new AttributeModifier(
                attribute_id: OxyfernThroughput.Id,
                value: THROUGHPUT_MODIFIER_FARMTINKER,
                is_multiplier: true,
                is_readonly: false);
            effectFarmTinker.Add(OxyfernThroughputFarmTinkerModifier);

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

            // todo: добавить текстовку и отрегулировать
            ExtraSeedTendingChance = db.AttributeConverters.Create(
                id: nameof(ExtraSeedTendingChance),
                name: "Seed Tending Chance",
                description: DUPLICANTS.ATTRIBUTES.BOTANIST.BONUS_SEEDS,
                attribute: db.Attributes.Botanist,
                multiplier: EXTRA_SEED_CHANCE_PER_BOTANIST_SKILL,
                base_value: 0f,
                formatter: toPercent);

#if EXPANSION1
            // модификаторы для жучинкусов
            var effectDivergentCropTended = db.effects.Get(DIVERGENT_CROP_TENDED_EFFECT_ID);
            var effectWormCropTended = db.effects.Get(DIVERGENT_CROP_TENDED_WORM_EFFECT_ID);

            ColdBreatherThroughputDivergentModifier = new AttributeModifier(
                attribute_id: ColdBreatherThroughput.Id,
                value: THROUGHPUT_MODIFIER_DIVERGENT,
                is_multiplier: true,
                is_readonly: false);
            effectDivergentCropTended.Add(ColdBreatherThroughputDivergentModifier);

            ColdBreatherThroughputWormModifier = new AttributeModifier(
                attribute_id: ColdBreatherThroughput.Id,
                value: THROUGHPUT_MODIFIER_WORM,
                is_multiplier: true,
                is_readonly: false);
            effectWormCropTended.Add(ColdBreatherThroughputWormModifier);

            OxyfernThroughputDivergentModifier = new AttributeModifier(
                attribute_id: OxyfernThroughput.Id,
                value: THROUGHPUT_MODIFIER_DIVERGENT,
                is_multiplier: true,
                is_readonly: false);
            effectDivergentCropTended.Add(OxyfernThroughputDivergentModifier);

            OxyfernThroughputWormModifier = new AttributeModifier(
                attribute_id: OxyfernThroughput.Id,
                value: THROUGHPUT_MODIFIER_WORM,
                is_multiplier: true,
                is_readonly: false);
            effectWormCropTended.Add(OxyfernThroughputWormModifier);

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
#endif
        }

        internal static void LoadOptions()
        {
            BetterPlantTendingOptions.Reload();
            var options = BetterPlantTendingOptions.Instance;
            ColdBreatherThroughputFarmTinkerModifier.SetValue(options.ColdBreatherThroughputFarmTinkerModifier);
            OxyfernThroughputFarmTinkerModifier.SetValue(options.OxyfernThroughputFarmTinkerModifier);
            ExtraSeedChanceDecorativeBaseValue.SetValue(options.ExtraSeedChanceDecorativeBaseValue);
            ExtraSeedChanceNotDecorativeBaseValue.SetValue(options.ExtraSeedChanceNotDecorativeBaseValue);
            ExtraSeedTendingChance.multiplier = options.ExtraSeedTendingChance;
#if EXPANSION1
            ColdBreatherThroughputDivergentModifier.SetValue(options.ColdBreatherThroughputDivergentModifier);
            ColdBreatherThroughputWormModifier.SetValue(options.ColdBreatherThroughputWormModifier);
            OxyfernThroughputDivergentModifier.SetValue(options.OxyfernThroughputDivergentModifier);
            OxyfernThroughputWormModifier.SetValue(options.OxyfernThroughputWormModifier);
            ExtraSeedChanceDivergentModifier.SetValue(options.ExtraSeedChanceDivergentModifier);
            ExtraSeedChanceWormModifier.SetValue(options.ExtraSeedChanceWormModifier);
#endif
        }
    }
}
