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
        [Limit(4, 8)]
        public int analyze_cluster_radius { get; set; } = 5;

        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(0, 200)]
        public float efficiency_multiplier { get; set; } = 50;

        [JsonProperty]
        [Option]
        public bool add_glass { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool not_require_gas_pipe { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool prohibit_inside_rocket { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool fix_no_consume_power_bug { get; set; } = true;
    }
}
