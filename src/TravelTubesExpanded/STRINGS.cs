using STRINGS;
using SanchozzONIMods.Lib;

namespace TravelTubesExpanded
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        public class BUILDINGS
        {
            public class PREFABS
            {
                public class TRAVELTUBEBUNKERWALLBRIDGE
                {
                    public static LocString NAME = UI.FormatAsLink("Transit Tube Bunker Crossing", "TRAVELTUBEBUNKERWALLBRIDGE");
                    public static LocString DESC = "Tube crossings can run transit tubes through walls without leaking gas or liquid.\nCan withstand extreme pressures and impacts.";
                    public static LocString EFFECT = $"Allows {UI.FormatAsLink("Transit Tubes", "TRAVELTUBE")} to be run through wall and floor tile.\n\nFunctions as {UI.FormatAsLink("Bunker Tile", "BUNKERTILE")}.";
                }
                public class TRAVELTUBEINSULATEDWALLBRIDGE
                {
                    public static LocString NAME = UI.FormatAsLink("Transit Tube Insulated Crossing", "TRAVELTUBEINSULATEDWALLBRIDGE");
                    public static LocString DESC = "Tube crossings can run transit tubes through walls without leaking gas or liquid.\nThe low thermal conductivity of insulated crossing slows any heat passing through them.";
                    public static LocString EFFECT = $"Allows {UI.FormatAsLink("Transit Tubes", "TRAVELTUBE")} to be run through wall and floor tile.\n\nFunctions as {UI.FormatAsLink("Insulated Tile", "INSULATIONTILE")}.";
                }
                public class TRAVELTUBELADDERBRIDGE
                {
                    public static LocString NAME = UI.FormatAsLink("Transit Tube Ladder Crossing", "TRAVELTUBELADDERBRIDGE");
                    public static LocString DESC = "Tube crossings can run transit tubes through ladders and fire poles.";
                    public static LocString EFFECT = $"Allows {UI.FormatAsLink("Transit Tubes", "TRAVELTUBE")} to be run through ladders and fire poles.\n\nFunctions as {UI.FormatAsLink("Ladder", "LADDER")}.";
                }
                public class TRAVELTUBEFIREPOLEBRIDGE
                {
                    public static LocString NAME = UI.FormatAsLink("Transit Tube Fire Pole Crossing", "TRAVELTUBEFIREPOLEBRIDGE");
                    public static LocString DESC = "Tube crossings can run transit tubes through ladders and fire poles.";
                    public static LocString EFFECT = $"Allows {UI.FormatAsLink("Transit Tubes", "TRAVELTUBE")} to be run through ladders and fire poles.\n\nFunctions as {UI.FormatAsLink("Fire Pole", "FIREPOLE")}.";
                }
                public class TRAVELTUBECROSSBRIDGE
                {
                    public static LocString NAME = UI.FormatAsLink("Transit Tube Bridge", "TRAVELTUBECROSSBRIDGE");
                    public static LocString DESC = "Duplicants can pass over it horizontally or vertically, but not diagonally.";
                    public static LocString EFFECT = $"Allows one {UI.FormatAsLink("Transit Tubes", "TRAVELTUBE")} section to be run through another without joining them.\n\nFunctions as regular tile.";
                }
                public class TRAVELTUBEDOOR
                {
                    public static LocString NAME = UI.FormatAsLink("Transit Tube Door", "TRAVELTUBEDOOR");
                    public static LocString DESC = "";
                    public static LocString EFFECT = $"Allows use Door controls inside {UI.FormatAsLink("Transit Tubes", "TRAVELTUBE")}.\n\nFunctions as {UI.FormatAsLink("Door", "DOOR")} and {UI.FormatAsLink("Transit Tube Crossing", "TRAVELTUBEWALLBRIDGE")}.";
                }
            }
        }
        public class OPTIONS
        {
            public class KJOULES_PER_LAUNCH
            {
                public static LocString NAME = $"Transit Tube Access {UI.FormatAsKeyWord("Power")} consumption per launch, kJ";
            }
        }

        internal static void DoReplacement()
        {
            BUILDINGS.PREFABS.TRAVELTUBEDOOR.DESC.ReplaceText(global::STRINGS.BUILDINGS.PREFABS.DOOR.DESC.text);
            LocString.CreateLocStringKeys(typeof(BUILDINGS));
        }
    }
}
