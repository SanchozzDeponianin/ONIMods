using TUNING;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace AttributeRestrictions
{
    internal enum ManualGeneratorAttribute
    {
        [Option] Machinery,
        [Option] Athletics,
    }

    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [Option(Format = "F0")]
        [Limit(20, 9999)]
        public int max_level { get; set; } = DUPLICANTSTATS.ATTRIBUTE_LEVELING.MAX_GAINED_ATTRIBUTE_LEVEL;

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        [Option]
        public ManualGeneratorAttribute attribute { get; set; }
    }
}
