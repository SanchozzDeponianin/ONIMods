using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace LargeTelescope
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class LargeTelescopeOptions : BaseOptions<LargeTelescopeOptions>
    {
        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.EXPANSION1_ID)]
        [Limit(4, 8)]
        public int analyze_cluster_radius { get; set; } = 5;

        [JsonProperty]
        [Option(Format = "F0")]
        [RequireDLC(DlcManager.EXPANSION1_ID)]
        [Limit(0, 200)]
        public float efficiency_multiplier { get; set; } = 50;

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.EXPANSION1_ID)]
        public bool add_glass { get; set; } = true;

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.VANILLA_ID)]
        [RequireDLC(DlcManager.EXPANSION1_ID,false)]
        public bool vanilla_add_glass { get; set; } = true;

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.EXPANSION1_ID)]
        public bool prohibit_inside_rocket { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool not_require_gas_pipe { get; set; } = true;        
    }
}
