namespace BetterPlantTending
{
    public class TendedDinofern : TendedPlant
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Dinofern dinofern;
#pragma warning restore CS0649

        public override void ApplyModifier()
        {
            dinofern.SetConsumptionRate();
        }
    }
}
