using System.Collections.Generic;
using KSerialization;

namespace ControlYourRobots
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class DefaultFilterableManager : KMonoBehaviour
    {
        [Serialize]
        private readonly Dictionary<Tag, HashSet<Tag>> acceptedTags = new();

        public static DefaultFilterableManager Instance { get; private set; }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Instance = this;
        }

        protected override void OnCleanUp()
        {
            Instance = null;
            base.OnCleanUp();
        }

        public void Set(Tag prefab_id, HashSet<Tag> accepted_tags)
        {
            if (acceptedTags.TryGetValue(prefab_id, out var tags))
            {
                tags.Clear();
                tags.UnionWith(accepted_tags);
            }
            else
                acceptedTags.Add(prefab_id, new HashSet<Tag>(accepted_tags));
        }

        public bool TryGet(Tag prefab_id, out HashSet<Tag> accepted_tags)
        {
            return acceptedTags.TryGetValue(prefab_id, out accepted_tags);
        }
    }
}
