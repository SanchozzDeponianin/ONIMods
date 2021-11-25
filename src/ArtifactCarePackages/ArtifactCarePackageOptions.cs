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
        [Option]
        [Limit(1, 500)]
        public int CyclesUntilTier0 { get; set; } = 150;
        [JsonProperty]
        [Option]
        [Limit(1, 200)]
        public int CyclesUntilTierNext { get; set; } = 25;
        [JsonProperty]
        [Option]
        [Limit(1, 30)]
        public int RandomArtifactDropTableSlots { get; set; } = 5;
        [JsonProperty]
        [Option]
        public bool DynamicProbability { get; set; } = true;
    }
}
