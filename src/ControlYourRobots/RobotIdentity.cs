using System.Collections.Generic;
using KSerialization;

namespace ControlYourRobots
{
    // Minion/RobotAssignablesProxy не может быть затаргечена само на себя, иначе уйдет в рекурсию
    // поэтому вот такая минимально необходимая заглушка. по сути нам нужен только GetProperName

    [SerializationConfig(MemberSerialization.OptIn)]
    public class RobotIdentity : KMonoBehaviour, ISaveLoadable, IAssignableIdentity
    {
        [Serialize]
        private Tag prefabID;

        public Tag PrefabID { get => prefabID; internal set => prefabID = value; }

        public string GetProperName() => prefabID.ProperName();
        public List<Ownables> GetOwners() => null;
        public Ownables GetSoleOwner() => null;
        public bool IsNull() => this == null;
        public bool HasOwner(Assignables owner) => false;
        public int NumOwners() => 0;
    }
}
