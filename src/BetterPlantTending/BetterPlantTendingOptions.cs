using Newtonsoft.Json;
using UnityEngine;

using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;
using static BetterPlantTending.BetterPlantTendingAttributes;

namespace BetterPlantTending
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Better Plant Tending", "https://steamcommunity.com/sharedfiles/filedetails/?id=1933433002")]
    [ConfigFile(IndentOutput: true)]
    internal class BetterPlantTendingOptions : BaseOptions<BetterPlantTendingOptions>
    {
        // фермер
        [JsonIgnore]
        private float coldbreatherthroughputfarmtinkermodifier = THROUGHPUT_MODIFIER_FARMTINKER;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER",
             Format = "F1")]
        [Limit(1, 4)]
        public float ColdBreatherThroughputFarmTinkerModifier { get => coldbreatherthroughputfarmtinkermodifier; set => coldbreatherthroughputfarmtinkermodifier = Mathf.Clamp(value, 1, 4); }

        [JsonIgnore]
        private float oxyfernthroughputfarmtinkermodifier = THROUGHPUT_MODIFIER_FARMTINKER;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.FARMTINKER",
            Format = "F1")]
        [Limit(1, 4)]
        public float OxyfernThroughputFarmTinkerModifier { get => oxyfernthroughputfarmtinkermodifier; set => oxyfernthroughputfarmtinkermodifier = Mathf.Clamp(value, 1, 4); }

#if EXPANSION1
        // жучара
        [JsonIgnore]
        private float coldbreatherthroughputdivergentmodifier = THROUGHPUT_MODIFIER_DIVERGENT;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.DIVERGENTCROPTENDED",
            Format = "F1")]
        [Limit(0.05, 0.5)]
        public float ColdBreatherThroughputDivergentModifier { get => coldbreatherthroughputdivergentmodifier; set => coldbreatherthroughputdivergentmodifier = Mathf.Clamp(value, 0.05f, 0.5f); }

        [JsonIgnore]
        private float oxyfernthroughputdivergentmodifier = THROUGHPUT_MODIFIER_DIVERGENT;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.DIVERGENTCROPTENDED",
            Format = "F2")]
        [Limit(0.05, 0.5)]
        public float OxyfernThroughputDivergentModifier { get => oxyfernthroughputdivergentmodifier; set => oxyfernthroughputdivergentmodifier = Mathf.Clamp(value, 0.05f, 0.5f); }

        // червячара
        [JsonIgnore]
        private float coldbreatherthroughputwormmodifier = THROUGHPUT_MODIFIER_WORM;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.WORMCROPTENDED",
            Format = "F1")]
        [Limit(0.5, 2)]
        public float ColdBreatherThroughputWormModifier { get => coldbreatherthroughputwormmodifier; set => coldbreatherthroughputwormmodifier = Mathf.Clamp(value, 0.5f, 2); }

        [JsonIgnore]
        private float oxyfernthroughputwormmodifier = THROUGHPUT_MODIFIER_WORM;

        [JsonProperty]
        [Option(
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TITLE",
            "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MODIFIER.TOOLTIP",
            "BetterPlantTending.STRINGS.OPTIONS.CATEGORY.WORMCROPTENDED",
            Format = "F1")]
        [Limit(0.5, 2)]
        public float OxyfernThroughputWormModifier { get => oxyfernthroughputwormmodifier; set => oxyfernthroughputwormmodifier = Mathf.Clamp(value, 0.5f, 2); }
#endif
    }
}
