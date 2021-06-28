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
        [Option("VaricolouredBalloons.STRINGS.OPTIONS.DESTROYFXAFTEREFFECTEXPIRED.TITLE", "VaricolouredBalloons.STRINGS.OPTIONS.DESTROYFXAFTEREFFECTEXPIRED.TOOLTIP")]
        public bool DestroyFXAfterEffectExpired { get; set; }

        public VaricolouredBalloonsOptions()
        {
            DestroyFXAfterEffectExpired = false;
        }
    }
}
