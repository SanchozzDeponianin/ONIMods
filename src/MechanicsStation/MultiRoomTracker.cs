using System;
using System.Linq;
using PeterHan.PLib.Detours;

namespace MechanicsStation
{
    public class MultiRoomTracker : KMonoBehaviour
    {
        public string[] possibleRoomTypes;

#pragma warning disable CS0649
        [MyCmpGet]
        RoomTracker roomTracker;
#pragma warning restore CS0649

        private static readonly EventSystem.IntraObjectHandler<MultiRoomTracker> OnUpdateRoomDelegate =
            new EventSystem.IntraObjectHandler<MultiRoomTracker>(
                (MultiRoomTracker component, object data) => component.OnUpdateRoom(data));

        private static readonly DetouredMethod<Action<RoomTracker, object>> UPDATEROOM = 
            typeof(RoomTracker).DetourLazy<Action<RoomTracker, object>>("OnUpdateRoom");

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe((int)GameHashes.UpdateRoom, OnUpdateRoomDelegate);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.UpdateRoom, OnUpdateRoomDelegate);
            base.OnCleanUp();
        }

        private void OnUpdateRoom(object data)
        {
            var room = (Room)data;
            if (room != null && roomTracker != null && possibleRoomTypes != null && room.roomType.Id != roomTracker.requiredRoomType && possibleRoomTypes.Contains(room.roomType.Id))
            {
                roomTracker.requiredRoomType = room.roomType.Id;
                UPDATEROOM.Invoke(roomTracker, room);
            }
        }
    }
}
