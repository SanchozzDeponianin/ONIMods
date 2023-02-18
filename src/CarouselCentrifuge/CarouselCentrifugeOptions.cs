using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace CarouselCentrifuge
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    internal sealed class CarouselCentrifugeOptions : BaseOptions<CarouselCentrifugeOptions>
    {
        [JsonProperty]
        [Option(Format = "F1")]
        [Limit(1, 12)]
        public float SpecificEffectDuration { get; set; } = 4f;

        [JsonIgnore]
        public float TrackingEffectDuration => 0.5f;

        [JsonProperty]
        [Option]
        [Limit(1, 8)]
        public int MoraleBonus { get; set; } = 4;

        [JsonProperty]
        [Option(Format = "F1")]
        [Limit(0, 100)]
        public float DizzinessChancePercent { get; set; } = 1f;

        [JsonProperty]
        [Option]
        public bool EnableTraining { get; set; } = true;
    }
}
