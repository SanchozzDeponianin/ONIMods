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
        public bool allow_tinker_saptree { get; set; } = true;

        [JsonProperty]
        [Option(Format = "F2")]
        [Limit(0, 1)]
        public float farm_tinker_bonus_decor { get; set; } = FARM_TINKER_BONUS_DECOR;

        [JsonProperty]
        [Option]
        public bool prevent_tending_grown_or_wilting { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool prevent_fertilization_irrigation_not_growning { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool tree_fix_tinkering_branches { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool tree_unlock_mutation { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool oxyfern_fix_output_cell { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool saltplant_adjust_gas_consumption { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool coldbreather_adjust_radiation_by_grow_speed { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool coldbreather_decrease_radiation_by_wildness { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool critter_trap_adjust_gas_production { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool critter_trap_decrease_gas_production_by_wildness { get; set; } = false;

        [JsonProperty]
        [Option]
        public bool critter_trap_can_give_seeds { get; set; } = true;

        // шансы доп семян
        [JsonObject(MemberSerialization.OptIn)]
        public class ExtraSeeds
        {
            [JsonProperty]
            [Option(Format = "F2")]
            [Limit(0, 4 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
            public float base_chance_decorative { get; set; } = EXTRA_SEED_CHANCE_BASE_VALUE_DECORATIVE;

            [JsonProperty]
            [Option(Format = "F2")]
            [Limit(0, CROPS.BASE_BONUS_SEED_PROBABILITY)]
            public float base_chance_not_decorative { get; set; } = EXTRA_SEED_CHANCE_BASE_VALUE_NOT_DECORATIVE;

            [JsonProperty]
            [Option(Format = "F2")]
            [Limit(0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
            public float modifier_divergent { get; set; } = EXTRA_SEED_CHANCE_MODIFIER_DIVERGENT;

            [JsonProperty]
            [Option(Format = "F2")]
            [Limit(0, 2 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
            public float modifier_worm { get; set; } = EXTRA_SEED_CHANCE_MODIFIER_WORM;

            [JsonProperty]
            [Option]
            public bool pip_required_to_extract { get; set; } = true;
        }
        [JsonProperty]
        [Option]
        public ExtraSeeds extra_seeds { get; set; } = new ExtraSeeds();
    }
}
