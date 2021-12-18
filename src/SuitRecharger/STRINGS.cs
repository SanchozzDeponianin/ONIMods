using static STRINGS.BUILDING.STATUSITEMS;
using static STRINGS.UI;

namespace SuitRecharger
{
    public class STRINGS
    {
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class SUITRECHARGER
                {
                    public static LocString NAME = FormatAsLink("Suit Recharger", "SUITRECHARGER");
                    public static LocString DESC = "Suit Recharger allows Duplicants to recharge their Suits and Oxygen Masks without taking them off.";
                    public static LocString EFFECT = $"Recharges all kinds of {FormatAsLink("Exosuits", "EQUIPMENT")} and {FormatAsLink("Oxygen Masks", "OXYGENMASK")} and refuels them with {FormatAsLink("Oxygen", "OXYGEN")} and {FormatAsLink("Petroleum", "PETROLEUM")}.\n\nEmpties suits of {FormatAsLink("Polluted Water", "DIRTYWATER")}.\n\nOptionally, it can be connected to {FormatAsKeyWord("Pipes")} for waste disposal.";
                }
            }
        }

        public class DUPLICANTS
        {
            public class CHORES
            {
                public class PRECONDITIONS
                {
                    public static LocString IS_ENOUGH_FUEL = "{Selected} does not have enough Fuel";
                    public static LocString IS_ENOUGH_OXYGEN = "{Selected} does not have enough Oxygen";
                    public static LocString IS_SUIT_EQUIPPED = "Does not have an equipped Suit";
                    public static LocString IS_SUIT_HAS_ENOUGH_DURABILITY = "Suit is not durable enough";
                }
            }
        }

        public class STATUSITEMS
        {
            public class FUEL_NOPIPE
            {
                public static LocString TOOLTIP = $"\n{FormatAsKeyWord("Jet Suits")} will be charged only with {FormatAsLink("Oxygen", "OXYGEN")}";
            }
            public class LIQUIDWASTE_NOPIPE
            {
                public static LocString TOOLTIP = $"\n{FormatAsKeyWord("Soiled Suits")} will not be emptied of liquid waste";
            }
            public class GASWASTE_NOPIPE
            {
                public static LocString TOOLTIP = $"\nWhen cleaning the {FormatAsKeyWord("Suit")}, the gaseous waste will be released into the atmosphere";
            }
        }

        public class UI
        {
            public class UISIDESCREENS
            {
                public class SUITRECHARGERSIDESCREEN
                {
                    public static LocString TITLE = "Suit Durability Threshold";
                    public static LocString TOOLTIP = $"Duplicants will not use this Recharger when the {FormatAsKeyWord("Durability")} of their Suits falls below <b>{{0}}%</b>";
                }
            }
        }

        internal static void DoReplacement()
        {
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
            LocString.CreateLocStringKeys(typeof(UI));

            var prefix = "STRINGS.BUILDING.STATUSITEMS";
            var name = NEEDLIQUIDIN.NAME.Replace("{LiquidRequired}", NEEDLIQUIDIN.LINE_ITEM);
            Strings.Add($"{prefix}.FUELNOPIPECONNECTED.NAME", name);
            Strings.Add($"{prefix}.FUELNOPIPECONNECTED.TOOLTIP", NEEDLIQUIDIN.TOOLTIP + STATUSITEMS.FUEL_NOPIPE.TOOLTIP);

            Strings.Add($"{prefix}.LIQUIDWASTENOPIPECONNECTED.NAME", NEEDLIQUIDOUT.NAME);
            Strings.Add($"{prefix}.LIQUIDWASTENOPIPECONNECTED.TOOLTIP", NEEDLIQUIDOUT.TOOLTIP + STATUSITEMS.LIQUIDWASTE_NOPIPE.TOOLTIP);

            Strings.Add($"{prefix}.LIQUIDWASTEPIPEFULL.NAME", CONDUITBLOCKED.NAME);
            Strings.Add($"{prefix}.LIQUIDWASTEPIPEFULL.TOOLTIP", CONDUITBLOCKED.TOOLTIP + STATUSITEMS.LIQUIDWASTE_NOPIPE.TOOLTIP);

            Strings.Add($"{prefix}.GASWASTENOPIPECONNECTED.NAME", NEEDGASOUT.NAME);
            Strings.Add($"{prefix}.GASWASTENOPIPECONNECTED.TOOLTIP", NEEDGASOUT.TOOLTIP + STATUSITEMS.GASWASTE_NOPIPE.TOOLTIP);

            Strings.Add($"{prefix}.GASWASTEPIPEFULL.NAME", CONDUITBLOCKED.NAME);
            Strings.Add($"{prefix}.GASWASTEPIPEFULL.TOOLTIP", CONDUITBLOCKED.TOOLTIP + STATUSITEMS.GASWASTE_NOPIPE.TOOLTIP);
        }
    }
}