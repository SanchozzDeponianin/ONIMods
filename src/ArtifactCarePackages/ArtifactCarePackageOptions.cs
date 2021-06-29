using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace ArtifactCarePackages
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    internal class ArtifactCarePackageOptions : BaseOptions<ArtifactCarePackageOptions>
    {
        [JsonProperty]
        [Option("ArtifactCarePackages.STRINGS.OPTIONS.CYCLESUNTILTIER0.TITLE")]
        [Limit(1, 500)]
        public int CyclesUntilTier0 { get; set; }
        [JsonProperty]
        [Option("ArtifactCarePackages.STRINGS.OPTIONS.CYCLESUNTILTIERNEXT.TITLE")]
        [Limit(1, 200)]
        public int CyclesUntilTierNext { get; set; }
        [JsonProperty]
        [Option(
            "ArtifactCarePackages.STRINGS.OPTIONS.RANDOMARTIFACTDROPTABLESLOTS.TITLE",
            "ArtifactCarePackages.STRINGS.OPTIONS.RANDOMARTIFACTDROPTABLESLOTS.TOOLTIP")]
        [Limit(1, 30)]
        public int RandomArtifactDropTableSlots { get; set; }

        public ArtifactCarePackageOptions()
        {
            CyclesUntilTier0 = 150;
            CyclesUntilTierNext = 25;
            RandomArtifactDropTableSlots = 5;
        }
    }
}
