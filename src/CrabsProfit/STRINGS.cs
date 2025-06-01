using System;
using System.Collections.Generic;
using STRINGS;
using SanchozzONIMods.Lib;

namespace CrabsProfit
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        private const string CRAB = "{CRAB}";
        private const string CRABWOOD = "{CRABWOOD}";
        private const string BABYCRABFRESHWATER = "{BABYCRABFRESHWATER}";

        public class ITEMS
        {
            public class INDUSTRIAL_PRODUCTS
            {
                public class BABY_CRAB_SHELL
                {
                    public class VARIANT_FRESH_WATER
                    {
                        public static LocString NAME = "Small Sanishell Molt";
                        public static LocString DESC = $"Can be crushed to produce {UI.FormatAsKeyWord(RANDOMORE.NAME)}.";
                    }
                }
                public class CRAB_SHELL
                {
                    public class VARIANT_FRESH_WATER
                    {
                        public static LocString NAME = "Sanishell Molt";
                        public static LocString DESC = BABY_CRAB_SHELL.VARIANT_FRESH_WATER.DESC;
                    }
                }
                public class RANDOMORE
                {
                    public static LocString NAME = "Random Ore";
                    public static LocString DESC = "It magically turns into a randomly chosen Metallic Ore.";
                }
            }
        }

        public class OPTIONS
        {
            public class CRAB_MEAT
            {
                public static LocString NAME = $"{CRAB} meat amount, units";
            }
            public class CRABWOOD_MEAT
            {
                public static LocString NAME = $"{CRABWOOD} meat amount, units";
            }
            public class BABYCRABFRESHWATER_MEAT
            {
                public static LocString NAME = $"{BABYCRABFRESHWATER} meat amount, units";
            }
            public class CRABFRESHWATER_SHELL_MASS
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord(ITEMS.INDUSTRIAL_PRODUCTS.CRAB_SHELL.VARIANT_FRESH_WATER.NAME)} mass, kg";
                public static LocString TOOLTIP = "If you set this to zero, the new molt will not drop,\nand the previously dropped molt will have a mass of 50 kg.";
            }
            public class BABYCRABFRESHWATER_MASS_DIVIDER
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord(ITEMS.INDUSTRIAL_PRODUCTS.BABY_CRAB_SHELL.VARIANT_FRESH_WATER.NAME)} is less X times";
            }
            public class ORE_WEIGHTS
            {
                public static LocString CATEGORY = "Random Ore Chances Table";
            }
            public class BASE_ORE
            {
                public static LocString NAME = "Basic Ores";
            }
            public class DLC1_ORE
            {
                public static LocString NAME = "Spaced Out! Ores";
            }
            public class EXOTIC_ORE
            {
                public static LocString NAME = "Exotic Ores";
                public static LocString TOOLTIP = "These Ores exist in the game, but are not used.\nThey can be used by some mods.";
            }
            public class CHEMICAL_PROCESSING_ORE
            {
                public static LocString NAME = $"Ronivan's {UI.FormatAsKeyWord("Chemical Processing")} Ores";
            }
            public class ARGENTITEORE
            {
                public static LocString NAME = "Silver Ore";
            }
            public class AURICHALCITEORE
            {
                public static LocString NAME = "Zinc Ore";
            }
        }

        internal static void DoReplacement()
        {
            var dictionary = new Dictionary<string, string>()
            {
                {CRAB, UI.FormatAsKeyWord(CREATURES.SPECIES.CRAB.NAME) },
                {CRABWOOD, UI.FormatAsKeyWord(CREATURES.SPECIES.CRAB.VARIANT_WOOD.NAME) },
                {BABYCRABFRESHWATER, UI.FormatAsKeyWord(CREATURES.SPECIES.CRAB.VARIANT_FRESH_WATER.BABY.NAME) },
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
            // добавляем строки для опций-чекбоксов
            var name = "STRINGS.CRABSPROFIT.OPTIONS.{0}.NAME";
            var tooltip = "STRINGS.CRABSPROFIT.OPTIONS.{0}.TOOLTIP";
            void AddStrings(string key, string value)
            {
                Strings.Add(string.Format(name, key), value);
                Strings.Add(string.Format(tooltip, key), string.Empty);
            }
            foreach (var @enum in (ShellMass[])Enum.GetValues(typeof(ShellMass)))
                AddStrings(@enum.ToString().ToUpperInvariant(), ((int)@enum).ToString());
            foreach (var @enum in (BabyShellMassDivider[])Enum.GetValues(typeof(BabyShellMassDivider)))
                AddStrings(@enum.ToString().ToUpperInvariant(), ((int)@enum).ToString());
            // добавляем строки для списка руды
            var element = "STRINGS.ELEMENTS.{0}.NAME";
            foreach (var info in typeof(CrabsProfitOptions.OreWeights).GetProperties())
            {
                var id = info.Name.ToUpperInvariant();
                if (Strings.TryGet(string.Format(element, id), out var ore_name))
                    AddStrings(id, UI.StripLinkFormatting(ore_name));
            }
        }
    }
}
