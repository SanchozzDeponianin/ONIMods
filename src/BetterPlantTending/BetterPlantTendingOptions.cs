using Newtonsoft.Json;
using UnityEngine;
using TUNING;

using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;
using static BetterPlantTending.BetterPlantTendingAssets;

namespace BetterPlantTending
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Better Plant Tending", "https://steamcommunity.com/sharedfiles/filedetails/?id=1933433002")]
    [ConfigFile(IndentOutput: true)]
    internal class BetterPlantTendingOptions : BaseOptions<BetterPlantTendingOptions>
    {
        // эффекты и настройки фермера
        // todo: причесать
        [JsonProperty]
        [Option(
            "allowDecorative", 
            "allowDecorative", 
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER")]
        public bool AllowFarmTinkerDecorative { get; set; }

        [JsonProperty]
        [Option(
            "allowGrownOrWilting", 
            "allowGrownOrWilting", 
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER")]
        public bool AllowFarmTinkerGrownOrWilting { get; set; }

        [JsonIgnore]
        private float coldBreatherThroughputFarmTinkerModifier = THROUGHPUT_MODIFIER_FARMTINKER;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER",
             Format = "F1")]
        [Limit(1, 4)]
        public float ColdBreatherThroughputFarmTinkerModifier { get => coldBreatherThroughputFarmTinkerModifier; set => coldBreatherThroughputFarmTinkerModifier = Mathf.Clamp(value, 1, 4); }

        [JsonIgnore]
        private float oxyfernThroughputFarmTinkerModifier = THROUGHPUT_MODIFIER_FARMTINKER;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER",
            Format = "F1")]
        [Limit(1, 4)]
        public float OxyfernThroughputFarmTinkerModifier { get => oxyfernThroughputFarmTinkerModifier; set => oxyfernThroughputFarmTinkerModifier = Mathf.Clamp(value, 1, 4); }

        // шансы доп семян
        [JsonIgnore]
        private float extraSeedChanceDecorativeBaseValue = EXTRA_SEED_CHANCE_BASE_VALUE_DECORATIVE;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS..TITLE",
            "BetterPlantTending.STRINGS.OPTIONS..TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER",
            Format = "F2")]
        [Limit(0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
        public float ExtraSeedChanceDecorativeBaseValue { get => extraSeedChanceDecorativeBaseValue; set => extraSeedChanceDecorativeBaseValue = Mathf.Clamp(value, 0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY); }

        [JsonIgnore]
        private float extraSeedChanceNotDecorativeBaseValue = EXTRA_SEED_CHANCE_BASE_VALUE_NOT_DECORATIVE;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS..TITLE",
            "BetterPlantTending.STRINGS.OPTIONS..TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER",
            Format = "F2")]
        [Limit(0, CROPS.BASE_BONUS_SEED_PROBABILITY)]
        public float ExtraSeedChanceNotDecorativeBaseValue { get => extraSeedChanceNotDecorativeBaseValue; set => extraSeedChanceNotDecorativeBaseValue = Mathf.Clamp(value, 0, CROPS.BASE_BONUS_SEED_PROBABILITY); }

        [JsonIgnore]
        private float extraSeedTendingChance = EXTRA_SEED_CHANCE_PER_BOTANIST_SKILL;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS..TITLE",
            "BetterPlantTending.STRINGS.OPTIONS..TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER",
            Format = "F3")]
        [Limit(0.005, 0.04)]
        public float ExtraSeedTendingChance { get => extraSeedTendingChance; set => extraSeedTendingChance = Mathf.Clamp(value, 0.005f, 0.04f); }

#if EXPANSION1
        // эффекты жучары
        [JsonIgnore]
        private float coldBreatherThroughputDivergentModifier = THROUGHPUT_MODIFIER_DIVERGENT;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.DIVERGENTCROPTENDED",
            Format = "F1")]
        [Limit(0.05, 0.5)]
        public float ColdBreatherThroughputDivergentModifier { get => coldBreatherThroughputDivergentModifier; set => coldBreatherThroughputDivergentModifier = Mathf.Clamp(value, 0.05f, 0.5f); }

        [JsonIgnore]
        private float oxyfernThroughputDivergentModifier = THROUGHPUT_MODIFIER_DIVERGENT;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.DIVERGENTCROPTENDED",
            Format = "F2")]
        [Limit(0.05, 0.5)]
        public float OxyfernThroughputDivergentModifier { get => oxyfernThroughputDivergentModifier; set => oxyfernThroughputDivergentModifier = Mathf.Clamp(value, 0.05f, 0.5f); }

        [JsonIgnore]
        private float extraSeedChanceDivergentModifier = EXTRA_SEED_CHANCE_MODIFIER_DIVERGENT;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS..TITLE",
            "BetterPlantTending.STRINGS.OPTIONS..TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.DIVERGENTCROPTENDED",
            Format = "F2")]
        [Limit(0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
        public float ExtraSeedChanceDivergentModifier { get => extraSeedChanceDivergentModifier; set => extraSeedChanceDivergentModifier = Mathf.Clamp(value, 0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY); }

        // эффекты червячары
        [JsonIgnore]
        private float coldBreatherThroughputWormModifier = THROUGHPUT_MODIFIER_WORM;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.WORMCROPTENDED",
            Format = "F1")]
        [Limit(0.5, 2)]
        public float ColdBreatherThroughputWormModifier { get => coldBreatherThroughputWormModifier; set => coldBreatherThroughputWormModifier = Mathf.Clamp(value, 0.5f, 2); }

        [JsonIgnore]
        private float oxyfernThroughputWormModifier = THROUGHPUT_MODIFIER_WORM;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.WORMCROPTENDED",
            Format = "F1")]
        [Limit(0.5, 2)]
        public float OxyfernThroughputWormModifier { get => oxyfernThroughputWormModifier; set => oxyfernThroughputWormModifier = Mathf.Clamp(value, 0.5f, 2); }

        [JsonIgnore]
        private float extraSeedChanceWormModifier = EXTRA_SEED_CHANCE_MODIFIER_WORM;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS..TITLE",
            "BetterPlantTending.STRINGS.OPTIONS..TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.WORMCROPTENDED",
            Format = "F2")]
        [Limit(0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
        public float ExtraSeedChanceWormModifier { get => extraSeedChanceWormModifier; set => extraSeedChanceWormModifier = Mathf.Clamp(value, 0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY); }
#endif
        public BetterPlantTendingOptions()
        {
            AllowFarmTinkerDecorative = true;
            AllowFarmTinkerGrownOrWilting = false;
        }
    }
}
