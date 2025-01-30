using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace Lagoo
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class LagooOptions : BaseOptions<LagooOptions>
    {
        [JsonProperty]
        [Option]
        [Limit(1, 3)]
        public int warm_touch_duration { get; set; } = 3;
    }
}
