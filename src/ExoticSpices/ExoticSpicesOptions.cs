using Newtonsoft.Json;
using TUNING;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace ExoticSpices
{
    internal enum EmitMass
    {
        [Option] x0_25 = -2,
        [Option] x0_5 = -1,
        [Option] x1 = 0,
        [Option] x2 = 1,
        [Option] x4 = 2,
    }

    internal enum EmitGas
    {
        [Option] Methane,
        [Option] ContaminatedOxygen,
    }

    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    [RestartRequired]
    internal class ExoticSpicesOptions : BaseOptions<ExoticSpicesOptions>
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class PhosphoRufusSpice
        {
            [JsonProperty]
            [Option]
            [Limit(0, 50)]
            public int joy_reaction_chance { get; set; } = 15;
            [JsonProperty]
            [Option]
            [Limit(1, 6)]
            public float range { get; set; } = 3f;
            [JsonProperty]
            [Option(Format = "F0")]
            [Limit(100, 10000)]
            public int lux { get; set; } = LIGHT2D.LIGHTBUG_LUX;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class GassyMooSpice
        {
            [JsonProperty]
            [Option]
            [Limit(0, 50)]
            public int joy_reaction_chance { get; set; } = 20;

            [JsonProperty]
            [Option]
            [Limit(1, 15)]
            public int attribute_buff { get; set; } = 8;

            [JsonProperty]
            [Option]
            public EmitMass emit_mass { get; set; } = EmitMass.x1;

            [JsonProperty]
            [Option]
            public EmitGas emit_gas { get; set; } = EmitGas.Methane;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class ZombieSpice
        {
            [JsonProperty]
            [Option]
            [Limit(0, 50)]
            public int joy_reaction_chance { get; set; } = 25;

            [JsonProperty]
            [Option]
            [Limit(0, 100)]
            public int stamina_buff { get; set; } = 60;
        }

        [JsonProperty]
        [Option]
        public PhosphoRufusSpice phospho_rufus_spice { get; set; } = new PhosphoRufusSpice();

        [JsonProperty]
        [Option]
        public GassyMooSpice gassy_moo_spice { get; set; } = new GassyMooSpice();

        [JsonProperty]
        [Option]
        public ZombieSpice zombie_spice { get; set; } = new ZombieSpice();

        [JsonProperty]
        [Option]
        [Limit(0, 5)]
        public int carepackage_seeds_amount { get; set; } = 3;
    }
}
