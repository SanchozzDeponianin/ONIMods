using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace VirtualPlanetarium
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    internal sealed class VirtualPlanetariumOptions : BaseOptions<VirtualPlanetariumOptions>
    {
        [JsonProperty]
        [Option]
        [Limit(5, 30)]
        public int StressDelta { get; set; } = 15;

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
    }
}
