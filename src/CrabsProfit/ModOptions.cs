using Newtonsoft.Json;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace CrabsProfit
{
    /*
        некоторые соображения:
        крабло живёт 100 циклов (95 взрослый)
        размножается 60 циклов дикий, 2.5 циклов ручной довольный при счастье 10 (почесать + бракен + домег)
        итого, 1 крабло родит до 38 при благоприятных условиях

        электросклизняк жрет руду 60 кг в цикл и живет 100 циклов
        итого ему нужно 6к руды
        
        жеготные дающие руду, дают:
        крокодилло 60 кг в цикл
        коровло   до 250 кг в 10 циклов (стрижка)
        черепахло до 250 кг в 10 циклов (стрижка)

        устрица	  50 кг пеарла в 8 циклов == 6.25 в цикл за 35 кг песка в цикл
        
        чтобы 1 краб прокормил 1 склизьняка оптимально 6к / 38 == 150 руды с краба
        уровнять с коровло и черепахло		 2.5к / 38 == 65 руды с краба
        сделать как устриццу				 625 / 38 == 16,5 пеарла с краба

    */
    internal enum ShellMass
    {
        [Option] mass25 = 25,
        [Option] mass50 = 50,
        [Option] mass75 = 75,
        [Option] mass100 = 100,
        [Option] mass150 = 150,
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

    internal enum SecondaryResource
    {
        [Option] Pearl,
        [Option] Coquina,
    }

    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [Option]
        public ShellMass CrabFreshWater_Shell_Mass { get; set; } = ShellMass.mass75;

        [JsonProperty]
        [Option]
        public BabyShellMassDivider BabyCrabFreshWater_Mass_Divider { get; set; } = BabyShellMassDivider.div4;

        [JsonIgnore]
        public float AdultShellMass => Mathf.Max((float)CrabFreshWater_Shell_Mass, (float)ShellMass.mass25);

        [JsonIgnore]
        public float BabyShellMass => AdultShellMass / Mathf.Max((float)BabyCrabFreshWater_Mass_Divider, 1f);

        [JsonProperty]
        [Option]
        public SecondaryResource SecondaryResource { get; set; } = SecondaryResource.Pearl;

        [JsonIgnore]
        public SimHashes SecondaryOre
        {
            get
            {
                switch (SecondaryResource)
                {
                    case SecondaryResource.Coquina:
                        return SimHashes.Coquina;
                    case SecondaryResource.Pearl:
                    default:
                        return SimHashes.Pearl;
                }
            }
        }

        [Option]
        [Limit(0, (int)ShellMass.mass25)]
        public int SecondaryMass { get; set; } = 15;

        [JsonProperty]
        [Option]
        public bool DisableRandom { get; set; } = false;

        public class OreWeights
        {
            private const int max = 10;

            [JsonIgnore]
            [Option]
            public LocText Base_Ore => null;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int AluminumOre { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int Cuprite { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int GoldAmalgam { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int IronOre { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int Galena { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int Rust { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int Wolframite { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int Cobaltite { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int Cinnabar { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int NickelOre { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int ZincOre { get; set; } = 5;

            [JsonIgnore]
            [Option]
            [RequireDLC(DlcManager.EXPANSION1_ID)]
            public LocText DLC1_Ore => null;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            [RequireDLC(DlcManager.EXPANSION1_ID)]
            public int UraniumOre { get; set; } = 1;

            [JsonIgnore]
            [Option]
            public LocText Exotic_Ore => null;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int Electrum { get; set; } = 0;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int FoolsGold { get; set; } = 0;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            public int Radium { get; set; } = 0;

            [JsonIgnore]
            [Option]
            [RequireMod("RonivansLegacy_ChemicalProcessing")]
            public LocText Chemical_Processing_Ore => null;

            [JsonProperty]
            [Option]
            [Limit(0, max)]
            [RequireMod("RonivansLegacy_ChemicalProcessing")]
            public int ArgentiteOre { get; set; } = 0;
        }

        [JsonProperty]
        [Option]
        public OreWeights Ore_Weights { get; set; } = new OreWeights();
    }
}
