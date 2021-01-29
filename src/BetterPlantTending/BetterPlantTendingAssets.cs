using Klei.AI;

namespace BetterPlantTending
{
    internal static class BetterPlantTendingAssets
    {
        public const string FARM_TINKER_EFFECT_ID = "FarmTinker";
        public const string DIVERGENT_CROP_TENDED_EFFECT_ID = "DivergentCropTended";
        public const string DIVERGENT_CROP_TENDED_WORM_EFFECT_ID = "DivergentCropTendedWorm";

        internal const float THROUGHPUT_BASE_VALUE = 1;
        internal const float THROUGHPUT_MODIFIER_FARMTINKER = 2;
#if EXPANSION1
        internal const float THROUGHPUT_MODIFIER_DIVERGENT = 0.2f;
        internal const float THROUGHPUT_MODIFIER_WORM = 1;
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

        internal static void Init()
        {
            var db = Db.Get();
            var effectFarmTinker = db.effects.Get(FARM_TINKER_EFFECT_ID);

            ColdBreatherThroughput = new Attribute(
                id: nameof(ColdBreatherThroughput),
                is_trainable: false,
                show_in_ui: Attribute.Display.General,
                is_profession: false,
                base_value: 0);
            ColdBreatherThroughput.SetFormatter(new PercentAttributeFormatter());
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
            OxyfernThroughput.SetFormatter(new PercentAttributeFormatter());
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
#endif
        }

        internal static void LoadOptions()
        {
            BetterPlantTendingOptions.Reload();
            ColdBreatherThroughputFarmTinkerModifier.SetValue(BetterPlantTendingOptions.Instance.ColdBreatherThroughputFarmTinkerModifier);
            OxyfernThroughputFarmTinkerModifier.SetValue(BetterPlantTendingOptions.Instance.OxyfernThroughputFarmTinkerModifier);
#if EXPANSION1
            ColdBreatherThroughputDivergentModifier.SetValue(BetterPlantTendingOptions.Instance.ColdBreatherThroughputDivergentModifier);
            OxyfernThroughputDivergentModifier.SetValue(BetterPlantTendingOptions.Instance.OxyfernThroughputDivergentModifier);
            ColdBreatherThroughputWormModifier.SetValue(BetterPlantTendingOptions.Instance.ColdBreatherThroughputWormModifier);
            OxyfernThroughputWormModifier.SetValue(BetterPlantTendingOptions.Instance.OxyfernThroughputWormModifier);
#endif
        }
    }
}
