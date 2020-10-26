using UnityEngine;
using Newtonsoft.Json;
using SanchozzONIMods.Lib;

namespace SquirrelGenerator
{
    internal class Config : BaseConfig<Config>
    {
        [JsonIgnore]
        private int generatorwattagerating = 400;
        public int GeneratorWattageRating { get => generatorwattagerating; set => generatorwattagerating = Mathf.Clamp(value, 50, 500); }

        [JsonIgnore]
        private int searchwheelradius = WheelRunningMonitor.SEARCHWHEELRADIUS;
        public int SearchWheelRadius { get => searchwheelradius; set => searchwheelradius = Mathf.Clamp(value, 10, 50); }

        [JsonIgnore]
        private int searchmininterval = WheelRunningMonitor.SEARCHMININTERVAL;
        public int SearchMinInterval { get => searchmininterval; set => searchmininterval = Mathf.Clamp(value, 10, 600); }

        [JsonIgnore]
        private int searchmaxinterval = WheelRunningMonitor.SEARCHMAXINTERVAL;
        public int SearchMaxInterval { get => searchmaxinterval; set => searchmaxinterval = Mathf.Clamp(value, searchmininterval, 600); }

        [JsonIgnore]
        private int happinessbonus = WheelRunningStates.HAPPINESS_BONUS;
        public int HappinessBonus { get => happinessbonus; set => happinessbonus = Mathf.Clamp(value, 0, 5); }

        [JsonIgnore]
        private int metabolismbonus = WheelRunningStates.METABOLISM_BONUS;
        public int MetabolismBonus { get => metabolismbonus; set => metabolismbonus = Mathf.Clamp(value, 25, 500); }
    }
}
