using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace OilWellCapBugFix
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    public class OilWellCapBugFixOptions : BaseOptions<OilWellCapBugFixOptions>
    {
        [JsonProperty]
        [Option]
        public bool AllowDepressurizeWhenOutOfWater { get; set; } = true;
    }
}
