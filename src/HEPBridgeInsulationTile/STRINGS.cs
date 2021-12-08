using STRINGS;
using static STRINGS.BUILDINGS.PREFABS;
using static STRINGS.RESEARCH.TECHS;
using SanchozzONIMods.Lib;

namespace HEPBridgeInsulationTile
{
    public class STRINGS
    {
        private const string HEPBRIDGETILE_NAME = "{HEPBRIDGETILE_NAME}";
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class HIGHENERGYPARTICLEWALLBRIDGEREDIRECTOR
                {
                    public static LocString NAME = UI.FormatAsLink($"Insulated {HEPBRIDGETILE_NAME}", HEPBridgeInsulationTileConfig.ID);
                    public static LocString DESC = "";
                    public static LocString EFFECT = "";
                }
            }
        }
        public class OPTIONS
        {
            public class RESEARCH_KLEI
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord(HEPBRIDGETILE_NAME)} require Research";
                public static LocString TOOLTIP = "";
            }
            public class RESEARCH_MOD
            {
                public static LocString NAME = $"{UI.FormatAsKeyWord($"Insulated {HEPBRIDGETILE_NAME}")} require Research";
                public static LocString TOOLTIP = $"The Options UI the is imperfect.\nIn-game, {UI.FormatAsKeyWord($"Insulated {HEPBRIDGETILE_NAME}")} \nwill require Research equal to or higher \nthan {UI.FormatAsKeyWord(HEPBRIDGETILE_NAME)}.";
            }
            public class NUCLEARREFINEMENT
            {
                public static LocString NAME = "";
            }
            public class NUCLEARSTORAGE
            {
                public static LocString NAME = "";
            }
            public class ADVANCEDNUCLEARRESEARCH
            {
                public static LocString NAME = "";
            }
        }

        internal static void DoReplacement()
        {
            string name = UI.StripLinkFormatting(HEPBRIDGETILE.NAME);
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), new System.Collections.Generic.Dictionary<string, string>
            {{HEPBRIDGETILE_NAME, name }});
            BUILDINGS.PREFABS.HIGHENERGYPARTICLEWALLBRIDGEREDIRECTOR.DESC.ReplaceText($"{HEPBRIDGETILE.DESC.text}\n{INSULATEDWIRE.DESC.text}");
            string effect = HEPBRIDGETILE.EFFECT.text;
            int i = INSULATIONTILE.EFFECT.text.IndexOf("\n\n");
            if (i >= 0)
                effect += INSULATIONTILE.EFFECT.text.Substring(i);
            BUILDINGS.PREFABS.HIGHENERGYPARTICLEWALLBRIDGEREDIRECTOR.EFFECT.ReplaceText(effect);
            OPTIONS.NUCLEARREFINEMENT.NAME.ReplaceText(UI.StripLinkFormatting(NUCLEARREFINEMENT.NAME));
            OPTIONS.NUCLEARSTORAGE.NAME.ReplaceText(UI.StripLinkFormatting(NUCLEARSTORAGE.NAME));
            OPTIONS.ADVANCEDNUCLEARRESEARCH.NAME.ReplaceText(UI.StripLinkFormatting(ADVANCEDNUCLEARRESEARCH.NAME));
            OPTIONS.RESEARCH_KLEI.TOOLTIP.ReplaceText(OPTIONS.RESEARCH_MOD.TOOLTIP);
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
