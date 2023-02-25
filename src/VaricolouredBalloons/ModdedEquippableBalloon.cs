using System.Runtime.Serialization;
using KSerialization;

namespace VaricolouredBalloons
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class ModdedEquippableBalloon : KMonoBehaviour, ISaveLoadable
    {
        [Serialize]
        public string facadeAnim;

        [Serialize]
        public string symbolID;

        [OnDeserialized]
        private void OnDeserialized()
        {
            if (!string.IsNullOrEmpty(facadeAnim)
                && !string.IsNullOrEmpty(symbolID)
                && Assets.TryGetAnim(facadeAnim, out var kAnimFile)
                && kAnimFile.GetData().build.GetSymbol(symbolID) != null)
            {
                return;
            }
            facadeAnim = null;
            symbolID = null;
        }
    }
}
