using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace Smelter
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    [RestartRequired]
    internal sealed class SmelterOptions : BaseOptions<SmelterOptions>
    {
        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.KATAIRITE_TO_TUNGSTEN.TITLE", "Smelter.STRINGS.OPTIONS.KATAIRITE_TO_TUNGSTEN.TOOLTIP", "Smelter.STRINGS.OPTIONS.RECIPES.TITLE")]
        public bool RecipeKatairiteToTungsten { get; set; }

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.PHOSPHORITE_TO_PHOSPHORUS.TITLE", "Smelter.STRINGS.OPTIONS.PHOSPHORITE_TO_PHOSPHORUS.TOOLTIP", "Smelter.STRINGS.OPTIONS.RECIPES.TITLE")]
        public bool RecipePhosphoriteToPhosphorus { get; set; }

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.POLYPROPYLENE_TO_NAPHTHA.TITLE", "Smelter.STRINGS.OPTIONS.POLYPROPYLENE_TO_NAPHTHA.TOOLTIP", "Smelter.STRINGS.OPTIONS.RECIPES.TITLE")]
        public bool RecipePlasticToNaphtha { get; set; }

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.WOOD_TO_REFINEDCARBON.TITLE", "Smelter.STRINGS.OPTIONS.WOOD_TO_REFINEDCARBON.TOOLTIP", "Smelter.STRINGS.OPTIONS.RECIPES.TITLE")]
        public bool RecipeWoodToCarbon { get; set; }

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.DROP_OVERHEATED_COOLANT.TITLE", "Smelter.STRINGS.OPTIONS.DROP_OVERHEATED_COOLANT.TOOLTIP", "Smelter.STRINGS.OPTIONS.FEATURES.TITLE")]
        public bool MetalRefineryDropOverheatedCoolant { get; set; }

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.REUSE_COOLANT.TITLE", "Smelter.STRINGS.OPTIONS.REUSE_COOLANT.TOOLTIP", "Smelter.STRINGS.OPTIONS.FEATURES.TITLE")]
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
