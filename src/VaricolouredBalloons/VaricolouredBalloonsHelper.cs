using System.Linq;
using System.Runtime.Serialization;

using KSerialization;

using PeterHan.PLib;

namespace VaricolouredBalloons
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class VaricolouredBalloonsHelper : KMonoBehaviour, ISaveLoadable
    {
        const string NEW_BALLOON_ANIM = "varicoloured_balloon_kanim";
        const string BALLOON_SYMBOL = "body";
        const int OVERRIDE_PRIORITY = 4;

        private static string[] BalloonSymbolNames = new string[] { BALLOON_SYMBOL };

        [Serialize]
        private uint receiverballoonsymbolidx;

        internal uint ArtistBalloonSymbolIdx { get; private set; }
        internal uint ReceiverBalloonSymbolIdx { get => receiverballoonsymbolidx; }

        internal BalloonFX.Instance fx;

        internal void SetArtistBalloonSymbolIdx(uint value)
        {
            ArtistBalloonSymbolIdx = Clamp(value);
        }

        internal void SetReceiverBalloonSymbolIdx(uint value)
        {
            receiverballoonsymbolidx = Clamp(value);
        }

        [OnDeserialized]
        private void OnDeserialized()
        {
            receiverballoonsymbolidx = Clamp(receiverballoonsymbolidx);
        }

        // собираем названия символов в загруженной анимации баллонов
        internal static void Initialize()
        {
            KAnim.Build.Symbol[] symbols = Assets.GetAnim(NEW_BALLOON_ANIM)?.GetData().build.symbols;
            if (symbols == null)
            {
                PUtil.LogError($"Missing Anim: '{NEW_BALLOON_ANIM}'.");
                return;
            }

            string[] symbolnames = symbols?.Select(symbol => HashCache.Get().Get(symbol.hash))?.Where(symbolname => symbolname?.StartsWith(BALLOON_SYMBOL) ?? false)?.ToArray();

            if (symbolnames != null && symbolnames.Length > 0)
            {
                BalloonSymbolNames = symbolnames;
            }
            else
            {
                PUtil.LogError($"Collected 0 '{BALLOON_SYMBOL}' symbols from anim '{NEW_BALLOON_ANIM}'.");
            }
        }

        internal static uint GetRandomSymbolIdx()
        {
            return (uint)UnityEngine.Random.Range(0, BalloonSymbolNames.Length);
        }

        private static uint Clamp(uint idx)
        {
            return (idx % (uint)(BalloonSymbolNames.Length));
        }

        // переопределение символа, по индексу в массиве анимации
        internal static void ApplySymbolOverrideByIdx(SymbolOverrideController symbolOverrideController, uint idx)
        {
            if (symbolOverrideController == null)
            {
#if DEBUG
                PUtil.LogWarning($"SymbolOverrideController is null");
#endif
                return;
            }

            if (BalloonSymbolNames == null || BalloonSymbolNames.Length == 0)
            {
#if DEBUG
                PUtil.LogWarning($"BalloonSymbols is null or empty.");
#endif
                return;
            }

            idx = Clamp(idx);
            string symbolname = BalloonSymbolNames[idx];
            KAnim.Build.Symbol symbol = Assets.GetAnim(NEW_BALLOON_ANIM)?.GetData().build.GetSymbol(symbolname);
            if (symbol != null)
            {
                symbolOverrideController.AddSymbolOverride(BALLOON_SYMBOL, symbol, OVERRIDE_PRIORITY);
            }
#if DEBUG
            else
            {
                PUtil.LogWarning($"Could not find anim '{NEW_BALLOON_ANIM}' symbol '{symbolname}'");
            }
#endif
        }
    }
}
