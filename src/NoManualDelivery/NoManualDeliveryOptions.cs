using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace NoManualDelivery
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("No Manual Delivery", null, null, false)]
    [ConfigFile(IndentOutput: true)]
    [RestartRequired]
    internal sealed class NoManualDeliveryOptions : BaseOptions<NoManualDeliveryOptions>
    {
        [JsonProperty]
        [Option("NoManualDelivery.STRINGS.OPTIONS.ALLOWALWAYSPICKUPEDIBLE.TITLE", "NoManualDelivery.STRINGS.OPTIONS.ALLOWALWAYSPICKUPEDIBLE.TOOLTIP")]
        public bool AllowAlwaysPickupEdible { get; set; }

        [JsonProperty]
        [Option("NoManualDelivery.STRINGS.OPTIONS.ALLOWTRANSFERARMPICKUPGASLIQUID.TITLE", "NoManualDelivery.STRINGS.OPTIONS.ALLOWTRANSFERARMPICKUPGASLIQUID.TOOLTIP")]
        public bool AllowTransferArmPickupGasLiquid { get; set; }

        public NoManualDeliveryOptions()
        {
            AllowAlwaysPickupEdible = true;
            AllowTransferArmPickupGasLiquid = false;
        }
    }
}
