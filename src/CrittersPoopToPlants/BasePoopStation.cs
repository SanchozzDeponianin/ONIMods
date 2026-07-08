using STRINGS;
using UnityEngine;

namespace CrittersPoopToPlants
{
    public abstract class BasePoopStation : KMonoBehaviour, IPoopStation
    {
        private GameObject poopUser;

        [SerializeField]
        public float capacity;

        [SerializeField]
        public Tag[] allowedUsersIds;

        public virtual Storage Storage => null;

        public override void OnSpawn()
        {
            RegisterPoopStation();
            base.OnSpawn();
        }

        public override void OnCleanUp()
        {
            UnregisterPoopStation();
            base.OnCleanUp();
        }

        public bool IsUserCompatibleWithPoopStation(KPrefabID userPrefabID) => userPrefabID.HasAnyTags(allowedUsersIds);

        public GameObject GetPoopStationObject() => gameObject;

        public GameObject GetCurrentPoopStationUser() => poopUser;

        public virtual bool IsPoopStationOperational() => false;

        public string[] GetPoopingAnimNames() => null;

        public void RegisterPoopStation() => Components.PoopStations.Add(gameObject.GetMyWorldId(), this);

        public void UnregisterPoopStation() => Components.PoopStations.Remove(gameObject.GetMyWorldId(), this);

        public PoopData GetPoopData()
        {
            return new PoopData(false, Storage, CREATURES.POOP.PLANT_POOP_STATION_WILD, Def.GetUISprite(gameObject, "ui", false).first);
        }

        public float GetPoopCapacity() => capacity;

        public float GetAvailablePoopCapacityPercentage() => GetAvailablePoopCapacity() / GetPoopCapacity();

        public virtual float GetAvailablePoopCapacity() => 0f;

        protected bool CanAcceptMorePoop() => GetAvailablePoopCapacity() > 0f;

        public void PlayPoopStationAnim(string animName, KAnim.PlayMode playMode) { }

        public void ClearPoopStationUser(GameObject userRequestingClearing)
        {
            if (poopUser == userRequestingClearing)
            {
                poopUser = null;
                Trigger(-984476291, null);
            }
        }

        public bool AttemptToReservePoopStation(GameObject userRequestingReserve)
        {
            if (poopUser != null && poopUser != userRequestingReserve)
                return false;
            poopUser = userRequestingReserve;
            return true;
        }
    }
}
