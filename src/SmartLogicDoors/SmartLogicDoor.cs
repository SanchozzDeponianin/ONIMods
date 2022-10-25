using System;
using KSerialization;
using static STRINGS.BUILDINGS.PREFABS.DOOR;
using static STRINGS.UI;
using PeterHan.PLib.Core;

namespace SmartLogicDoors
{
    public class SmartLogicDoor : KMonoBehaviour
    {
        private static string RedAutoDescription;
        private static string GreenAutoDescription;

        [Serialize]
        public Door.ControlState RedState = Door.ControlState.Locked;

        [Serialize]
        public Door.ControlState GreenState = Door.ControlState.Opened;

#pragma warning disable CS0649
        [MyCmpReq]
        private LogicPorts ports;
#pragma warning restore CS0649

        public bool IsLogicPortConnected => ports.IsPortConnected(Door.OPEN_CLOSE_PORT_ID);

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            if (string.IsNullOrEmpty(RedAutoDescription))
            {
                RedAutoDescription = $"{FormatAsAutomationState(LOGIC_PORTS.GATE_SINGLE_INPUT_ONE_INACTIVE, AutomationState.Standby)}: {CONTROL_STATE.AUTO.NAME}";
                GreenAutoDescription = $"{FormatAsAutomationState(LOGIC_PORTS.GATE_SINGLE_INPUT_ONE_ACTIVE, AutomationState.Active)}: {CONTROL_STATE.AUTO.NAME}";
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            UpdateLogicPortDescription();
        }

        // обновляем описание логик. порта, которое отображается в оверлее и некоторых других местах
        private void UpdateLogicPortDescription()
        {
            if (ports.inputPortInfo != null && ports.inputPortInfo.Length > 0)
            {
                int idx = Array.FindIndex(ports.inputPortInfo, port => port.id == Door.OPEN_CLOSE_PORT_ID);
                if (idx != -1)
                {
                    switch (RedState)
                    {
                        case Door.ControlState.Opened:
                            PUtil.LogWarning("Not Implemented");
                            break;
                        case Door.ControlState.Auto:
                            ports.inputPortInfo[idx].inactiveDescription = RedAutoDescription;
                            break;
                        case Door.ControlState.Locked:
                            ports.inputPortInfo[idx].inactiveDescription = LOGIC_OPEN_INACTIVE;
                            break;
                    }
                    switch (GreenState)
                    {
                        case Door.ControlState.Opened:
                            ports.inputPortInfo[idx].activeDescription = LOGIC_OPEN_ACTIVE;
                            break;
                        case Door.ControlState.Auto:
                            ports.inputPortInfo[idx].activeDescription = GreenAutoDescription;
                            break;
                        case Door.ControlState.Locked:
                            PUtil.LogWarning("Not Implemented");
                            break;
                    }
                }
            }
        }

        public bool ApplyControlState()
        {
            UpdateLogicPortDescription();
            if (IsLogicPortConnected)
            {
                Trigger((int)GameHashes.LogicEvent, new LogicValueChanged()
                {
                    portID = Door.OPEN_CLOSE_PORT_ID,
                    newValue = ports.GetInputValue(Door.OPEN_CLOSE_PORT_ID)
                });
                return true;
            }
            return false;
        }
    }
}
