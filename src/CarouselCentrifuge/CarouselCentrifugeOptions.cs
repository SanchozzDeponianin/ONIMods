using Newtonsoft.Json;
using UnityEngine;

using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace CarouselCentrifuge
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Carousel", "https://steamcommunity.com/sharedfiles/filedetails/?id=1899088142")]
    [ConfigFile(IndentOutput: true)]
    [RestartRequired]
    internal sealed class CarouselCentrifugeOptions : BaseOptions<CarouselCentrifugeOptions>
    {
        [JsonIgnore]
        private float specificeffectduration = 4f;

        [JsonProperty]
        [Option("CarouselCentrifuge.STRINGS.OPTIONS.EFFECTDURATION.TITLE", Format = "F1")]
        [Limit(1, 12)]
        public float SpecificEffectDuration { get => specificeffectduration; set => specificeffectduration = Mathf.Clamp(value, 1, 12); }

        [JsonIgnore]
        public float TrackingEffectDuration => 0.5f;

        [JsonIgnore]
        private int moralebonus = 4;

        [JsonProperty]
        [Option("CarouselCentrifuge.STRINGS.OPTIONS.MORALEBONUS.TITLE")]
        [Limit(1, 8)]
        public int MoraleBonus { get => moralebonus; set => moralebonus = Mathf.Clamp(value, 1, 8); }

        [JsonIgnore]
        private float dizzinesschancepercent = 2f;

        [JsonProperty]
        [Option("CarouselCentrifuge.STRINGS.OPTIONS.DIZZINESSCHANCE.TITLE", "CarouselCentrifuge.STRINGS.OPTIONS.DIZZINESSCHANCE.TOOLTIP", Format = "F1")]
        [Limit(0, 100)]
        public float DizzinessChancePercent { get => dizzinesschancepercent; set => dizzinesschancepercent = Mathf.Clamp(value, 0, 100); }
    }
}
