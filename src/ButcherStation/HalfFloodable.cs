namespace ButcherStation
{
    public class HalfFloodable : Floodable
    {
        // полузатопляемость. не проверяем нижний слой постройки

        public override void OnSpawn()
        {
            var extents = building.GetExtents();
            extents.y += 1;
            extents.height -= 1;
            partitionerEntry = GameScenePartitioner.Instance.Add("Floodable.OnSpawn", gameObject, extents,
                GameScenePartitioner.Instance.liquidChangedLayer, OnElementChanged);
            OnElementChanged(null);
        }

        public override void OnCleanUp()
        {
            base.OnCleanUp();
        }

        private new void OnElementChanged(object data)
        {
            bool flooded = false;
            for (int i = building.Def.WidthInCells; i < building.PlacementCells.Length; i++)
            {
                int cell = building.PlacementCells[i];
                if (Grid.IsSubstantialLiquid(cell))
                {
                    flooded = true;
                    break;
                }
            }
            if (flooded != isFlooded)
            {
                isFlooded = flooded;
                operational.SetFlag(notFloodedFlag, !isFlooded);
                GetComponent<KSelectable>().ToggleStatusItem(Db.Get().BuildingStatusItems.Flooded, isFlooded, this);
            }
        }
    }
}
