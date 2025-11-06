using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace HEPBridgeInsulationTile
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        public enum Research
        {
            [Option]
            AdvancedNuclearResearch,
            [Option]
            NuclearStorage,
            [Option]
            NuclearRefinement
        }

        [JsonProperty]
        [Option]
        public Research research_mod { get; set; } = Research.NuclearStorage;

        [JsonProperty]
        [Option]
        public bool use_old_anim { get; set; } = false;
    }
}