using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace AnyIceMachine
{
    using static Patches;
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
            var ingredient = ELEMENT_OPTIONS[new_ice];
            float capacity = 0f;
            if (machine.TryGetComponent(out ManualDeliveryKG mdk))
            {
                mdk.RequestedItemTag = ingredient;
                capacity = mdk.capacity;
            }
            machine.targetTemperature = Mathf.Min(TARGET_ICE_TEMP, ElementLoader.GetElement(ingredient).lowTemp + TARGET_TEMP_DELTA);
            machine.SetPipedEverythingConsumer(ingredient, capacity);
            for (int i = machine.waterStorage.Count; i > 0; i--)
            {
                var item = machine.waterStorage.items[i - 1];
                if (item != null && item.HasTag(GameTags.Liquid) && item.PrefabID() != ingredient)
                    machine.waterStorage.Drop(item);
            }
        }

        public static void SetPipedEverythingConsumer(this IceMachine machine, Tag input_liquid, float capacity)
        {
            if (PipedEverythingConsumer != null)
            {
                try
                {
                    foreach (var consumer in machine.GetComponents(PipedEverythingConsumer))
                    {
                        var traverse = Traverse.Create(consumer);
                        if (traverse.Property<Storage>("Storage").Value == machine.waterStorage)
                        {
                            traverse.Field<SimHashes[]>("elementFilter").Value = new[] { ElementLoader.GetElement(input_liquid).id };
                            traverse.Field<bool>("keepZeroMassObject").Value = false;
                            if (capacity > 0f)
                            {
                                traverse.Field<float>("capacityKG").Value = capacity;
                                machine.waterStorage.capacityKg = capacity;
                            }
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Utils.LogExcWarn(e);
                }
            }
        }

        private static SimHashes[] outputLiquids;
        private static Tag[] outputSolids;

        public static void SetPipedEverythingDispenser(this IceMachine machine)
        {
            if (outputSolids == null)
            {
                outputSolids = ELEMENT_OPTIONS.Keys.Where(tag => ElementLoader.GetElement(tag).IsSolid).ToArray();
                outputLiquids = ELEMENT_OPTIONS.Keys.Where(tag => ElementLoader.GetElement(tag).IsLiquid)
                    .Select(tag => ElementLoader.GetElement(tag).id).ToArray();
            }
            if (PipedEverythingDispenser != null)
            {
                try
                {
                    foreach (var dispenser in machine.GetComponents(PipedEverythingDispenser))
                    {
                        var traverse = Traverse.Create(dispenser);
                        if (traverse.Property<Storage>("Storage").Value == machine.iceStorage)
                        {
                            traverse.Field<SimHashes[]>("elementFilter").Value = outputLiquids;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Utils.LogExcWarn(e);
                }
            }
            if (PipedEverythingDispenserS != null)
            {
                try
                {
                    foreach (var dispenser in machine.GetComponents(PipedEverythingDispenserS))
                    {
                        var traverse = Traverse.Create(dispenser);
                        if (traverse.Property<Storage>("Storage").Value == machine.iceStorage)
                        {
                            traverse.Field<Tag[]>("tagFilter").Value = outputSolids;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Utils.LogExcWarn(e);
                }
            }
        }
    }
}
