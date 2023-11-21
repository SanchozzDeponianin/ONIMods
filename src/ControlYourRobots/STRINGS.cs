namespace ControlYourRobots
{
    public class STRINGS
    {
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
            public class LOW_POWER_MODE_ENABLE
            {
                public static LocString NAME = "Enable energy saving mode when idling";
                public static LocString TOOLTIP = "When the Robot is idle and just stands still and does nothing, it will consume less battery energy.";
            }
            public class LOW_POWER_MODE_VALUE
            {
                public static LocString NAME = "Battery consumption rate in energy saving mode, % of nominal value";
            }
        }
    }
}
