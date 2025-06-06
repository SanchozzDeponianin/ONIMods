using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace NoManualDelivery
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [Option]
        public bool AllowAlwaysPickupEdible { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool AllowAlwaysPickupKettle { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool AllowTransferArmPickupGasLiquid { get; set; } = false;
    }
}
