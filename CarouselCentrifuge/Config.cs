using System;
using UnityEngine;
using Newtonsoft.Json;
using SanchozzONIMods.Lib;

namespace CarouselCentrifuge
{
    internal class Config : BaseConfig<Config>
    {
        [JsonIgnore]
        private float specificeffectduration = 3.95f;
        public float SpecificEffectDuration { get => specificeffectduration; set => specificeffectduration = Mathf.Max(value, 1); }

        [JsonIgnore]
        private int moralebonus = 4;
        public int MoraleBonus { get => moralebonus; set => moralebonus = Mathf.Max(value, 2); }

        [JsonIgnore]
        private float dizzinesschancepercent = 5f;
        public float DizzinessChancePercent { get => dizzinesschancepercent; set => dizzinesschancepercent = Mathf.Clamp(value, 0, 100); }

        [JsonIgnore]
        public float TrackingEffectDuration => Math.Max(0.5f, SpecificEffectDuration - 1.5f);
    }
}
