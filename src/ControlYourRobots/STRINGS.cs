namespace ControlYourRobots
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        public class ROBOTS
        {
            public class STATUSITEMS
            {
                public class SLEEP_MODE
                {
                    public static LocString NAME = "Sleep Mode";
                    public static LocString TOOLTIP = "This Robot went into sleep mode to save its battery.\n\nDo robots dream of electric Dreckos?";
                }
            }
        }
        public class OPTIONS
        {
            public class ZZZ_ICON_ENABLE
            {
                public static LocString NAME = "Enable ZZZ status icon when sleeping";
            }
            public class LOW_POWER_MODE_ENABLE
            {
                public static LocString NAME = "Enable energy saving mode when idling";
                public static LocString TOOLTIP = "When the Robot is idle and just stands still and does nothing, it will consume less battery energy.";
            }
            public class LOW_POWER_MODE_FLYDO_LANDED
            {
                public static LocString NAME = "Enable Flydo energy saving mode when idling";
                public static LocString TOOLTIP = "When the Flydo is idle and does nothing, it will landed to floor and consume less battery energy.";
            }
            public class LOW_POWER_MODE_FLYDO_TIMEOUT
            {
                public static LocString NAME = "The timeout when the Flydo will enter energy saving mode";
            }
            public class LOW_POWER_MODE_VALUE
            {
                public static LocString NAME = "Battery consumption rate in energy saving mode, % of nominal value";
            }
            public class FLYDO_CAN_PASS_DOOR
            {
                public static LocString NAME = "Flydo can pass through Doors";
            }
            public class RESTRICT_FLYDO_BY_DEFAULT
            {
                public static LocString NAME = "Prohibit Flydo's access to Doors by default";
                public static LocString TOOLTIP = "If enabled, the mod will automatically deny Flydo access\non all non-configured Doors for performance reasons.";
            }
            public class FLYDO_CAN_FOR_ITSELF
            {
                public static LocString NAME = "Flydo can pick up the Power Bank for itself";
            }
            public class FLYDO_CAN_LIQUID_SOURCE
            {
                public static LocString NAME = "Flydo can use Pitcher Pump, Bottle Fillers, Ice Liquefier ect";
            }
            public class FLYDO_CAN_UNDERWATER
            {
                public static LocString NAME = "Flydo can pick up underwater things";
            }
            public class FLYDO_PREFERS_STRAIGHT
            {
                public static LocString NAME = "Flydo prefers a straight movement instead of a zigzag";
            }
            public class DEAD_FLYDO_RETURNS_MATERIALS
            {
                public static LocString NAME = "Flydo returns the materials from which it was made when destroyed";
            }
            public class DECONSTRUCT_DEAD_BIOBOT
            {
                public static LocString NAME = "Auto-create the errand to deconstruct a dead Biobot";
            }
            public class DECONSTRUCT_DEAD_ROVER
            {
                public static LocString NAME = "Auto-create the errand to deconstruct a dead Rover";
            }
        }
    }
}
