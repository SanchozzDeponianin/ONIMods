using STRINGS;

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
            }
        }
        public class OPTIONS
        {
            public class KJOULES_PER_LAUNCH
            {
                public static LocString NAME = $"Transit Tube Access {UI.FormatAsKeyWord("Power")} consumption per launch, kJ";
            }
        }
    }
}
