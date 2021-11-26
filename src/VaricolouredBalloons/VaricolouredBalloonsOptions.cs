using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace VaricolouredBalloons
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true)]
    internal sealed class VaricolouredBalloonsOptions : BaseOptions<VaricolouredBalloonsOptions>
    {
        [JsonProperty]
        [Option]
        public bool DestroyFXAfterEffectExpired { get; set; } = false;
    }
}
