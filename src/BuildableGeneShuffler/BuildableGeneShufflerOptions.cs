using TUNING;
using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace BuildableGeneShuffler
{
    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    public class BuildableGeneShufflerOptions : BaseOptions<BuildableGeneShufflerOptions>
    {
        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER3, BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER6)]
        public float constructionTime { get; set; } = BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER5;
    }
}
