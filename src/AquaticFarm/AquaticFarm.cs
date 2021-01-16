using System;
using Harmony;
using UnityEngine;

namespace AquaticFarm
{
    public class AquaticFarm : KMonoBehaviour
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private PlantablePlot plantablePlot;
#pragma warning restore CS0649

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.OccupantChanged, new Action<object>(OnOccupantChanged));
            OnOccupantChanged(plantablePlot.Occupant);
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            Unsubscribe((int)GameHashes.OccupantChanged, new Action<object>(OnOccupantChanged));
        }

        private void OnOccupantChanged(object data)
        {
            var elementConsumers = GetComponents<PassiveElementConsumer>();
            foreach (PassiveElementConsumer elementConsumer in elementConsumers)
            {
                elementConsumer.EnableConsumption(false);
            }

            if (data != null)
            {
                var consumed_infos = ((GameObject)data)?.GetSMI<IrrigationMonitor.Instance>()?.def.consumedElements;
                if (consumed_infos != null)
                {
                    foreach (var consumeInfo in consumed_infos)
                    {
                        foreach (var elementConsumer in elementConsumers)
                        {
                            var element = ElementLoader.FindElementByHash(elementConsumer.elementToConsume);
                            if (element != null)
                            {
                                if (element.tag != consumeInfo.tag)
                                {
                                    var traverse = Traverse.Create(elementConsumer);
                                    traverse.Method("SimUnregister").GetValue();
                                    elementConsumer.elementToConsume = ElementLoader.GetElementID(consumeInfo.tag);
                                    traverse.Method("SimRegister").GetValue();
                                }
                                elementConsumer.consumptionRate = consumeInfo.massConsumptionRate * 1.5f;
                                elementConsumer.EnableConsumption(true);
                            }
                        }
                    }
                }
            }
        }
    }
}
