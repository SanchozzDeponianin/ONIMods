using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace Smelter
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    [RestartRequired]
    internal class SmelterOptions : BaseOptions<SmelterOptions>
    {
        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.RECIPES.KATAIRITE_TO_TUNGSTEN.TITLE", "Smelter.STRINGS.OPTIONS.RECIPES.KATAIRITE_TO_TUNGSTEN.TOOLTIP", "Smelter.STRINGS.OPTIONS.RECIPES.TITLE")]
        public bool RecipeKatairiteToTungsten { get; set; } = true;

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.RECIPES.PHOSPHORITE_TO_PHOSPHORUS.TITLE", "Smelter.STRINGS.OPTIONS.RECIPES.PHOSPHORITE_TO_PHOSPHORUS.TOOLTIP", "Smelter.STRINGS.OPTIONS.RECIPES.TITLE")]
        public bool RecipePhosphoriteToPhosphorus { get; set; } = true;

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.RECIPES.POLYPROPYLENE_TO_NAPHTHA.TITLE", "Smelter.STRINGS.OPTIONS.RECIPES.POLYPROPYLENE_TO_NAPHTHA.TOOLTIP", "Smelter.STRINGS.OPTIONS.RECIPES.TITLE")]
        public bool RecipePlasticToNaphtha { get; set; } = true;

        [JsonProperty]
        public virtual bool RecipeResinToIsoresin { get; set; } = true;

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.RECIPES.WOOD_TO_REFINEDCARBON.TITLE", "Smelter.STRINGS.OPTIONS.RECIPES.WOOD_TO_REFINEDCARBON.TOOLTIP", "Smelter.STRINGS.OPTIONS.RECIPES.TITLE")]
        public bool RecipeWoodToCarbon { get; set; } = true;
       
        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.FEATURES.DROP_OVERHEATED_COOLANT.TITLE", "Smelter.STRINGS.OPTIONS.FEATURES.DROP_OVERHEATED_COOLANT.TOOLTIP", "Smelter.STRINGS.OPTIONS.FEATURES.TITLE")]
        public bool MetalRefineryDropOverheatedCoolant { get; set; } = false;

        [JsonProperty]
        [Option("Smelter.STRINGS.OPTIONS.FEATURES.REUSE_COOLANT.TITLE", "Smelter.STRINGS.OPTIONS.FEATURES.REUSE_COOLANT.TOOLTIP", "Smelter.STRINGS.OPTIONS.FEATURES.TITLE")]
        public bool MetalRefineryReuseCoolant { get; set; } = false;
    }

    internal sealed class SmelterOptionsExpansion1 : SmelterOptions
    {
        [Option("Smelter.STRINGS.OPTIONS.RECIPES.RESIN_TO_ISORESIN.TITLE", "Smelter.STRINGS.OPTIONS.RECIPES.RESIN_TO_ISORESIN.TOOLTIP", "Smelter.STRINGS.OPTIONS.RECIPES.TITLE")]
        public override bool RecipeResinToIsoresin { get => base.RecipeResinToIsoresin; set => base.RecipeResinToIsoresin = value; }
    }
}
