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
        private float coldbreatherthroughputmultiplier = BetterPlantTendingPatches.THROUGHPUT_MULTIPLIER;

        [JsonProperty]
        [Option("MoreTinkerablePlants.STRINGS.OPTIONS.COLDBREATHER_MULTIPLIER.TITLE", "MoreTinkerablePlants.STRINGS.OPTIONS.COLDBREATHER_MULTIPLIER.TOOLTIP", Format = "F1")]
        [Limit(2, 5)]
        public float ColdBreatherThroughputMultiplier { get => coldbreatherthroughputmultiplier; set => coldbreatherthroughputmultiplier = Mathf.Clamp(value, 2, 5); }

        [JsonIgnore]
        private float oxyfernthroughputmultiplier = BetterPlantTendingPatches.THROUGHPUT_MULTIPLIER;

        [JsonProperty]
        [Option("MoreTinkerablePlants.STRINGS.OPTIONS.OXYFERN_MULTIPLIER.TITLE", "MoreTinkerablePlants.STRINGS.OPTIONS.OXYFERN_MULTIPLIER.TOOLTIP", Format = "F1")]
        [Limit(2, 5)]
        public float OxyfernThroughputMultiplier { get => oxyfernthroughputmultiplier; set => oxyfernthroughputmultiplier = Mathf.Clamp(value, 2, 5); }
    }
}
