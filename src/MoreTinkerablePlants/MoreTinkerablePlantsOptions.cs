using Newtonsoft.Json;
using UnityEngine;

using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace MoreTinkerablePlants
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Farmer's Touch on more Plants", "https://steamcommunity.com/sharedfiles/filedetails/?id=1933433002")]
    [ConfigFile(IndentOutput: true)]
    //[RestartRequired]
    internal class MoreTinkerablePlantsOptions : BaseOptions<MoreTinkerablePlantsOptions>
    {
        [JsonIgnore]
        private float coldbreatherthroughputmultiplier = MoreTinkerablePlantsPatches.THROUGHPUT_MULTIPLIER;

        [JsonProperty]
        [Option("MoreTinkerablePlants.STRINGS.OPTIONS.COLDBREATHER_MULTIPLIER.TITLE", "MoreTinkerablePlants.STRINGS.OPTIONS.COLDBREATHER_MULTIPLIER.TOOLTIP", Format = "F1")]
        [Limit(2, 5)]
        public float ColdBreatherThroughputMultiplier { get => coldbreatherthroughputmultiplier; set => coldbreatherthroughputmultiplier = Mathf.Clamp(value, 2, 5); }

        [JsonIgnore]
        private float oxyfernthroughputmultiplier = MoreTinkerablePlantsPatches.THROUGHPUT_MULTIPLIER;

        [JsonProperty]
        [Option("MoreTinkerablePlants.STRINGS.OPTIONS.OXYFERN_MULTIPLIER.TITLE", "MoreTinkerablePlants.STRINGS.OPTIONS.OXYFERN_MULTIPLIER.TOOLTIP", Format = "F1")]
        [Limit(2, 5)]
        public float OxyfernThroughputMultiplier { get => oxyfernthroughputmultiplier; set => oxyfernthroughputmultiplier = Mathf.Clamp(value, 2, 5); }
    }
}
