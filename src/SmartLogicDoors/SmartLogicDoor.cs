using KSerialization;

namespace SmartLogicDoors
{
    public class SmartLogicDoor : KMonoBehaviour
    {
        [Serialize]
        public Door.ControlState RedState = Door.ControlState.Locked;

        [Serialize]
        public Door.ControlState GreenState = Door.ControlState.Opened;

#pragma warning disable CS0649
        [MyCmpReq]
        private LogicPorts ports;
#pragma warning restore CS0649

        public bool IsLogicPortConnected => ports.IsPortConnected(Door.OPEN_CLOSE_PORT_ID);

        public bool ApplyControlState()
        {
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
