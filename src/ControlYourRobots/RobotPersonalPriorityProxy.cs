using Klei.AI;

namespace ControlYourRobots
{
    [SkipSaveFileSerialization]
    public class RobotPersonalPriorityProxy : KMonoBehaviour
    {
        public static Components.Cmps<RobotPersonalPriorityProxy> Cmps = new Components.Cmps<RobotPersonalPriorityProxy>();

        [MyCmpReq]
        public ChoreConsumer consumer;

        [MyCmpReq]
        public Traits traits;

        public Tag PrefabID { get; private set; }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            PrefabID = this.PrefabID();
            SetDefaultPriority();
            Cmps.Add(this);
        }

        private void SetDefaultPriority()
        {
            // подгрузка приоритетов из первого гобота, чтобы у всех было одинаково
            RobotPersonalPriorityProxy first = null;
            foreach (var rppp in Cmps.Items)
            {
                if (rppp != null && rppp.PrefabID == PrefabID)
                {
                    first = rppp;
                    break;
                }
            }
            if (first != null)
            {
                foreach (var group in Db.Get().ChoreGroups.resources)
                {
                    if (!consumer.IsChoreGroupDisabled(group))
                    {
                        consumer.SetPersonalPriority(group, first.consumer.GetPersonalPriority(group));
                    }
                }
            }
        }

        protected override void OnCleanUp()
        {
            Cmps.Remove(this);
            base.OnCleanUp();
        }
    }
}
