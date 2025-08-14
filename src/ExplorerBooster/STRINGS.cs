using STRINGS;
using static TUNING.ITEMS.BIONIC_UPGRADES;

namespace ExplorerBooster
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        public class DUPLICANTS
        {
            public class TRAITS
            {
                public class STARTWITHBOOSTER_EXPLORER
                {
                    public static LocString NAME = "";
                    public static LocString DESC = "";
                    public static LocString SHORT_DESC = $"Starts with a preinstalled <b>{ITEMS.BIONIC_BOOSTERS.BOOSTER_EXPLORER.NAME.text}</b>";
                    public static LocString SHORT_DESC_TOOLTIP = "";
                }
            }
        }

        public class ITEMS
        {
            public class BIONIC_BOOSTERS
            {
                public class BOOSTER_EXPLORER
                {
                    public static LocString NAME = UI.FormatAsLink("Dowsing Booster", "BOOSTEREXPLORER");
                    public static LocString DESC = "Grants a Bionic Duplicant the ability to regularly uncover hidden geysers.";
                    public static LocString EFFECT = "Dowsing while Defragmenting";
                }
            }
        }

        public class OPTIONS
        {
            public class CRAFT_AT
            {
                public static LocString NAME = "Can be crafted at the";
            }
            public class WATTAGE
            {
                public static LocString NAME = "Wattage cost for Dowsing while Defragmenting";
            }
            public class STARTING_BOOSTER
            {
                public static LocString NAME = "Can be preinstalled on a new Bionic Duplicant";
            }
            public class CARE_PACKAGE
            {
                public static LocString NAME = "Add to Care Packages";
            }
        }

        internal static void DoReplacement()
        {
            DUPLICANTS.TRAITS.STARTWITHBOOSTER_EXPLORER.NAME = ITEMS.BIONIC_BOOSTERS.BOOSTER_EXPLORER.NAME.text;
            DUPLICANTS.TRAITS.STARTWITHBOOSTER_EXPLORER.DESC = ITEMS.BIONIC_BOOSTERS.BOOSTER_EXPLORER.DESC.text;
            DUPLICANTS.TRAITS.STARTWITHBOOSTER_EXPLORER.SHORT_DESC_TOOLTIP = global::STRINGS.DUPLICANTS.TRAITS.STARTING_BIONIC_BOOSTER_SHARED_DESC_TOOLTIP.text;
            LocString.CreateLocStringKeys(typeof(DUPLICANTS));
            LocString.CreateLocStringKeys(typeof(ITEMS));
            // добавляем строки для опций-чекбоксов
            const string name = "STRINGS.EXPLORERBOOSTER.OPTIONS.{0}.NAME";
            const string tooltip = "STRINGS.EXPLORERBOOSTER.OPTIONS.{0}.TOOLTIP";
            static void AddStrings(string key, string value)
            {
                Strings.Add(string.Format(name, key.ToUpperInvariant()), value);
                Strings.Add(string.Format(tooltip, key.ToUpperInvariant()), string.Empty);
            }
            AddStrings(CraftAt.Basic.ToString(), UI.StripLinkFormatting(BUILDINGS.PREFABS.CRAFTINGTABLE.NAME));
            AddStrings(CraftAt.Advanced.ToString(), UI.StripLinkFormatting(BUILDINGS.PREFABS.ADVANCEDCRAFTINGTABLE.NAME));
            AddStrings(WattageCost.TIER_0.ToString(), POWER_COST.TIER_0.ToString());
            AddStrings(WattageCost.TIER_1.ToString(), POWER_COST.TIER_1.ToString());
            AddStrings(WattageCost.TIER_2.ToString(), POWER_COST.TIER_2.ToString());
            AddStrings(WattageCost.TIER_3.ToString(), POWER_COST.TIER_3.ToString());
        }
    }
}