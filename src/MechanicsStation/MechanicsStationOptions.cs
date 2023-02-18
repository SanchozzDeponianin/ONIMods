using TUNING;
using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;
using static MechanicsStation.MechanicsStationAssets;

namespace MechanicsStation
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    internal sealed class MechanicsStationOptions : BaseOptions<MechanicsStationOptions>
    {
        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(20, 100)]
        public float machinery_speed_modifier { get; set; } = MACHINERY_SPEED_MODIFIER * 100;

        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(50, 200)]
        public float crafting_speed_modifier { get; set; } = CRAFTING_SPEED_MODIFIER * 100;

        [JsonProperty]
        [Option(Format = "F1")]
        [Limit(1, 3)]
        public float machine_tinker_effect_duration { get; set; } = MACHINE_TINKER_EFFECT_DURATION;

        [JsonProperty]
        [Option(Format = "F1")]
        [Limit(2.5, 10)]

        public float machine_tinker_effect_duration_per_skill { get; set; } = MACHINE_TINKER_EFFECT_DURATION_PER_SKILL * 100;

        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(BUILDINGS.WORK_TIME_SECONDS.SHORT_WORK_TIME, BUILDINGS.WORK_TIME_SECONDS.VERY_LONG_WORK_TIME)]
        public float machine_tinkerable_worktime { get; set; } = MACHINE_TINKERABLE_WORKTIME;
    }
}
