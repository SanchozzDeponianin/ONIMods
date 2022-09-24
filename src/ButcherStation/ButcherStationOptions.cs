using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace ButcherStation
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    [RestartRequired]
    public class ButcherStationOptions : BaseOptions<ButcherStationOptions>
    {
        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(ButcherStation.CREATURELIMIT, 1000)]
        public int max_creature_limit { get; set; } = ButcherStation.CREATURELIMIT;

        [JsonProperty]
        [Option(Format = "F2")]
        [Limit(0, 10)]
        public float extra_meat_per_ranching_attribute { get; set; } = ButcherStation.EXTRAMEATPERRANCHINGATTRIBUTE * 100f;

        [JsonProperty]
        [Option]
        public bool enable_not_count_babies { get; set; } = false;
    }
}
