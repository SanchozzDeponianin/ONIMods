using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace MooDiet
{
    public enum Beckoning
    {
        [Option] Zero,
        [Option] Quarter,
        [Option] Half,
        [Option] Full = 4,
    }

    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class MooDietOptions : BaseOptions<MooDietOptions>
    {
        public sealed class FlowerDiet
        {
            [JsonProperty]
            [Option]
            [Limit(6, 120)]
            public int lily_per_cow { get; set; } = 30;

            // регулировку газа пока скроем
            [JsonProperty]
            //[Option]
            public int gas { get; set; } = 100;

            [JsonIgnore]
            public float gas_multiplier => gas / 100f;

            [JsonProperty]
            [Option]
            public Beckoning beckoning { get; set; } = Beckoning.Quarter;

            [JsonIgnore]
            public float beckoning_penalty => 1f - (float)beckoning / (float)Beckoning.Full;
        }

        [JsonProperty]
        [Option]
        public FlowerDiet flower_diet { get; set; } = new FlowerDiet();

        public sealed class PalmeraDiet
        {
            [JsonIgnore]
            [Option]
            public LocText palmera_label { get; set; }

            [JsonIgnore]
            [Option]
            public System.Action<object> palmera_button =>
                delegate (object _) { App.OpenWebURL("https://steamcommunity.com/sharedfiles/filedetails/?id=1823182964"); };

            [JsonProperty]
            [Option]
            [Limit(1, 10)]
            public int palmera_per_cow { get; set; } = 2;

            [JsonProperty]
            [Option]
            public bool eat_palmera { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool eat_berry { get; set; } = true;
        }

        [JsonProperty]
        [Option]
        public PalmeraDiet palmera_diet { get; set; } = new PalmeraDiet();
    }
}
