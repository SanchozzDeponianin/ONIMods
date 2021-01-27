using Newtonsoft.Json;
using UnityEngine;

using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace BetterPlantTending
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Better Plant Tending", "https://steamcommunity.com/sharedfiles/filedetails/?id=1933433002")]
    [ConfigFile(IndentOutput: true)]
    //[RestartRequired]
    internal class BetterPlantTendingOptions : BaseOptions<BetterPlantTendingOptions>
    {
        [JsonIgnore]
        private float coldbreatherthroughputfarmtinkermodifier = BetterPlantTendingPatches.THROUGHPUT_MODIFIER_FARMTINKER;

        [JsonProperty]
        [Option("BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MULTIPLIER.TITLE", "BetterPlantTending.STRINGS.OPTIONS.COLDBREATHER_MULTIPLIER.TOOLTIP", Format = "F1")]
        [Limit(1, 4)]
        public float ColdBreatherThroughputFarmTinkerModifier { get => coldbreatherthroughputfarmtinkermodifier; set => coldbreatherthroughputfarmtinkermodifier = Mathf.Clamp(value, 1, 4); }

        [JsonIgnore]
        private float oxyfernthroughputfarmtinkermodifier = BetterPlantTendingPatches.THROUGHPUT_MODIFIER_FARMTINKER;

        [JsonProperty]
        [Option("BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MULTIPLIER.TITLE", "BetterPlantTending.STRINGS.OPTIONS.OXYFERN_MULTIPLIER.TOOLTIP", Format = "F1")]
        [Limit(1, 4)]
        public float OxyfernThroughputFarmTinkerModifier { get => oxyfernthroughputfarmtinkermodifier; set => oxyfernthroughputfarmtinkermodifier = Mathf.Clamp(value, 1, 4); }
    }
}
