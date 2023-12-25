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

        public static int GetRobotProxyID(Tag prefabID)
        {
            foreach (var proxi in Cmps.Items)
            {
                if (!proxi.IsNullOrDestroyed() && proxi.PrefabID == prefabID)
                    return proxi.TargetInstanceID;
            }
            var go = GameUtil.KInstantiate(Assets.GetPrefab(RobotAssignablesProxyConfig.ID), Grid.SceneLayer.NoLayer);
            var identity = go.GetComponent<RobotIdentity>();
            identity.PrefabID = prefabID;
            var proxy = go.GetComponent<RobotAssignablesProxy>();
            proxy.SetTarget(identity, go);
            go.SetActive(true);
            return proxy.TargetInstanceID;
        }

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
            Cmps.Add(this);
        }

        protected override void OnCleanUp() { }

        private void OnQueueDestroyObject(object data)
        {
            Cmps.Remove(this);
        }
    }
}
