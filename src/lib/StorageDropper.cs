using System;
using UnityEngine;
using HarmonyLib;

namespace SanchozzONIMods.Shared
{
    // компонент позволяющий гибко настроить:
    // при уничтожении объекта содержимое хранилища - газ и жидкость
    // оставить в виде бутылок или высвободить в мир
    // принцип работы: отписываем Storage от события OnQueueDestroyObject
    // и сами дергаем его обработчик когда придет время
    [SkipSaveFileSerialization]
    public class StorageDropper : KMonoBehaviour
    {
        private static readonly EventSystem.IntraObjectHandler<StorageDropper> OnQueueDestroyObjectDelegate =
            new((component, data) => component.OnQueueDestroyObject(data));

        private static EventSystem.IntraObjectHandler<Storage> Storage_OnQueueDestroyObjectDelegate;
        private static Action<Storage, object> Storage_OnQueueDestroyObjectHandler;

        private Storage[] storages;

        [SerializeField]
        public bool vent_gas = false;

        [SerializeField]
        public bool dump_liquid = false;

        [SerializeField]
        public Vector3 offset = default;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            if (Storage_OnQueueDestroyObjectDelegate == null)
            {
                Storage_OnQueueDestroyObjectDelegate = Traverse.Create<Storage>()
                    .Field<EventSystem.IntraObjectHandler<Storage>>("OnQueueDestroyObjectDelegate").Value;
                Storage_OnQueueDestroyObjectHandler = Traverse.Create(Storage_OnQueueDestroyObjectDelegate)
                    .Field<Action<Storage, object>>("handler").Value;
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.QueueDestroyObject, OnQueueDestroyObjectDelegate);
            storages = GetComponents<Storage>();
            foreach (var storage in storages)
            {
                storage.Unsubscribe((int)GameHashes.QueueDestroyObject, Storage_OnQueueDestroyObjectDelegate);
            }
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.QueueDestroyObject, OnQueueDestroyObjectDelegate);
            base.OnCleanUp();
        }

        private void OnQueueDestroyObject(object data)
        {
            foreach (var storage in storages)
            {
                storage.DropAll(vent_gas, dump_liquid, offset);
                Storage_OnQueueDestroyObjectHandler(storage, data);
            }
        }
    }
}