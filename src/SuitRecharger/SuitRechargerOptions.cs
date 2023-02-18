using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace SuitRecharger
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class SuitRechargerOptions : BaseOptions<SuitRechargerOptions>
    {
        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(75, 1000)]
        public float o2_capacity { get; set; } = 200f;

        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(50, 500)]
        public float fuel_capacity { get; set; } = 100f;
    }
}
