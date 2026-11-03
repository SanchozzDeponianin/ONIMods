using System;
using System.Linq;
using UnityEngine;
using PeterHan.PLib.Detours;

namespace SanchozzONIMods.Shared
{
    [SkipSaveFileSerialization]
    public class MultiRoomTracker : KMonoBehaviour
    {
        [SerializeField]
        public bool allowAnyRoomType = false;

        [SerializeField]
        public string[] possibleRoomTypes;

#pragma warning disable CS0649
        [MyCmpGet]
        RoomTracker roomTracker;
#pragma warning restore CS0649

        private static readonly DetouredMethod<Action<RoomTracker, object>> UPDATEROOM =
            typeof(RoomTracker).DetourLazy<Action<RoomTracker, object>>("OnUpdateRoom");

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            hOnUpdateRoom = Subscribe((int)GameHashes.UpdateRoom, OnUpdateRoomDelegate, this);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe(ref hOnUpdateRoom);
            base.OnCleanUp();
        }

        private int hOnUpdateRoom;
        private static readonly Action<object, object> OnUpdateRoomDelegate = (cmp, data) => ((MultiRoomTracker)cmp).OnUpdateRoom(data);
        private void OnUpdateRoom(object data)
        {
            var room = data as Room;
            if (room != null && roomTracker != null && room.roomType.Id != roomTracker.requiredRoomType
                && (allowAnyRoomType || (possibleRoomTypes != null && possibleRoomTypes.Contains(room.roomType.Id))))
            {
                roomTracker.requiredRoomType = room.roomType.Id;
                UPDATEROOM.Invoke(roomTracker, room);
            }
        }
    }
}
