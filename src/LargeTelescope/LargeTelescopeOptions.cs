using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace LargeTelescope
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    [RestartRequired]
    internal sealed class LargeTelescopeOptions : BaseOptions<LargeTelescopeOptions>
    {
        [JsonProperty]
        [Option]
        [Limit(4, 10)]
        public int AnalyzeClusterRadius { get; set; } = 6;

        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(0, 200)]
        public float EfficiencyMultiplier { get; set; } = 50;

        [JsonProperty]
        [Option]
        public bool FixNoConsumePowerBug { get; set; } = true;
    }
}
