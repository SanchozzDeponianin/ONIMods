using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace ControlYourRobots
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
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

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.DLC3_ID)]
        public bool flydo_prefers_straight { get; set; } = false;

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.DLC3_ID)]
        public bool dead_flydo_returns_materials { get; set; } = false;

        [JsonProperty]
        [Option]
        public bool deconstruct_dead_biobot { get; set; } = true;

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.EXPANSION1_ID)]
        public bool deconstruct_dead_rover { get; set; } = false;
    }
}
