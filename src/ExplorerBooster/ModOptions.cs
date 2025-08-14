using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace ExplorerBooster
{
    internal enum CraftAt
    {
        [Option] Basic,
        [Option] Advanced
    }

    internal enum WattageCost
    {
        [Option] TIER_0,
        [Option] TIER_1,
        [Option] TIER_2,
        [Option] TIER_3,
    }

    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [Option]
        public CraftAt craft_at { get; set; } = CraftAt.Advanced;

        [JsonProperty]
        [Option]
        public WattageCost wattage { get; set; } = WattageCost.TIER_2;

        [JsonProperty]
        [Option]
        public bool starting_booster { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool care_package { get; set; } = true;
    }
}
