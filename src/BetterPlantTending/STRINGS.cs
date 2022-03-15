using System.Collections.Generic;
using System.Linq;
using STRINGS;
using SanchozzONIMods.Lib;

namespace BetterPlantTending
{
    public class STRINGS
    {
        private const string COLDBREATHER = "{COLDBREATHER}";
        private const string OXYFERN = "{OXYFERN}";
        private const string GASGRASS = "{GASGRASS}";
        private const string WOOD_TREE = "{WOOD_TREE}";
        private const string SALTPLANT = "{SALTPLANT}";
        private const string CRITTERTRAPPLANT = "{CRITTERTRAPPLANT}";
        private const string SAPTREE = "{SAPTREE}";
        private const string SQUIRREL = "{SQUIRREL}";
        private const string FARMTINKER = "{FARMTINKER}";
        private const string DIVERGENTCROPTENDED = "{DIVERGENTCROPTENDED}";
        private const string WORMCROPTENDED = "{WORMCROPTENDED}";

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

        public class OPTIONS
        {
            public class ALLOW_TINKER_DECORATIVE
            {
                public static LocString NAME = "Enable tinkering decorative plants";
                public static LocString TOOLTIP = "To obtaining additional seeds";
            }
            public class ALLOW_TINKER_SAPTREE
            {
                public static LocString NAME = $"Enable tinkering {SAPTREE}";
                public static LocString TOOLTIP = "To accelerate the absorption of food and increase the conversion rate of calories into Resine";
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
            public class TREE_FIX_TINKERING_BRANCHES
            {
                public static LocString NAME = $"Fix tinkering of {WOOD_TREE} branches";
                public static LocString TOOLTIP = "Usually farmers lose the ability to tinker with them after rebuilding the Greenhouse or reloading the save file";
            }
            public class TREE_UNLOCK_MUTATION
            {
                public static LocString NAME = $"Unlock {WOOD_TREE} mutations";
                public static LocString TOOLTIP = "These mutations are provided within the game, but were not used for some reason";
            }
            public class OXYFERN_FIX_OUTPUT_CELL
            {
                public static LocString NAME = $"Move the output cell of {OXYFERN} 1 up";
            }
            public class SALTPLANT_ADJUST_GAS_CONSUMPTION
            {
                public static LocString NAME = $"Adjust the gas absorption rate of {SALTPLANT} in proportion to the growth rate";
            }
            public class COLDBREATHER_ADJUST_RADIATION_BY_GROW_SPEED
            {
                public static LocString NAME = $"Adjust the radiation emission of {COLDBREATHER} in proportion to the growth rate";
            }
            public class COLDBREATHER_DECREASE_RADIATION_BY_WILDNESS
            {
                public static LocString NAME = $"Decrease the radiation emission of wild {COLDBREATHER}";
                public static LocString TOOLTIP = $"To be honest, it should have been done by the {UI.FormatAsKeyWord("Klei")} themselves";
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
            public class PIP_REQUIRED_TO_EXTRACT
            {
                public static LocString NAME = "A {SQUIRREL} is required to extract the seeds";
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
                { SALTPLANT, CREATURES.SPECIES.SALTPLANT.NAME },
                { CRITTERTRAPPLANT, CREATURES.SPECIES.CRITTERTRAPPLANT.NAME },
                { SAPTREE, CREATURES.SPECIES.SAPTREE.NAME },
                { SQUIRREL, CREATURES.SPECIES.SQUIRREL.NAME },
                { FARMTINKER, global::STRINGS.DUPLICANTS.MODIFIERS.FARMTINKER.NAME },
                { DIVERGENTCROPTENDED, CREATURES.MODIFIERS.DIVERGENTPLANTTENDED.NAME},
                { WORMCROPTENDED, CREATURES.MODIFIERS.DIVERGENTPLANTTENDEDWORM.NAME},
            };
            foreach (var k in dictionary.Keys.ToList())
                dictionary[k] = UI.FormatAsKeyWord(dictionary[k]);
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
            OPTIONS.PREVENT_FERTILIZATION_IRRIGATION_NOT_GROWNING.TOOLTIP.ReplaceText(OPTIONS.PREVENT_TENDING_GROWN_OR_WILTING.TOOLTIP.text);
            OPTIONS.BASE_CHANCE_NOT_DECORATIVE.TOOLTIP.ReplaceText(OPTIONS.BASE_CHANCE_DECORATIVE.TOOLTIP.text);
            LocString.CreateLocStringKeys(typeof(DUPLICANTS));
        }
    }
}
