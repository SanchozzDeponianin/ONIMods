using Klei.AI;

namespace MechanicsStation
{
    public class TinkerableWorkable : KMonoBehaviour
    {
        private AttributeInstance craftingSpeed;
        private AttributeInstance machinerySpeed;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            var attributes = gameObject.GetAttributes();
            craftingSpeed = attributes.Add(MechanicsStationAssets.CraftingSpeed);
            machinerySpeed = attributes.Add(Db.Get().Attributes.MachinerySpeed);
        }

        public float GetCraftingSpeedMultiplier()
        {
            return craftingSpeed.GetTotalValue();
        }

        public float GetMachinerySpeedMultiplier()
        {
            return machinerySpeed.GetTotalValue();
        }
    }
}
