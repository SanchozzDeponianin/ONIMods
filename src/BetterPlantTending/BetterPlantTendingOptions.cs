using Newtonsoft.Json;
using TUNING;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;
using static BetterPlantTending.BetterPlantTendingAssets;

namespace BetterPlantTending
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    internal class BetterPlantTendingOptions : BaseOptions<BetterPlantTendingOptions>
    {
        // основные настройки
        [JsonProperty]
        [Option]
        public bool allow_tinker_decorative { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool prevent_tending_grown_or_wilting { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool fix_tinkering_tree_branches { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool adjust_gas_consumption { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool adjust_radiation_emission_by_grow_speed { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool adjust_radiation_emission_by_wildness { get; set; } = true;

        // шансы доп семян
        [JsonProperty]
        [Option(Format = "F2")]
        [Limit(0, 4 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
        public float extra_seed_chance_base_value_decorative { get; set; } = EXTRA_SEED_CHANCE_BASE_VALUE_DECORATIVE;

        [JsonProperty]
        [Option(Format = "F2")]
        [Limit(0, CROPS.BASE_BONUS_SEED_PROBABILITY)]
        public float extra_seed_chance_base_value_not_decorative { get; set; } = EXTRA_SEED_CHANCE_BASE_VALUE_NOT_DECORATIVE;

        [JsonProperty]
        [Option(Format = "F2")]
        [Limit(0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
        public float extra_seed_chance_modifier_divergent { get; set; } = EXTRA_SEED_CHANCE_MODIFIER_DIVERGENT;

        [JsonProperty]
        [Option(Format = "F2")]
        [Limit(0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
        public float extra_seed_chance_modifier_worm { get; set; } = EXTRA_SEED_CHANCE_MODIFIER_WORM;
    }
}
