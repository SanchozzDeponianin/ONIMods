namespace BetterPlantTending
{
    public class TendedBlueGrass : TendedPlant
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private BlueGrass blueGrass;
#pragma warning restore CS0649

        public override void ApplyModifier()
        {
            blueGrass.SetConsumptionRate();
        }
    }
}
