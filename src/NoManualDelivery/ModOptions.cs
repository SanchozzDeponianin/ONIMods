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

        [JsonProperty]
        [Option]
        public SmartMode HoldMode { get; set; } = new();

        [JsonObject(MemberSerialization.OptIn)]
        public class SmartMode
        {
            [JsonIgnore]
            public bool Enabled => Chores || Items;

            [JsonProperty]
            [Option]
            public bool Chores { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool ByDefault { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool Items { get; set; } = true;

            [JsonProperty]
            [Option]
            [Limit(2, 60)]
            public int Timeout { get; set; } = 8;
        }
    }
}
