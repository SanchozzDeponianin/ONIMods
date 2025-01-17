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
        public bool zzz_icon_enable { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool low_power_mode_enable { get; set; } = false;

        [JsonProperty]
        [Option]
        [Limit(0, 100)]
        public int low_power_mode_value { get; set; } = 15;

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.DLC3_ID)]
        public bool flydo_can_pass_door { get; set; } = true;

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.DLC3_ID)]
        public bool restrict_flydo_by_default { get; set; } = true;

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.DLC3_ID)]
        public bool flydo_can_for_itself { get; set; } = true;
    }
}
