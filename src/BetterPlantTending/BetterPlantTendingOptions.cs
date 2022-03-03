using Newtonsoft.Json;
using TUNING;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;
using static BetterPlantTending.BetterPlantTendingAssets;

namespace BetterPlantTending
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Better Plant Tending", "https://steamcommunity.com/sharedfiles/filedetails/?id=1933433002")]
    [ConfigFile(IndentOutput: true)]
    internal class BetterPlantTendingOptions : BaseOptions<BetterPlantTendingOptions>
    {
        // основные настройки
        // todo: причесать
        [JsonProperty]
        [Option(
            "allowDecorative",
            "allowDecorative",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.GENEGAL")]
        public bool AllowFarmTinkerDecorative { get; set; } = true;

        [JsonProperty]
        [Option(
            "allowGrownOrWilting",
            "allowGrownOrWilting",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.GENEGAL")]
        public bool PreventTendingGrownOrWilting { get; set; } = true;

        // шансы доп семян
        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS..TITLE",
            "BetterPlantTending.STRINGS.OPTIONS..TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.GENEGAL",
            Format = "F2")]
        [Limit(0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
        public float ExtraSeedChanceDecorativeBaseValue { get; set; } = EXTRA_SEED_CHANCE_BASE_VALUE_DECORATIVE;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS..TITLE",
            "BetterPlantTending.STRINGS.OPTIONS..TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.GENEGAL",
            Format = "F2")]
        [Limit(0, CROPS.BASE_BONUS_SEED_PROBABILITY)]
        public float ExtraSeedChanceNotDecorativeBaseValue { get; set; } = EXTRA_SEED_CHANCE_BASE_VALUE_NOT_DECORATIVE;

        // эффекты фермера
        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER",
             Format = "F1")]
        [Limit(1, 4)]
        public float ColdBreatherThroughputFarmTinkerModifier { get; set; } = THROUGHPUT_MODIFIER_FARMTINKER;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER",
            Format = "F1")]
        [Limit(1, 4)]
        public float OxyfernThroughputFarmTinkerModifier { get; set; } = THROUGHPUT_MODIFIER_FARMTINKER;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS..TITLE",
            "BetterPlantTending.STRINGS.OPTIONS..TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER",
            Format = "F3")]
        [Limit(0.005, 0.04)]
        public float ExtraSeedTendingChance { get; set; } = EXTRA_SEED_CHANCE_PER_BOTANIST_SKILL;

        // эффекты жучары
        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.DIVERGENTCROPTENDED",
            Format = "F1")]
        [Limit(0.05, 0.5)]
        public float ColdBreatherThroughputDivergentModifier { get; set; } = THROUGHPUT_MODIFIER_DIVERGENT;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.DIVERGENTCROPTENDED",
            Format = "F2")]
        [Limit(0.05, 0.5)]
        public float OxyfernThroughputDivergentModifier { get; set; } = THROUGHPUT_MODIFIER_DIVERGENT;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS..TITLE",
            "BetterPlantTending.STRINGS.OPTIONS..TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.DIVERGENTCROPTENDED",
            Format = "F2")]
        [Limit(0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
        public float ExtraSeedChanceDivergentModifier { get; set; } = EXTRA_SEED_CHANCE_MODIFIER_DIVERGENT;

        // эффекты червячары
        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.WORMCROPTENDED",
            Format = "F1")]
        [Limit(0.5, 2)]
        public float ColdBreatherThroughputWormModifier { get; set; } = THROUGHPUT_MODIFIER_WORM;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.WORMCROPTENDED",
            Format = "F1")]
        [Limit(0.5, 2)]
        public float OxyfernThroughputWormModifier { get; set; } = THROUGHPUT_MODIFIER_WORM;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS..TITLE",
            "BetterPlantTending.STRINGS.OPTIONS..TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.WORMCROPTENDED",
            Format = "F2")]
        [Limit(0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
        public float ExtraSeedChanceWormModifier { get; set; } = EXTRA_SEED_CHANCE_MODIFIER_WORM;
    }
}
