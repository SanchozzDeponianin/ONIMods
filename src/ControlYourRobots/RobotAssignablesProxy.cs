namespace ControlYourRobots
{
    // мимикриацция под MinionAssignablesProxy для внедрения в контроль дверей
    // один прокси-объект для всех роботов одного типа
    // методы OnЧетотам переопределены для избежания создания и верчения ненужными нам сущностями

    using handler = EventSystem.IntraObjectHandler<RobotAssignablesProxy>;
    public class RobotAssignablesProxy : MinionAssignablesProxy
    {
        private static readonly handler OnQueueDestroyObjectDelegate = new handler((cmp, data) => cmp.OnQueueDestroyObject(data));
        public static Components.Cmps<RobotAssignablesProxy> Cmps = new Components.Cmps<RobotAssignablesProxy>();
        private static object @lock = new object();
        SchedulerHandle handle;

        public Tag PrefabID
        {
            get
            {
                RestoreTargetFromInstanceID();
                if (!target.IsNullOrDestroyed() && target is RobotIdentity identity)
                    return identity.PrefabID;
                else return Tag.Invalid;
            }
        }

        protected override void OnPrefabInit()
        {
            Subscribe((int)GameHashes.QueueDestroyObject, OnQueueDestroyObjectDelegate);
        }

        protected override void OnSpawn()
        {
            RestoreTargetFromInstanceID();
            TryAdd(this);
            handle = GameScheduler.Instance.Schedule("cleanup", 2 * UpdateManager.SecondsPerSimTick, CleanupLimbo);
        }

        private void CleanupLimbo(object _)
        {
            // остаться должен только один (с)
            foreach (var item in Cmps.Items)
                if (item == this)
                    return;
            Util.KDestroyGameObject(gameObject);
        }

        protected override void OnCleanUp()
        {
            handle.ClearScheduler();
        }

        private void OnQueueDestroyObject(object data)
        {
            Cmps.Remove(this);
        }

        private static bool TryGet(Tag prefabID, out RobotAssignablesProxy proxy)
        {
            lock (@lock)
            {
                foreach (var item in Cmps.Items)
                {
                    if (!item.IsNullOrDestroyed() && item.PrefabID == prefabID)
                    {
                        proxy = item;
                        return true;
                    }
                }
                proxy = null;
                return false;
            }
        }

        private static RobotAssignablesProxy TryAdd(RobotAssignablesProxy proxy)
        {
            var tag = proxy.PrefabID;
            lock (@lock)
            {
                foreach (var item in Cmps.Items)
                {
                    if (!item.IsNullOrDestroyed() && item.PrefabID == tag)
                    {
                        return item;
                    }
                }
                Cmps.Add(proxy);
                return proxy;
            }
        }

        private static RobotAssignablesProxy Instantiate(Tag prefabID)
        {
            var go = GameUtil.KInstantiate(Assets.GetPrefab(RobotAssignablesProxyConfig.ID), Grid.SceneLayer.NoLayer);
            var identity = go.GetComponent<RobotIdentity>();
            identity.PrefabID = prefabID;
            var proxy = go.GetComponent<RobotAssignablesProxy>();
            proxy.SetTarget(identity, go);
            go.SetActive(true);
            return proxy;
        }

        public static int GetRobotProxyID(Tag prefabID)
        {
            if (TryGet(prefabID, out var proxy))
                return proxy.TargetInstanceID;
            else
                return TryAdd(Instantiate(prefabID)).TargetInstanceID;
        }
    }
}
