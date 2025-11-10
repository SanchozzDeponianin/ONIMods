using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace ReBuildableAETN
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonObject(MemberSerialization.OptIn)]
        internal sealed class CarePackages
        {
            [JsonProperty]
            [Option]
            public bool Enabled { get; set; } = true;

            [JsonProperty]
            [Option]
            [Limit(0, 500)]
            public int MinCycle { get; set; } = 100;

            [JsonProperty]
            [Option]
            public bool RequireDiscovered { get; set; } = true;
        }

        [JsonObject(MemberSerialization.OptIn)]
        internal sealed class GravitasPOIChances
        {
            [JsonProperty]
            [Option]
            [Limit(0, 100)]
            public int RarePOIChance { get; set; } = 100;

            [JsonProperty]
            [Option]
            [Limit(0, 40)]
            public int LockerPOIChance { get; set; } = 10;
        }

        [JsonObject(MemberSerialization.OptIn)]
        internal sealed class VanillaPlanetChances
        {
            [JsonProperty]
            [Option]
            public bool Enabled { get; set; } = true;

            [JsonProperty]
            [Option]
            [Limit(0, 40)]
            public int IcyDwarfChance { get; set; } = 15;

            [JsonProperty]
            [Option]
            [Limit(0, 40)]
            public int IceGiantChance { get; set; } = 35;
        }

        [JsonObject(MemberSerialization.OptIn)]
        internal sealed class SpaceOutPOIChances
        {
            [JsonProperty]
            [Option]
            public bool Enabled { get; set; } = true;

            [JsonProperty]
            [Option]
            [Limit(0, 30)]
            public int SpacePOIChance { get; set; } = 10;
        }

        [JsonProperty]
        [Option]
        public bool AddLogicPort { get; set; } = true;

        [JsonProperty]
        [Option]
        public CarePackages CarePackage { get; set; } = new CarePackages();

        [JsonProperty]
        [Option]
        public GravitasPOIChances GravitasPOIChance { get; set; } = new GravitasPOIChances();

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.VANILLA_ID, true)]
        [RequireDLC(DlcManager.EXPANSION1_ID, false)]
        public VanillaPlanetChances VanillaPlanetChance { get; set; } = new VanillaPlanetChances();

        [JsonProperty]
        [Option]
        [RequireDLC(DlcManager.EXPANSION1_ID)]
        public SpaceOutPOIChances SpaceOutPOIChance { get; set; } = new SpaceOutPOIChances();
    }
}
