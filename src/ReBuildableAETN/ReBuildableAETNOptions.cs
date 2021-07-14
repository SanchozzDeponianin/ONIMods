using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace ReBuildableAETN
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
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
        internal class VanillaPlanets
        {
            [JsonProperty]
            [Option]
            public bool Enabled { get; set; } = true;

            [JsonProperty]
            [Option]
            [Limit(0, 40)]
            public int IcyDwarfChance { get; set; } = 10;

            [JsonProperty]
            [Option]
            [Limit(0, 40)]
            public int IceGiantChance { get; set; } = 30;
        }

        [JsonProperty]
        [Option]
        public bool AddLogicPort { get; set; } = true;

        [JsonProperty]
        [Option]
        public CarePackages CarePackage { get; set; } = new CarePackages();

        [JsonProperty]
        public virtual VanillaPlanets VanillaPlanet { get; set; } = new VanillaPlanets();
    }

    internal class ReBuildableAETNVanillaOptions : ReBuildableAETNOptions
    {
        [Option]
        public override VanillaPlanets VanillaPlanet { get => base.VanillaPlanet; set => base.VanillaPlanet = value; }
    }

    internal class ReBuildableAETNSpaceOutOptions : ReBuildableAETNOptions
    {

    }
}
