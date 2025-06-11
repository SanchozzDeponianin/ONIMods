using System.Collections.Generic;
using System.Linq;
using STRINGS;
using static STRINGS.UI;
using SanchozzONIMods.Lib;

namespace BetterPlantTending
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        private const string COLDBREATHER = "{COLDBREATHER}";
        private const string OXYFERN = "{OXYFERN}";
        private const string GASGRASS = "{GASGRASS}";
        private const string WOOD_TREE = "{WOOD_TREE}";
        private const string SPACE_TREE = "{SPACE_TREE}";
        private const string SUGARWATER = "{SUGARWATER}";
        private const string SALTPLANT = "{SALTPLANT}";
        private const string FILTERPLANT = "{FILTERPLANT}";
        private const string BLUE_GRASS = "{BLUE_GRASS}";
        private const string DINOFERN = "{DINOFERN}";
        private const string CRITTERTRAPPLANT = "{CRITTERTRAPPLANT}";
        private const string SAPTREE = "{SAPTREE}";
        private const string SQUIRREL = "{SQUIRREL}";
        private const string FARMTINKER = "{FARMTINKER}";
        private const string DIVERGENTCROPTENDED = "{DIVERGENTCROPTENDED}";
        private const string WORMCROPTENDED = "{WORMCROPTENDED}";
        private const string BUTTERFLYCROPTENDED = "{BUTTERFLYCROPTENDED}";
        private const string MATURITYMAX = "{MATURITYMAX}";
        private const string YIELDAMOUNT = "{YIELDAMOUNT}";
        private const string TINKER = "{TINKER}";

        public class DUPLICANTS
        {
            public class ATTRIBUTES
            {
                public class EXTRASEEDCHANCE
                {
                    public static LocString NAME = "Extra Seed Chance";
                }
            }
        }

        public class UI
        {
            public class UISIDESCREENS
            {
                public class PLANTERSIDESCREEN
                {
                    public static LocString BONUS_SEEDS = $"Base {FormatAsLink("Extra Seed", "PLANTS")} Harvest Chance: {{0}}";
                    public class TOOLTIPS
                    {
                        public static LocString BONUS_SEEDS = "This plant has a {0} chance to produce new seeds when tended or pollinated\n\nAffected by: {1}{2}";
                        public static LocString SQUIRREL_NEEDED = $"\n\nA {SQUIRREL} is required to extract the seeds";
                    }
                }
            }
        }

        public class OPTIONS
        {
            public class ALLOW_TINKER_DECORATIVE
            {
                public static LocString NAME = "Enable tinkering decorative plants by default";
                public static LocString TOOLTIP = $"To obtaining additional seeds\nThis affects newly spawned plants.\nFor existing plants, use the {TINKER} button";
            }
            public class ALLOW_TINKER_SAPTREE
            {
                public static LocString NAME = $"Enable tinkering {SAPTREE}";
                public static LocString TOOLTIP = "To accelerate the absorption of food and increase the conversion rate of calories into Resine";
            }
            public class ALLOW_TENDING_TOGETHER
            {
                public static LocString NAME = $"Insectoids of different species being able to accelerate plants together at the same time";
            }
            public class PREVENT_TENDING_GROWN_OR_WILTING
            {
                public static LocString NAME = "Farmers will not tinkering with wilted or non-growing plants";
                public static LocString TOOLTIP = $"Including fully grown plants, sleeping {GASGRASS} and the hunger {CRITTERTRAPPLANT}";
            }
            public class PREVENT_FERTILIZATION_IRRIGATION_NOT_GROWNING
            {
                public static LocString NAME = "Non-growing plants will not consume solid and liquid fertilizers";
                public static LocString TOOLTIP = "";
            }
            public class FARM_TINKER_BONUS_DECOR
            {
                public static LocString NAME = $"The {FARMTINKER} effect gives a bonus to the decor";
            }
            public class TREE_UNLOCK_MUTATION
            {
                public static LocString NAME = $"Unlock {WOOD_TREE} mutations";
                public static LocString TOOLTIP = "These mutations are provided within the game, but were not used for some reason";
            }
            public class SPACE_TREE_UNLOCK_MUTATION
            {
                public static LocString NAME = $"Unlock {SPACE_TREE} mutations";
                public static LocString TOOLTIP = $"Now mutations affect the {SUGARWATER} production rate\nThe higher the {YIELDAMOUNT} and the shorter the {MATURITYMAX}, the higher the {SUGARWATER} production rate";
            }
            public class SPACE_TREE_ADJUST_PRODUCTIVITY
            {
                public static LocString NAME = $"Adjust the {SUGARWATER} production rate of {SPACE_TREE} in proportion to the growth rate of its branches";
            }
            public class OXYFERN_FIX_OUTPUT_CELL
            {
                public static LocString NAME = $"Move the output cell of {OXYFERN} 1 up";
            }
            public class COLDBREATHER_ADJUST_RADIATION_BY_GROW_SPEED
            {
                public static LocString NAME = $"Adjust the radiation emission of {COLDBREATHER} in proportion to the growth rate";
            }
            public class COLDBREATHER_DECREASE_RADIATION_BY_WILDNESS
            {
                public static LocString NAME = $"Decrease the radiation emission of wild {COLDBREATHER}";
                public static LocString TOOLTIP = $"To be honest, it should have been done by the {FormatAsKeyWord("Klei")} themselves";
            }
            public class CRITTER_TRAP_ADJUST_GAS_PRODUCTION
            {
                public static LocString NAME = $"Adjust the gas production rate of {CRITTERTRAPPLANT} in proportion to the growth rate";
                public static LocString TOOLTIP = "So you will always get the same amount of Gas for each sacrificed Creature";
            }
            public class CRITTER_TRAP_DECREASE_GAS_PRODUCTION_BY_WILDNESS
            {
                public static LocString NAME = $"Decrease the gas production rate of wild {CRITTERTRAPPLANT}";
            }
            public class CRITTER_TRAP_CAN_GIVE_SEEDS
            {
                public static LocString NAME = $"The {CRITTERTRAPPLANT} can produce seeds when harvesting";
            }
            public class SALTPLANT_ADJUST_GAS_CONSUMPTION
            {
                public static LocString NAME = $"Adjust the gas absorption rate of {SALTPLANT} in proportion to the growth rate";
            }
            public class HYDROCACTUS_ADJUST_GAS_CONSUMPTION
            {
                public static LocString NAME = $"Adjust the gas absorption rate of {FILTERPLANT} in proportion to the growth rate";
            }
            public class BLUE_GRASS_ADJUST_GAS_CONSUMPTION
            {
                public static LocString NAME = $"Adjust the gas absorption rate of {BLUE_GRASS} in proportion to the growth rate";
            }
            public class DINOFERN_ADJUST_GAS_CONSUMPTION
            {
                public static LocString NAME = $"Adjust the gas absorption rate of {DINOFERN} in proportion to the growth rate";
            }
            public class DINOFERN_CAN_GIVE_SEEDS
            {
                public static LocString NAME = $"The {DINOFERN} can produce seeds when harvesting";
            }
            public class EXTRA_SEEDS
            {
                public static LocString CATEGORY = "Extra seeds for non-yielding plants";
            }
            public class BASE_CHANCE_DECORATIVE
            {
                public static LocString NAME = "Base chance to obtain an extra seed of decorative plants";
                public static LocString TOOLTIP = "Extra seeds are obtained when farmers and insectoids apply their accelerating effects to plants\nThe chances from the effects of insectoids and the farmer's own chance are added to the base chance";
            }
            public class BASE_CHANCE_NOT_DECORATIVE
            {
                public static LocString NAME = "Base chance to obtain an extra seed of non-decorative plants";
                public static LocString TOOLTIP = "";
            }
            public class MODIFIER_DIVERGENT
            {
                public static LocString NAME = $"Additional chance from the {DIVERGENTCROPTENDED} effect";
            }
            public class MODIFIER_WORM
            {
                public static LocString NAME = $"Additional chance from the {WORMCROPTENDED} effect";
            }
            public class MODIFIER_BUTTERFLY
            {
                public static LocString NAME = $"Additional chance from the {BUTTERFLYCROPTENDED} effect";
            }
            public class PIP_REQUIRED_TO_EXTRACT
            {
                public static LocString NAME = $"A {SQUIRREL} is required to extract the seeds";
                public static LocString TOOLTIP = "If you turn it off, the seeds themselves will drop after a while";
            }
        }

        internal static void DoReplacement()
        {
            var dictionary = new Dictionary<string, string>
            {
                { COLDBREATHER, CREATURES.SPECIES.COLDBREATHER.NAME },
                { OXYFERN, CREATURES.SPECIES.OXYFERN.NAME },
                { GASGRASS, CREATURES.SPECIES.GASGRASS.NAME },
                { WOOD_TREE, CREATURES.SPECIES.WOOD_TREE.NAME },
                { SPACE_TREE, CREATURES.SPECIES.SPACETREE.NAME },
                { SUGARWATER, ELEMENTS.SUGARWATER.NAME },
                { SALTPLANT, CREATURES.SPECIES.SALTPLANT.NAME },
                { FILTERPLANT, CREATURES.SPECIES.FILTERPLANT.NAME },
                { BLUE_GRASS, CREATURES.SPECIES.BLUE_GRASS.NAME },
                { DINOFERN, CREATURES.SPECIES.DINOFERN.NAME },
                { CRITTERTRAPPLANT, CREATURES.SPECIES.CRITTERTRAPPLANT.NAME },
                { SAPTREE, CREATURES.SPECIES.SAPTREE.NAME },
                { SQUIRREL, CREATURES.SPECIES.SQUIRREL.NAME },
                { FARMTINKER, global::STRINGS.DUPLICANTS.MODIFIERS.FARMTINKER.NAME },
                { DIVERGENTCROPTENDED, CREATURES.MODIFIERS.DIVERGENTPLANTTENDED.NAME},
                { WORMCROPTENDED, CREATURES.MODIFIERS.DIVERGENTPLANTTENDEDWORM.NAME},
                { BUTTERFLYCROPTENDED, CREATURES.MODIFIERS.BUTTERFLYPOLLINATED.NAME},
                { MATURITYMAX, CREATURES.ATTRIBUTES.MATURITYMAX.NAME },
                { YIELDAMOUNT, global::STRINGS.DUPLICANTS.ATTRIBUTES.YIELDAMOUNT.NAME },
                { TINKER, USERMENUACTIONS.TINKER.ALLOW },
            };
            foreach (var k in dictionary.Keys.ToList())
                dictionary[k] = FormatAsKeyWord(StripLinkFormatting(dictionary[k]));
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
            OPTIONS.PREVENT_FERTILIZATION_IRRIGATION_NOT_GROWNING.TOOLTIP.ReplaceText(OPTIONS.PREVENT_TENDING_GROWN_OR_WILTING.TOOLTIP.text);
            OPTIONS.BASE_CHANCE_NOT_DECORATIVE.TOOLTIP.ReplaceText(OPTIONS.BASE_CHANCE_DECORATIVE.TOOLTIP.text);
            LocString.CreateLocStringKeys(typeof(DUPLICANTS));
        }
    }
}
