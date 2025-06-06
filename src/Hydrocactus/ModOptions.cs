using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace Hydrocactus
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(350, 1000)]
        public int yield_amount { get; set; } = 650;
        [JsonProperty]
        [Option]
        [Limit(1, 3)]
        public int carepackage_seeds_amount { get; set; } = 2;
    }
}
