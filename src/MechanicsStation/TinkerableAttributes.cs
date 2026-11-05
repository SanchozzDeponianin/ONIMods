using Klei.AI;
using UnityEngine;

namespace MechanicsStation
{
    [SkipSaveFileSerialization]
    public class TinkerableAttributes : KMonoBehaviour
    {
        [SerializeField]
        public bool disabled = false;

        private AttributeInstance craftingSpeed;
        private AttributeInstance machinerySpeed;

        public float CraftingSpeedMultiplier => craftingSpeed?.GetTotalValue() ?? 1f;
        public float MachinerySpeedMultiplier => machinerySpeed?.GetTotalValue() ?? 1f;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            var attributes = gameObject.GetAttributes();
            craftingSpeed = attributes.Get(ModAssets.CraftingSpeed);
            machinerySpeed = attributes.Get(Db.Get().Attributes.MachinerySpeed);
        }
    }
}
