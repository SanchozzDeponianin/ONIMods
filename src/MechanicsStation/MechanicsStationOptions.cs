using TUNING;
using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;
using static MechanicsStation.MechanicsStationAssets;

namespace MechanicsStation
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    internal class MechanicsStationOptions : BaseOptions<MechanicsStationOptions>
    {
        [JsonProperty]
        [Option("MechanicsStation.STRINGS.OPTIONS.MACHINERY_SPEED_MODIFIER.TITLE", "MechanicsStation.STRINGS.OPTIONS.MACHINERY_SPEED_MODIFIER.TOOLTIP", Format = "F0")]
        [Limit(20, 100)]
        public float MachinerySpeedModifier { get; set; } = MACHINERY_SPEED_MODIFIER * 100;

        [JsonProperty]
        [Option("MechanicsStation.STRINGS.OPTIONS.CRAFTING_SPEED_MODIFIER.TITLE", "MechanicsStation.STRINGS.OPTIONS.CRAFTING_SPEED_MODIFIER.TOOLTIP", Format = "F0")]
        [Limit(50, 200)]
        public float CraftingSpeedModifier { get; set; } = CRAFTING_SPEED_MODIFIER * 100;

        [JsonProperty]
        [Option("MechanicsStation.STRINGS.OPTIONS.MACHINE_TINKER_EFFECT_DURATION.TITLE", Format = "F1")]
        [Limit(1, 3)]
        public float MachineTinkerEffectDuration { get; set; } = MACHINE_TINKER_EFFECT_DURATION;

        [JsonProperty]
        [Option("MechanicsStation.STRINGS.OPTIONS.MACHINE_TINKER_EFFECT_DURATION_PER_SKILL.TITLE", Format = "F1")]
        [Limit(2.5, 10)]

        public float MachineTinkerEffectDurationPerSkill { get; set; } = MACHINE_TINKER_EFFECT_DURATION_PER_SKILL * 100;

        [JsonProperty]
        [Option("MechanicsStation.STRINGS.OPTIONS.MACHINE_TINKERABLE_WORKTIME.TITLE", Format = "F0")]
        [Limit(BUILDINGS.WORK_TIME_SECONDS.SHORT_WORK_TIME, BUILDINGS.WORK_TIME_SECONDS.VERY_LONG_WORK_TIME)]
        public float MachineTinkerableWorkTime { get; set; } = MACHINE_TINKERABLE_WORKTIME;
    }
}
