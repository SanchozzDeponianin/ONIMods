using STRINGS;

namespace MechanicsStation
{
    public class STRINGS
    {
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class MECHANICSSTATION
                {
                    public static LocString EFFECT = string.Concat(new string[]
                    {
                        "Produces ",
                        UI.FormatAsLink("Custom Parts", "MACHINE_PARTS"),
                        " to improve building production efficiency.\n\n",
                        "Assigned Duplicants must possess the ",
                        UI.FormatAsLink("Improved Tinkering", "MACHINE_TECHNICIAN"),
                        " trait.\n\nThis building is a necessary component of the Machine Shop room."
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
            public static LocString DESCRIPTION = UI.FormatAsLink(global::STRINGS.DUPLICANTS.MODIFIERS.MACHINETINKER.NAME, "BUILDINGS") + " and " + ITEMS.INDUSTRIAL_PRODUCTS.MACHINE_PARTS.NAME + " Crafting";
        }


        internal static void DoReplacement()
        {
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
            LocString.CreateLocStringKeys(typeof(DUPLICANTS));
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{MechanicsStationConfig.ID.ToUpperInvariant()}.DESC", global::STRINGS.BUILDINGS.PREFABS.MACHINESHOP.DESC);
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{MechanicsStationConfig.ID.ToUpperInvariant()}.NAME", global::STRINGS.BUILDINGS.PREFABS.MACHINESHOP.NAME);
        }
    }
}
