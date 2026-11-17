using System.Runtime.Serialization;
using Klei.AI;
using KSerialization;
using UnityEngine;

namespace AttributeRestrictions
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class AttributeRestriction : KMonoBehaviour
    {
        [SerializeField]
        public bool showInUI = true;

        [Serialize]
        public bool isEnabled = false;

        [Serialize]
        [SerializeField]
        public bool isBelow = false;

        [Serialize]
        public int requiredAttributeLevel = 5;

        [SerializeField]
        public Workable workable;

        [SerializeField]
        public HashedString overrideAttribute;

        public Attribute requiredAttribute => (overrideAttribute.IsValid ? Db.Get().Attributes.TryGet(overrideAttribute) : null) ?? workable?.GetWorkAttribute();

        public static Chore.Precondition IsSufficientAttributeLevel = new()
        {
            id = nameof(IsSufficientAttributeLevel),
            description = STRINGS.DUPLICANTS.CHORES.PRECONDITIONS.IS_SUFFICIENT_ATTRIBUTE_LEVEL,
            sortOrder = 20,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                var restriction = data as AttributeRestriction;
                Attribute attribute;
                if (restriction != null && restriction.isEnabled && (attribute = restriction.requiredAttribute) != null)
                {
                    var value = context.consumerState.resume?.GetAttributes()?.Get(attribute)?.GetTotalValue() ?? 0f;
                    return restriction.isBelow ? value <= restriction.requiredAttributeLevel : value >= restriction.requiredAttributeLevel;
                }
                return true;
            }
        };

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            hOnCopySettings = Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate, this);
        }

        [OnDeserialized]
        public void OnDeserialized()
        {
            requiredAttributeLevel = Mathf.Clamp(requiredAttributeLevel, 0, ModOptions.Instance.max_level);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe(ref hOnCopySettings);
            base.OnCleanUp();
        }

        private int hOnCopySettings;
        private static readonly System.Action<object, object> OnCopySettingsDelegate = (cmp, data) => ((AttributeRestriction)cmp).OnCopySettings(data);
        private void OnCopySettings(object data)
        {
            var go = data as GameObject;
            if (go != null && go.TryGetComponent(out AttributeRestriction restriction))
            {
                isEnabled = restriction.isEnabled;
                isBelow = restriction.isBelow;
                requiredAttributeLevel = restriction.requiredAttributeLevel;
            }
        }
    }
}
