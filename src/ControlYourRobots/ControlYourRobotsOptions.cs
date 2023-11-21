using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace ControlYourRobots
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ControlYourRobotsOptions : BaseOptions<ControlYourRobotsOptions>
    {
        [JsonProperty]
        [Option]
        public bool low_power_mode_enable { get; set; } = false;

        [JsonProperty]
        [Option]
        [Limit(0, 100)]
        public int low_power_mode_value { get; set; } = 15;
    }
}
