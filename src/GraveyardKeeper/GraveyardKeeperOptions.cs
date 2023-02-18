using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace GraveyardKeeper
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    internal sealed class GraveyardKeeperOptions : BaseOptions<GraveyardKeeperOptions>
    {
        [JsonIgnore]
        [Option]
        public LocText title { get; set; }

        [JsonProperty]
        [Option]
        public bool non_yielding_plants { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool single_harvest_plants { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool regular_plants { get; set; } = true;

        [JsonProperty]
        [Option]
        [Limit(1, 5)]
        public int max_plants_spawn { get; set; } = 3;
    }
}
