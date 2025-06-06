using TUNING;
using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;
using static MechanicsStation.ModAssets;

namespace MechanicsStation
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    internal sealed class ModOptions : BaseOptions<ModOptions>
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
        [Option]
        public bool machine_tinker_freeze_effect_duration { get; set; } = true;
    }
}
