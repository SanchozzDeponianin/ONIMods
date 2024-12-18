using static STRINGS.MISC.TAGS;
using static STRINGS.UI;

namespace PotatoElectrobanks
{
    using static PotatoElectrobanksPatches;

    public class STRINGS
    {
        public class ITEMS
        {
            public class INDUSTRIAL_PRODUCTS
            {
                public class ELECTROBANK_POTATO
                {
                    public static LocString NAME = FormatAsLink("Bio \"{0}\" Power Bank", "ELECTROBANK_POTATO_{1}");
                    public static LocString DESC = $"A disposable {FormatAsLink("Bio Power Bank", "ELECTROBANK")} made with {{0}}.\n\nDuplicants can produce new {FormatAsLink("Bio Power Banks", "ELECTROBANK")} at the {FormatAsLink("Crafting Station", "CRAFTINGTABLE")}.\n\nMust be kept dry.";
                }
            }
        }

        internal static void DoReplacement()
        {
            LocString.CreateLocStringKeys(typeof(ITEMS));
            Strings.Add($"STRINGS.MISC.TAGS.{PotatoPortableBattery.ToString().ToUpperInvariant()}", CHARGEDPORTABLEBATTERY);
            Strings.Add($"STRINGS.MISC.TAGS.{NonPotatoPortableBattery.ToString().ToUpperInvariant()}", CHARGEDPORTABLEBATTERY);
        }
    }
}