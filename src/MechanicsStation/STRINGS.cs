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
                        global::STRINGS.UI.FormatAsLink("Custom Parts", "MACHINE_PARTS"),
                        " to improve building production efficiency.\n\nAssigned Duplicants must possess the ",
                        global::STRINGS.UI.FormatAsLink("Improved Tinkering", "MACHINE_TECHNICIAN"),
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
                    public static LocString TOOLTIP = string.Concat(new string[]
                    {
                        "A skilled Duplicant has jerry rigged this ",
                        global::STRINGS.UI.PRE_KEYWORD,
                        "Building",
                        global::STRINGS.UI.PST_KEYWORD,
                        " to temporarily run faster"
                    });
                }
            }
        }

        public class UI
        {
            public class ROLES_SCREEN
            {
                public class PERKS
                {
                    public class CAN_MACHINE_TINKER
                    {
                        public static LocString DESCRIPTION = global::STRINGS.UI.FormatAsLink(global::STRINGS.DUPLICANTS.MODIFIERS.MACHINETINKER.NAME, "BUILDINGS") + " and " + ITEMS.INDUSTRIAL_PRODUCTS.MACHINE_PARTS.NAME + " Crafting";
                    }
                }
            }
        }
    }
}
