using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace AnyIceKettle
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class AnyIceKettleOptions : BaseOptions<AnyIceKettleOptions>
    {
        [JsonIgnore]
        [Option]
        public LocText Label { get; set; }

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.EXPANSION1_ID)]
        public bool melt_resin { get; set; } = false;

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.DLC3_ID)]
        public bool melt_gunk { get; set; } = false;

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.DLC3_ID)]
        public bool melt_phytooil { get; set; } = false;
    }
}
