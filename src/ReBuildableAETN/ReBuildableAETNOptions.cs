using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace ReBuildableAETN
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal class ReBuildableAETNOptions : BaseOptions<ReBuildableAETNOptions>
    {
        [JsonObject(MemberSerialization.OptIn)]
        internal class CarePackages
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
        internal class GravitasPOIChances
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
        internal class VanillaPlanetChances
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
        internal class SpaceOutPOIChances
        {
            [JsonProperty]
            [Option]
            public bool Enabled { get; set; } = true;

            [JsonProperty]
            [Option]
            [Limit(0, 30)]
            public int SpacePOIChance { get; set; } = 15;
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
        public virtual VanillaPlanetChances VanillaPlanetChance { get; set; } = new VanillaPlanetChances();

        [JsonProperty]
        public virtual SpaceOutPOIChances SpaceOutPOIChance { get; set; } = new SpaceOutPOIChances();
    }

    internal class ReBuildableAETNVanillaOptions : ReBuildableAETNOptions
    {
        [Option]
        public override VanillaPlanetChances VanillaPlanetChance { get => base.VanillaPlanetChance; set => base.VanillaPlanetChance = value; }
    }

    internal class ReBuildableAETNSpaceOutOptions : ReBuildableAETNOptions
    {
        [Option]
        public override SpaceOutPOIChances SpaceOutPOIChance { get => base.SpaceOutPOIChance; set => base.SpaceOutPOIChance = value; }
    }
}
