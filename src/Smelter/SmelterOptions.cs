using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace Smelter
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class SmelterOptions : BaseOptions<SmelterOptions>
    {
        [JsonObject(MemberSerialization.OptIn)]
        public sealed class Recipes
        {
            [JsonProperty]
            [Option]
            public bool Katairite_To_Tungsten { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool Phosphorite_To_Phosphorus { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool Plastic_To_Naphtha { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool Sulfur_To_LiquidSulfur { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool Resin_To_Isoresin { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool Wood_To_Carbon { get; set; } = true;
        }

        [JsonProperty]
        [Option]
        public Recipes recipes { get; set; } = new Recipes();

        [JsonObject(MemberSerialization.OptIn)]
        public sealed class Features
        {
            [JsonProperty]
            [Option]
            public bool MetalRefinery_Drop_Overheated_Coolant { get; set; } = false;

            [JsonProperty]
            [Option]
            public bool MetalRefinery_Reuse_Coolant { get; set; } = false;
        }

        [JsonProperty]
        [Option]
        public Features features { get; set; } = new Features();
    }
}
