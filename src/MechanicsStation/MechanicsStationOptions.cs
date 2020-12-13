using TUNING;
using Newtonsoft.Json;
using UnityEngine;

using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace MechanicsStation
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Mechanics Station", "https://steamcommunity.com/sharedfiles/filedetails/?id=1938117526")]
    [ConfigFile(IndentOutput: true)]
    internal class MechanicsStationOptions : BaseOptions<MechanicsStationOptions>
    {
        [JsonIgnore]
        private float machineryspeedmodifier = MechanicsStationPatches.MACHINERY_SPEED_MODIFIER * 100;

        [JsonProperty]
        [Option("MechanicsStation.STRINGS.OPTIONS.MACHINERY_SPEED_MODIFIER.TITLE", "MechanicsStation.STRINGS.OPTIONS.MACHINERY_SPEED_MODIFIER.TOOLTIP", Format = "F0")]
        [Limit(20, 100)]
        public float MachinerySpeedModifier { get => machineryspeedmodifier; set => machineryspeedmodifier = Mathf.Clamp(value, 20, 100); }

        [JsonIgnore]
        private float craftingspeedmodifier = MechanicsStationPatches.CRAFTING_SPEED_MODIFIER * 100;

        [JsonProperty]
        [Option("MechanicsStation.STRINGS.OPTIONS.CRAFTING_SPEED_MODIFIER.TITLE", "MechanicsStation.STRINGS.OPTIONS.CRAFTING_SPEED_MODIFIER.TOOLTIP", Format = "F0")]
        [Limit(50, 200)]
        public float CraftingSpeedModifier { get => craftingspeedmodifier; set => craftingspeedmodifier = Mathf.Clamp(value, 50, 200); }

        [JsonIgnore]
        private float machinetinkereffectduration = MechanicsStationPatches.MACHINE_TINKER_EFFECT_DURATION;

        [JsonProperty]
        [Option("MechanicsStation.STRINGS.OPTIONS.MACHINE_TINKER_EFFECT_DURATION.TITLE", Format = "F1")]
        [Limit(1, 3)]
        public float MachineTinkerEffectDuration { get => machinetinkereffectduration; set => machinetinkereffectduration = Mathf.Clamp(value, 1, 3); }

        [JsonIgnore]
        private float machinetinkerableworktime = MechanicsStationPatches.MACHINE_TINKERABLE_WORKTIME;

        [JsonProperty]
        [Option("MechanicsStation.STRINGS.OPTIONS.MACHINE_TINKERABLE_WORKTIME.TITLE", Format = "F0")]
        [Limit(BUILDINGS.WORK_TIME_SECONDS.SHORT_WORK_TIME, BUILDINGS.WORK_TIME_SECONDS.VERY_LONG_WORK_TIME)]
        public float MachineTinkerableWorkTime { get => machinetinkerableworktime; set => machinetinkerableworktime = Mathf.Clamp(value, BUILDINGS.WORK_TIME_SECONDS.SHORT_WORK_TIME, BUILDINGS.WORK_TIME_SECONDS.VERY_LONG_WORK_TIME); }
    }
}
