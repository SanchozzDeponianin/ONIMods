using UnityEngine;

namespace AnyIceMachine
{
    internal static class IceMachineExtension
    {
        public const float TARGET_ICE_TEMP = 253.15f; // из IceMachineConfig
        public const float TARGET_TEMP_DELTA = -10f;

        public static void OnCopySettings(this IceMachine machine, object data)
        {
            if (data is GameObject go && go != null && go.TryGetComponent(out IceMachine other)
                && machine.targetProductionElement != other.targetProductionElement)
            {
                machine.SetChosenIce(other.targetProductionElement.CreateTag());
            }
        }

        public static void SetChosenIce(this IceMachine machine, Tag new_ice)
        {
            var ingredient = Patches.ELEMENT_OPTIONS[new_ice];
            if (machine.TryGetComponent(out ManualDeliveryKG mdk))
                mdk.RequestedItemTag = ingredient;
            machine.targetTemperature = Mathf.Min(TARGET_ICE_TEMP, ElementLoader.GetElement(ingredient).lowTemp + TARGET_TEMP_DELTA);
            for (int i = machine.waterStorage.Count; i > 0; i--)
            {
                var item = machine.waterStorage.items[i - 1];
                if (item != null && item.HasTag(GameTags.Liquid) && item.PrefabID() != ingredient)
                    machine.waterStorage.Drop(item);
            }
        }
    }
}
