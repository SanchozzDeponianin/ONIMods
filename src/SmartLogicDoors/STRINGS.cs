using System.Collections.Generic;
using SanchozzONIMods.Lib;
using static STRINGS.BUILDING.STATUSITEMS;
using static STRINGS.UI;

namespace SmartLogicDoors
{
    public class STRINGS
    {
        private const string OPENED = "{OPENED}";
        private const string LOCKED = "{LOCKED}";
        private const string AUTO = "{AUTO}";
        private const string GREEN = "{GREEN}";
        private const string RED = "{RED}";

        public class UI
        {
            public class UISIDESCREENS
            {
                public class SMARTLOGICDOOR_SIDESCREEN
                {
                    public static LocString TITLE = $"Door State at the {FormatAsAutomationState("Green", AutomationState.Active)} / {FormatAsAutomationState("Red", AutomationState.Standby)} signal:";
                    public class OPENED_LOCKED
                    {
                        public static LocString NAME = $"{OPENED} / {LOCKED}";
                        public static LocString TOOLTIP = $"{GREEN}: {OPENED}\n{RED}: {LOCKED}";
                    }
                    public class OPENED_AUTO
                    {
                        public static LocString NAME = $"{OPENED} / {AUTO}";
                        public static LocString TOOLTIP = $"{GREEN}: {OPENED}\n{RED}: {AUTO}";
                    }
                    public class AUTO_LOCKED
                    {
                        public static LocString NAME = $"{AUTO} / {LOCKED}";
                        public static LocString TOOLTIP = $"{GREEN}: {AUTO}\n{RED}: {LOCKED}";
                    }
                }
            }
        }

        internal static void DoReplacement()
        {
            var dictionary = new Dictionary<string, string>()
            {
                {OPENED, CURRENTDOORCONTROLSTATE.OPENED },
                {LOCKED, CURRENTDOORCONTROLSTATE.LOCKED },
                {AUTO, CURRENTDOORCONTROLSTATE.AUTO},
                {GREEN, FormatAsAutomationState(LOGIC_PORTS.GATE_SINGLE_INPUT_ONE_ACTIVE, AutomationState.Active) },
                {RED, FormatAsAutomationState(LOGIC_PORTS.GATE_SINGLE_INPUT_ONE_INACTIVE, AutomationState.Standby) },
            };
            Utils.ReplaceAllLocStringTextByDictionary(typeof(STRINGS), dictionary);
            LocString.CreateLocStringKeys(typeof(UI));
        }
    }
}
