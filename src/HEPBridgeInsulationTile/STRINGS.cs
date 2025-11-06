using System.Collections.Generic;
using STRINGS;
using static STRINGS.BUILDINGS.PREFABS;
using static STRINGS.RESEARCH.TECHS;
using SanchozzONIMods.Lib;

namespace HEPBridgeInsulationTile
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";
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
        public class MISC
        {
            public class TAGS
            {
                public static LocString EXTRUDABLE_DESC = "";
            }
        }
        public class OPTIONS
        {
            public class RESEARCH_MOD
            {
                public static LocString NAME = "Required Research";
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
            public class USE_OLD_ANIM
            {
                public static LocString NAME = "Use animation from early version of the mod";
            }
        }

        internal static void DoReplacement()
        {
            string name = UI.StripLinkFormatting(HEPBRIDGETILE.NAME);
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), new Dictionary<string, string> { { HEPBRIDGETILE_NAME, name } });
            BUILDINGS.PREFABS.HIGHENERGYPARTICLEWALLBRIDGEREDIRECTOR.DESC.ReplaceText($"{HEPBRIDGETILE.DESC.text}\n{INSULATEDWIRE.DESC.text}");
            string effect = HEPBRIDGETILE.EFFECT.text;
            int i = INSULATIONTILE.EFFECT.text.IndexOf("\n\n");
            if (i >= 0)
                effect += INSULATIONTILE.EFFECT.text.Substring(i);
            BUILDINGS.PREFABS.HIGHENERGYPARTICLEWALLBRIDGEREDIRECTOR.EFFECT.ReplaceText(effect);
            OPTIONS.NUCLEARREFINEMENT.NAME.ReplaceText(UI.StripLinkFormatting(NUCLEARREFINEMENT.NAME));
            OPTIONS.NUCLEARSTORAGE.NAME.ReplaceText(UI.StripLinkFormatting(NUCLEARSTORAGE.NAME));
            OPTIONS.ADVANCEDNUCLEARRESEARCH.NAME.ReplaceText(UI.StripLinkFormatting(ADVANCEDNUCLEARRESEARCH.NAME));
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
            LocString.CreateLocStringKeys(typeof(MISC));
        }
    }
}
