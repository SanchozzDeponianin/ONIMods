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

        [JsonProperty]
        [Option]
        public bool unlock_tree_mutation { get; set; } = true;

        // растение-ловушка
        [JsonObject(MemberSerialization.OptIn)]
        public class CritterTrap
        {
            [JsonProperty]
            [Option]
            public bool adjust_gas_production { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool use_gas_production_replanted_value { get; set; } = false;

            [JsonProperty]
            [Option]
            public bool can_give_seeds { get; set; } = true;
        }
        [JsonProperty]
        [Option]
        public CritterTrap critter_trap { get; set; } = new CritterTrap();

        // шансы доп семян
        [JsonObject(MemberSerialization.OptIn)]
        public class ExtraSeedChance
        {
            [JsonProperty]
            [Option(Format = "F2")]
            [Limit(0, 4 * CROPS.BASE_BONUS_SEED_PROBABILITY)]
            public float base_value_decorative { get; set; } = EXTRA_SEED_CHANCE_BASE_VALUE_DECORATIVE;

            [JsonProperty]
            [Option(Format = "F2")]
            [Limit(0, CROPS.BASE_BONUS_SEED_PROBABILITY)]
            public float base_value_not_decorative { get; set; } = EXTRA_SEED_CHANCE_BASE_VALUE_NOT_DECORATIVE;

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
        public ExtraSeedChance extra_seed_chance { get; set; } = new ExtraSeedChance();
    }
}
