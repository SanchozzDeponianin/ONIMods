namespace AquaticFarm
{
    public class AquaticFarm : KMonoBehaviour
    {
#pragma warning disable CS0649
        [MyCmpReq]
        Building building;

        [MyCmpReq]
        private PlantablePlot plantablePlot;
#pragma warning restore CS0649

        private PassiveElementConsumer[] consumers;
        private HandleVector<int>.Handle partitionerEntry;

        public override void OnSpawn()
        {
            base.OnSpawn();
            consumers = GetComponents<PassiveElementConsumer>();
            Subscribe((int)GameHashes.OccupantChanged, RefreshConsumption);
            partitionerEntry = GameScenePartitioner.Instance.Add("AquaticFarm.OnSpawn", gameObject, building.GetValidPlacementExtents(),
                GameScenePartitioner.Instance.liquidChangedLayer, RefreshConsumption);
            RefreshConsumption(plantablePlot.Occupant);
        }

        public override void OnCleanUp()
        {
            base.OnCleanUp();
            Unsubscribe((int)GameHashes.OccupantChanged, RefreshConsumption);
            GameScenePartitioner.Instance.Free(ref partitionerEntry);
        }

        private void RefreshConsumption(object data)
        {
            if (plantablePlot.Occupant != null
                && plantablePlot.Occupant.GetSMI<IrrigationMonitor.Instance>()?.def.consumedElements is var consumed_infos
                && consumed_infos != null && consumed_infos.Length > 0)
            {
                foreach (var consumer in consumers)
                {
                    int cell = consumer.GetSampleCell();
                    if (Grid.IsValidCell(cell))
                    {
                        bool enable = false;
                        var current = Grid.Element[cell];
                        foreach (var info in consumed_infos)
                        {
                            if (info.tag == current.tag)
                            {
                                if (consumer.elementToConsume != current.id)
                                {
                                    consumer.EnableConsumption(false);
                                    consumer.SimUnregister();
                                    consumer.elementToConsume = current.id;
                                    consumer.SimRegister();
                                }
                                consumer.consumptionRate = info.massConsumptionRate * 1.5f;
                                consumer.EnableConsumption(true);
                                enable = true;
                                break;
                            }
                        }
                        if (!enable)
                            consumer.EnableConsumption(false);
                    }
                }
            }
            else
            {
                foreach (var consumer in consumers)
                    consumer.EnableConsumption(false);
            }
        }
    }
}
