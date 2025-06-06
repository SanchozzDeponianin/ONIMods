using TUNING;
using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace BuildableGeneShuffler
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2, BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER6)]
        public float constructionTime { get; set; } = BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER4;

        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2, BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER6)]
        public float manipulationTime { get; set; } = BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER5;
    }
}
