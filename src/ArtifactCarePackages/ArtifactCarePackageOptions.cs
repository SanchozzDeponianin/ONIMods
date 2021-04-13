using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace ArtifactCarePackages
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Artifacts in Care Packages")]
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
            if (DlcManager.IsExpansion1Active())
            {
                CyclesUntilTier0 = 50;
                CyclesUntilTierNext = 15;
                RandomArtifactDropTableSlots = 5;
            }
            else
            {
                CyclesUntilTier0 = 300;
                CyclesUntilTierNext = 50;
                RandomArtifactDropTableSlots = 1;
            }
        }
    }
}
