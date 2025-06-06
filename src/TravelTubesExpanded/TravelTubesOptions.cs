using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace TravelTubesExpanded
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [Option]
        [Limit(1, TravelTubeEntranceConfig.JOULES_PER_LAUNCH * Constants.W2KW * TravelTubeEntranceConfig.LAUNCHES_FROM_FULL_CHARGE)]
        public int kjoules_per_launch { get; set; } = (int)(TravelTubeEntranceConfig.JOULES_PER_LAUNCH * Constants.W2KW);
    }
}
