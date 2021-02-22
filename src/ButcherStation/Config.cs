using UnityEngine;
using Newtonsoft.Json;
using SanchozzONIMods.Lib;

namespace ButcherStation
{
    public class Config : BaseConfig<Config>
    {
        [JsonIgnore]
        private int maxcreaturelimit = ButcherStation.CREATURELIMIT;
        public int MAXCREATURELIMIT { get => maxcreaturelimit; set => maxcreaturelimit = Mathf.Clamp(value, ButcherStation.CREATURELIMIT, 1000); }

        [JsonIgnore]
        private float extrameatperranchingattribute = ButcherStation.EXTRAMEATPERRANCHINGATTRIBUTE;
        public float EXTRAMEATPERRANCHINGATTRIBUTE { get => extrameatperranchingattribute; set => extrameatperranchingattribute = Mathf.Clamp01(value); }
    }
}
