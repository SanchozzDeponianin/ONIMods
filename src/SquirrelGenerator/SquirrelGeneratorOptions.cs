using TUNING;
using Newtonsoft.Json;
using UnityEngine;

using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace SquirrelGenerator
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    [RestartRequired]
    internal class SquirrelGeneratorOptions : BaseOptions<SquirrelGeneratorOptions>
    {
        [JsonIgnore]
        private int generatorwattagerating = 250;
        [JsonProperty]
        [Option("SquirrelGenerator.STRINGS.OPTIONS.GENERATORWATTAGE.TITLE", "SquirrelGenerator.STRINGS.OPTIONS.GENERATORWATTAGE.TOOLTIP")]
        [Limit(50, 500)]
        public int GeneratorWattageRating { get => generatorwattagerating; set => generatorwattagerating = Mathf.Clamp(value, 50, 500); }

        [JsonIgnore]
        private int selfheatwatts = (int)(BUILDINGS.SELF_HEAT_KILOWATTS.TIER2 * Constants.KW2DTU_S);
        [JsonProperty]
        [Option("SquirrelGenerator.STRINGS.OPTIONS.SELFHEAT.TITLE", "SquirrelGenerator.STRINGS.OPTIONS.SELFHEAT.TOOLTIP", Format = "F0")]
        [Limit(BUILDINGS.SELF_HEAT_KILOWATTS.TIER1 * Constants.KW2DTU_S, BUILDINGS.SELF_HEAT_KILOWATTS.TIER3 * Constants.KW2DTU_S)]
        public int SelfHeatWatts { get => selfheatwatts; set => selfheatwatts = Mathf.Clamp(value, (int)(BUILDINGS.SELF_HEAT_KILOWATTS.TIER1 * Constants.KW2DTU_S), (int)(BUILDINGS.SELF_HEAT_KILOWATTS.TIER3 * Constants.KW2DTU_S)); }

        [JsonIgnore]
        private int happinessbonus = WheelRunningStates.HAPPINESS_BONUS;
        [JsonProperty]
        [Option("SquirrelGenerator.STRINGS.OPTIONS.HAPPINESSBONUS.TITLE", "SquirrelGenerator.STRINGS.OPTIONS.HAPPINESSBONUS.TOOLTIP")]
        [Limit(0, 5)]
        public int HappinessBonus { get => happinessbonus; set => happinessbonus = Mathf.Clamp(value, 0, 5); }

        [JsonIgnore]
        private int metabolismbonus = WheelRunningStates.METABOLISM_BONUS;
        [JsonProperty]
        [Option("SquirrelGenerator.STRINGS.OPTIONS.METABOLISMBONUS.TITLE", "SquirrelGenerator.STRINGS.OPTIONS.METABOLISMBONUS.TOOLTIP")]
        [Limit(25, 500)]
        public int MetabolismBonus { get => metabolismbonus; set => metabolismbonus = Mathf.Clamp(value, 25, 500); }

        [JsonIgnore]
        private int searchwheelradius = WheelRunningMonitor.SEARCHWHEELRADIUS;
        [JsonProperty]
        [Option("SquirrelGenerator.STRINGS.OPTIONS.SEARCHWHEELRADIUS.TITLE", "SquirrelGenerator.STRINGS.OPTIONS.SEARCHWHEELRADIUS.TOOLTIP")]
        [Limit(10, 50)]
        public int SearchWheelRadius { get => searchwheelradius; set => searchwheelradius = Mathf.Clamp(value, 10, 50); }

        [JsonIgnore]
        private int searchmininterval = WheelRunningMonitor.SEARCHMININTERVAL;
        [JsonProperty]
        [Option("SquirrelGenerator.STRINGS.OPTIONS.SEARCHMININTERVAL.TITLE", "SquirrelGenerator.STRINGS.OPTIONS.SEARCHMININTERVAL.TOOLTIP")]
        [Limit(10, 600)]
        public int SearchMinInterval { get => searchmininterval; set => searchmininterval = Mathf.Clamp(value, 10, 600); }

        [JsonIgnore]
        private int searchmaxinterval = WheelRunningMonitor.SEARCHMAXINTERVAL;
        [JsonProperty]
        [Option("SquirrelGenerator.STRINGS.OPTIONS.SEARCHMAXINTERVAL.TITLE", "SquirrelGenerator.STRINGS.OPTIONS.SEARCHMININTERVAL.TOOLTIP")]
        [Limit(10, 600)]
        public int SearchMaxInterval { get => searchmaxinterval; set => searchmaxinterval = Mathf.Clamp(value, searchmininterval, 600); }
    }
}
