using Newtonsoft.Json;
using UnityEngine;

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
        // фермер
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

        // todo: причесать
        [JsonIgnore]
        private bool allowFarmTinkerDecorative = true;

        [JsonProperty]
        [Option("allowDecorative", "allowDecorative", "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER")]
        public bool AllowFarmTinkerDecorative => allowFarmTinkerDecorative;

        [JsonIgnore]
        private bool allowFarmTinkerGrownOrWilting = true;

        [JsonProperty]
        [Option("allowGrownOrWilting", "allowGrownOrWilting", "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER")]
        public bool AllowFarmTinkerGrownOrWilting => allowFarmTinkerGrownOrWilting;

#if EXPANSION1
        // жучара
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

        // червячара
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
#endif
    }
}
