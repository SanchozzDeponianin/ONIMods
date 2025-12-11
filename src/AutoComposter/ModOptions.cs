using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace AutoComposter
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonIgnore]
        [Option]
        public LocText label { get; set; } = null;

        [JsonProperty]
        [Option]
        public bool make_special { get; set; } = false;

        [JsonProperty]
        [Option]
        public bool hide_from_filters { get; set; } = false;

        [JsonProperty]
        [Option]
        public bool hide_from_resources { get; set; } = false;
    }
}
