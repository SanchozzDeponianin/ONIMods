using System.Collections.Generic;
using System.Runtime.Serialization;
using KSerialization;


namespace VaricolouredBalloons
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class VaricolouredBalloonsHelper : KMonoBehaviour, ISaveLoadable
    {
        const string NEW_BALLOON_ANIM = "varicoloured_balloon_kanim";
        const string BALLOON_SYMBOL = "body";

        private static string[] BalloonSymbols = new string[] { "body" };

        [Serialize]
        private int receiverballoonsymbolidx;

        public int ArtistBalloonSymbolIdx { get; private set; }
        public int ReceiverBalloonSymbolIdx { get => receiverballoonsymbolidx; }

        public void SetArtistBalloonSymbolIdx(int value)
        {
            ArtistBalloonSymbolIdx = Clamp(value);
        }

        public void SetReceiverBalloonSymbolIdx(int value)
        {
            receiverballoonsymbolidx = Clamp(value);
        }

        // собираем названия символов в загруженной анимации баллонов
        public static void Initialize()
        {
            KAnim.Build.Symbol[] symbols = Assets.GetAnim(NEW_BALLOON_ANIM)?.GetData().build.symbols;
            List<string> symbolnames = new List<string>();
            for (int i = 0; i < symbols.Length; i++)
            {
                string text = HashCache.Get().Get(symbols[i].hash);
                if (!text.IsNullOrWhiteSpace() && text != BALLOON_SYMBOL)
                {
                    symbolnames.Add(text);
                }
            }
            if (symbolnames.Count > 0)
            {
                BalloonSymbols = symbolnames.ToArray();
            }
        }

        [OnDeserialized]
        private void OnDeserialized()
        {
            receiverballoonsymbolidx = Clamp(receiverballoonsymbolidx);
        }

        public static int GetRandomSymbolIdx()
        {
            return UnityEngine.Random.Range(0, BalloonSymbols.Length);
        }

        private static int Clamp(int idx)
        {
            return (idx < 0) ? GetRandomSymbolIdx() : (idx % BalloonSymbols.Length);
        }

        // переопределение символа, по индексу в массиве анимации
        public static void ApplySymbolOverrideByIdx(SymbolOverrideController symbolOverrideController, int idx)
        {
            idx = Clamp(idx);
            string symbolname = BalloonSymbols[idx];
            KAnim.Build.Symbol symbol = Assets.GetAnim(NEW_BALLOON_ANIM)?.GetData().build.GetSymbol(symbolname);
            if (symbol != null)
            {
                symbolOverrideController?.AddSymbolOverride(BALLOON_SYMBOL, symbol, 4);
            }
#if DEBUG
            else
            {
                Debug.LogWarning($"Could not find anim '{NEW_BALLOON_ANIM}' symbol '{symbolname}'");
            }
#endif
        }
    }
}
