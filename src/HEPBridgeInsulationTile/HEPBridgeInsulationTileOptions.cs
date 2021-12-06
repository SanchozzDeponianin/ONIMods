using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace HEPBridgeInsulationTile
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    [RestartRequired]
    public class HEPBridgeInsulationTileOptions : BaseOptions<HEPBridgeInsulationTileOptions>
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

        [JsonIgnore]
        private Research _research_klei = Research.NuclearRefinement;

        [JsonProperty]
        [Option]
        public Research research_klei
        {
            get => _research_klei;
            set
            {
                if (value > _research_mod)
                    _research_mod = value;
                _research_klei = value;
            }
        }
        [JsonIgnore]
        private Research _research_mod = Research.NuclearRefinement;

        [JsonProperty]
        [Option]
        public Research research_mod
        {
            get => _research_mod;
            set => _research_mod = (value < _research_klei) ? _research_klei : value;
        }
    }
}