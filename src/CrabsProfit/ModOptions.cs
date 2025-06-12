using Newtonsoft.Json;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace CrabsProfit
{
    /*
        некоторые соображения:
        краб живёт 100 циклов (95 взрослый)
        размножается 60 циклов дикий, 6 циклов ручной довольный
        итого, 1 краб родит до 15 при благоприятных условиях
        электрослизняк жрет руду 60 кг в цикл и живет 100 циклов
        итого ему нужно 6к руды
        чтобы 1 краб прокормил 1 слизня - оптимально 6к / 15 == 400 руды с краба
        и пусть руда с крабёнков будет бонусом
    */
    internal enum ShellMass
    {
        [Option] mass0 = 0,
        [Option] mass50 = 50,
        [Option] mass100 = 100,
        [Option] mass200 = 200,
        [Option] mass300 = 300,
        [Option] mass400 = 400,
        [Option] mass500 = 500,
        [Option] mass600 = 600,
        [Option] mass700 = 700,
        [Option] mass800 = 800,
        [Option] mass900 = 900,
        [Option] mass1000 = 1000,
    }

    internal enum BabyShellMassDivider
    {
        [Option] div2 = 2,
        [Option] div3 = 3,
        [Option] div4 = 4,
        [Option] div5 = 5,
        [Option] div6 = 6,
        [Option] div7 = 7,
        [Option] div8 = 8,
        [Option] div9 = 9,
        [Option] div10 = 10,
    }

    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [Option]
        [Limit(0, 3)]
        public int Crab_Meat { get; set; } = 1;

        [JsonProperty]
        [Option]
        [Limit(0, 3)]
        public int CrabWood_Meat { get; set; } = 1;

        [JsonProperty]
        [Option]
        public ShellMass CrabFreshWater_Shell_Mass { get; set; } = ShellMass.mass400;

        [JsonProperty]
        [Option]
        public BabyShellMassDivider BabyCrabFreshWater_Mass_Divider { get; set; } = BabyShellMassDivider.div4;

        // если в настройках задать нулевую массу - то пусть масса шкорлупы останется минимально ненулевой
        // просто не добавлять дроп
        [JsonIgnore]
        public float AdultShellMass => Mathf.Max((float)CrabFreshWater_Shell_Mass, (float)ShellMass.mass50);

        [JsonIgnore]
        public float BabyShellUnits => 1f / Mathf.Max((float)BabyCrabFreshWater_Mass_Divider, 1f);

        public class OreWeights
        {
            private const int min_normal = 1, min_rare = 0, max = 10;

            [JsonIgnore]
            [Option]
            public LocText Base_Ore => null;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int AluminumOre { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int Cuprite { get; set; } = 7;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int GoldAmalgam { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int IronOre { get; set; } = 6;

            [JsonProperty]
            [Option]
            [Limit(min_rare, max)]
            public int Lead { get; set; } = 2;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int Rust { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int Wolframite { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int Cobaltite { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int Cinnabar { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int NickelOre { get; set; } = 5;

            [JsonIgnore]
            [Option]
            public LocText DLC1_Ore => null;

            [JsonProperty]
            [Option]
            [Limit(min_rare, max)]
            public int UraniumOre { get; set; } = 1;

            [JsonIgnore]
            [Option]
            public LocText Exotic_Ore => null;

            [JsonProperty]
            [Option]
            [Limit(min_rare, max)]
            public int Electrum { get; set; } = 0;

            [JsonProperty]
            [Option]
            [Limit(min_rare, max)]
            public int FoolsGold { get; set; } = 0;

            [JsonProperty]
            [Option]
            [Limit(min_rare, max)]
            public int Radium { get; set; } = 0;

            [JsonIgnore]
            [Option]
            public LocText Chemical_Processing_Ore => null;

            [JsonProperty]
            [Option]
            [Limit(min_rare, max)]
            public int ArgentiteOre { get; set; } = 0;

            [JsonProperty]
            [Option]
            [Limit(min_rare, max)]
            public int AurichalciteOre { get; set; } = 0;
        }

        [JsonProperty]
        [Option]
        public OreWeights Ore_Weights { get; set; } = new OreWeights();
    }
}
