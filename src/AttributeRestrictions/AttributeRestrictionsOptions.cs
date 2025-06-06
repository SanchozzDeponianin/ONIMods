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
    [RestartRequired]

    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        [Option]
        public ManualGeneratorAttribute attribute { get; set; }
    }
}
