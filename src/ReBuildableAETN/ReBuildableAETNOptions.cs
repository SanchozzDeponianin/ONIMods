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
            [Option("ReBuildableAETN.STRINGS.OPTIONS.CARE_PACKAGES.ENABLED.TITLE")]
            public bool Enabled { get; set; } = true;

            [JsonProperty]
            [Option("ReBuildableAETN.STRINGS.OPTIONS.CARE_PACKAGES.MIN_CYCLE.TITLE")]
            [Limit(0, 500)]
            public int MinCycle { get; set; } = 100;

            [JsonProperty]
            [Option("ReBuildableAETN.STRINGS.OPTIONS.CARE_PACKAGES.REQUIRE_DISCOVERED.TITLE", "ReBuildableAETN.STRINGS.OPTIONS.CARE_PACKAGES.REQUIRE_DISCOVERED.TOOLTIP")]
            public bool RequireDiscovered { get; set; } = true;
        }

        [JsonProperty]
        [Option("ReBuildableAETN.STRINGS.OPTIONS.ADD_LOGIC_PORT.TITLE")]
        public bool AddLogicPort { get; set; } = true;

        [JsonProperty]
        [Option("xxxxx", null, "ReBuildableAETN.STRINGS.OPTIONS.CARE_PACKAGES.TITLE")]
        public CarePackages CarePackage { get; set; } = new CarePackages();
    }
}
