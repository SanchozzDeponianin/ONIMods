using Klei.AI;

namespace MechanicsStation
{
    public class TinkerableWorkable : KMonoBehaviour
    {
        private AttributeInstance craftingSpeedAttribute;
        private AttributeInstance machinerySpeedAttribute;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Attributes attributes = gameObject.GetAttributes();
            craftingSpeedAttribute = attributes.Add(MechanicsStationPatches.craftingSpeedAttribute);
            machinerySpeedAttribute = attributes.Add(Db.Get().Attributes.MachinerySpeed);
        }

        public float GetCraftingSpeedMultiplier()
        {
            return craftingSpeedAttribute.GetTotalValue();
        }

        public float GetMachinerySpeedMultiplier()
        {
            return machinerySpeedAttribute.GetTotalValue();
        }
    }
}
