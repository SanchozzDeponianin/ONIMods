using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace CrabsProfit
{
    internal enum ShellMass
    {
        [Option] mass50 = 50,
        [Option] mass100 = 100,
        [Option] mass200 = 200,
        [Option] mass300 = 300,
        [Option] mass400 = 400,
        [Option] mass500 = 500
    }

    internal enum BabyShellMassDivider
    {
        [Option] div2 = 2,
        [Option] div5 = 5,
        [Option] div10 = 10
    }

    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    [RestartRequired]
    internal sealed class CrabsProfitOptions : BaseOptions<CrabsProfitOptions>
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
        public ShellMass CrabFreshWater_Shell_Mass { get; set; } = ShellMass.mass200;

        [JsonProperty]
        [Option]
        public BabyShellMassDivider BabyCrabFreshWater_Mass_Divider { get; set; } = BabyShellMassDivider.div5;

        public class OreWeights
        {
            private const int min_normal = 1, min_rare = 0, max = 10;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int AluminumOre { get; set; } = 5;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int Cobaltite { get; set; } = 8;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int Cuprite { get; set; } = 7;

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
            [Limit(min_rare, max)]
            public int Mercury { get; set; } = 0;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int Rust { get; set; } = 7;

            [JsonProperty]
            [Option]
            [Limit(min_rare, max)]
            public int UraniumOre { get; set; } = 1;

            [JsonProperty]
            [Option]
            [Limit(min_normal, max)]
            public int Wolframite { get; set; } = 4;
        }

        [JsonProperty]
        [Option]
        public OreWeights Ore_Weights { get; set; } = new OreWeights();
    }
}
