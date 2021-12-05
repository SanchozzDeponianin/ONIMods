using STRINGS;

namespace HEPBridgeInsulationTile
{
    public class STRINGS
    {
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class HIGHENERGYPARTICLEWALLBRIDGEREDIRECTOR
                {
                    public static LocString NAME = UI.FormatAsLink("Through-Wall Radbolt Reflector", HEPBridgeInsulationTileConfig.ID);
                    public static LocString DESC = "";
                    public static LocString EFFECT = "Receives and redirects Radbolts from " + UI.FormatAsLink("Radbolt Generators", "HIGHENERGYPARTICLESPAWNER") + " through wall and floor tiles without leaking gas or liquid.\n\nFunctions as regular tile.";
                }
            }
        }

        internal static void DoReplacement()
        {
            BUILDINGS.PREFABS.HIGHENERGYPARTICLEWALLBRIDGEREDIRECTOR.DESC = global::STRINGS.BUILDINGS.PREFABS.HIGHENERGYPARTICLEREDIRECTOR.DESC;
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
