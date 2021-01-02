using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace Smelter
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Smelter")]
    [ConfigFile(IndentOutput: true)]
    [RestartRequired]
    internal sealed class SmelterOptions : BaseOptions<SmelterOptions>
    {
        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.KATAIRITE_TO_TUNGSTEN.TITLE", "Smelter.STRINGS.OPTIONS.KATAIRITE_TO_TUNGSTEN.TOOLTIP")]
        public bool RecipeKatairiteToTungsten { get; set; }

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.PHOSPHORITE_TO_PHOSPHORUS.TITLE", "Smelter.STRINGS.OPTIONS.PHOSPHORITE_TO_PHOSPHORUS.TOOLTIP")]
        public bool RecipePhosphoriteToPhosphorus { get; set; }

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.PLASTIC_TO_NAPHTHA.TITLE", "Smelter.STRINGS.OPTIONS.PLASTIC_TO_NAPHTHA.TOOLTIP")]
        public bool RecipePlasticToNaphtha { get; set; }

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.WOOD_TO_CARBON.TITLE", "Smelter.STRINGS.OPTIONS.WOOD_TO_CARBON.TOOLTIP")]
        public bool RecipeWoodToCarbon { get; set; }

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.DROP_OVERHEATED_COOLANT.TITLE", "Smelter.STRINGS.OPTIONS.DROP_OVERHEATED_COOLANT.TOOLTIP")]
        public bool MetalRefineryDropOverheatedCoolant { get; set; }

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.REUSE_COOLANT.TITLE", "Smelter.STRINGS.OPTIONS.REUSE_COOLANT.TOOLTIP")]
        public bool MetalRefineryReuseCoolant { get; set; }

        public SmelterOptions()
        {
            RecipeKatairiteToTungsten = true;
            RecipePhosphoriteToPhosphorus = true;
            RecipePlasticToNaphtha = true;
            RecipeWoodToCarbon = true;
            MetalRefineryDropOverheatedCoolant = false;
            MetalRefineryReuseCoolant = false;
        }
    }
}
