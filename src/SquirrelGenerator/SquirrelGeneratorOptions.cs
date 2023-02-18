using TUNING;
using Newtonsoft.Json;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace SquirrelGenerator
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class SquirrelGeneratorOptions : BaseOptions<SquirrelGeneratorOptions>
    {
        [JsonProperty]
        [Option]
        [Limit(50, 500)]
        public int GeneratorWattageRating { get; set; } = 250;

        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(BUILDINGS.SELF_HEAT_KILOWATTS.TIER1 * Constants.KW2DTU_S, BUILDINGS.SELF_HEAT_KILOWATTS.TIER3 * Constants.KW2DTU_S)]
        public int SelfHeatWatts { get; set; } = (int)(BUILDINGS.SELF_HEAT_KILOWATTS.TIER2 * Constants.KW2DTU_S);

        [JsonProperty]
        [Option]
        [Limit(0, 5)]
        public int HappinessBonus { get; set; } = WheelRunningStates.HAPPINESS_BONUS;

        [JsonProperty]
        [Option]
        [Limit(25, 500)]
        public int MetabolismBonus { get; set; } = WheelRunningStates.METABOLISM_BONUS;

        [JsonProperty]
        [Option]
        [Limit(10, 50)]
        public int SearchWheelRadius { get; set; } = WheelRunningMonitor.SEARCH_WHEEL_RADIUS;

        [JsonProperty]
        [Option]
        [Limit(10, 600)]
        public int SearchMinInterval { get; set; } = WheelRunningMonitor.SEARCH_MIN_INTERVAL;

        [JsonIgnore]
        private int searchmaxinterval = WheelRunningMonitor.SEARCH_MAX_INTERVAL;

        [JsonProperty]
        [Option]
        [Limit(10, 600)]
        public int SearchMaxInterval { get => searchmaxinterval; set => searchmaxinterval = Mathf.Max(value, SearchMinInterval); }
    }
}
