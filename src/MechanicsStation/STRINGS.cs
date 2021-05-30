using System.Collections.Generic;
using STRINGS;
using SanchozzONIMods.Lib;

namespace MechanicsStation
{
    public class STRINGS
    {
        private const string MACHINERY = "{MACHINERY}";
        private const string MACHINE_PARTS = "{MACHINE_PARTS}";
        private const string MACHINE_TECHNICIAN = "{MACHINE_TECHNICIAN}";
        private const string MACHINERY_SPEED = "{MACHINERY_SPEED}";
        private const string MACHINE_TINKER = "{MACHINE_TINKER}";
        private const string MACHINE_SHOP = "{MACHINE_SHOP}";

        public class BUILDINGS
        {
            public class PREFABS
            {
                public class MECHANICSSTATION
                {
                    public static LocString EFFECT = string.Concat(new string[]
                    {
                        $"Produces {UI.FormatAsLink(MACHINE_PARTS, "MACHINE_PARTS")} to improve building production efficiency.\n\n",
                        $"Assigned Duplicants must possess the {UI.FormatAsLink(MACHINE_TECHNICIAN, "Technicals1")} trait.\n\n",
                        $"This building is a necessary component of the {MACHINE_SHOP} room."
                    });
                }
            }
        }

        public class DUPLICANTS
        {
            public class ATTRIBUTES
            {
                public class CRAFTINGSPEED
                {
                    public static LocString NAME = "Fabrication Speed";
                }
                public class MACHINERY
                {
                    public static LocString MACHINE_TINKER_EFFECT_MODIFIER = "{0} {MACHINE_TINKER} Effect Duration";
                }
            }

            public class MODIFIERS
            {
                public class MACHINETINKER
                {
                    public static LocString TOOLTIP = $"A skilled Duplicant has jerry rigged this {UI.FormatAsKeyWord("Building")} to temporarily run faster";
                }
            }
        }

        public class PERK_CAN_MACHINE_TINKER
        {
            public static LocString DESCRIPTION = $"{UI.FormatAsLink(MACHINE_TINKER, "BUILDINGS")} and {UI.FormatAsKeyWord(MACHINE_PARTS)} Crafting";
        }

        public class OPTIONS
        {
            public class MACHINERY_SPEED_MODIFIER
            {
                public static LocString TITLE = $"{UI.FormatAsKeyWord(MACHINERY_SPEED)} +X% value";
                public static LocString TOOLTIP = "Increases work speed of autonomously working buildings";
            }
            public class CRAFTING_SPEED_MODIFIER
            {
                public static LocString TITLE = $"{UI.FormatAsKeyWord("Fabrication Speed")} +X% value";
                public static LocString TOOLTIP = "Increases work speed of duplicant-operated buildings";
            }
            public class MACHINE_TINKER_EFFECT_DURATION
            {
                public static LocString TITLE = $"The {UI.FormatAsKeyWord(MACHINE_TINKER)} effect duration, cycles";
            }
            public class MACHINE_TINKER_EFFECT_DURATION_PER_SKILL
            {
                public static LocString TITLE = $"The {UI.FormatAsKeyWord(MACHINE_TINKER)} effect +X% duration per {UI.FormatAsKeyWord(MACHINERY)} attribute level";
            }
            public class MACHINE_TINKERABLE_WORKTIME
            {
                public static LocString TITLE = "The time required to tune up buildings, seconds";
            }
        }

        internal static void DoReplacement()
        {
            var dictionary = new Dictionary<string, string>
            {
                { MACHINE_PARTS, ITEMS.INDUSTRIAL_PRODUCTS.MACHINE_PARTS.NAME },
                { MACHINERY, global::STRINGS.DUPLICANTS.ATTRIBUTES.MACHINERY.NAME },
                { MACHINERY_SPEED, global::STRINGS.DUPLICANTS.ATTRIBUTES.MACHINERYSPEED.NAME },
                { MACHINE_TECHNICIAN, global::STRINGS.DUPLICANTS.ROLES.MACHINE_TECHNICIAN.NAME },
                { MACHINE_TINKER, global::STRINGS.DUPLICANTS.MODIFIERS.MACHINETINKER.NAME },
                { MACHINE_SHOP, ROOMS.TYPES.MACHINE_SHOP.NAME }
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);

            LocString.CreateLocStringKeys(typeof(BUILDINGS));
            LocString.CreateLocStringKeys(typeof(DUPLICANTS));

            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{MechanicsStationConfig.ID.ToUpperInvariant()}.DESC", global::STRINGS.BUILDINGS.PREFABS.MACHINESHOP.DESC);
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{MechanicsStationConfig.ID.ToUpperInvariant()}.NAME", global::STRINGS.BUILDINGS.PREFABS.MACHINESHOP.NAME);
        }
    }
}
