using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace WhereMyLoot
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [Option]
        public Protect protect { get; set; } = new Protect();

        [JsonObject(MemberSerialization.OptIn)]
        public class Protect
        {
            [JsonProperty]
            [Option]
            public bool megabraintank { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool creaturemanipulator { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool fossildig { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool morbrovermaker { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool hijackedheadquarters { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool geneshuffler { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool massiveheatsink { get; set; } = true;

            [JsonProperty]
            [Option]
            [RequireDLC(DlcManager.EXPANSION1_ID)]
            public bool satellites { get; set; } = true;
        }
    }
}
