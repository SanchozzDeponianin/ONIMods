namespace Smelter
{
    // старая анимация смещена.
    public class SmelterWorkable : ComplexFabricatorWorkable
    {
        protected override void OnSpawn()
        {
            base.OnSpawn();
            SetOffsets(new[] { CellOffset.left });
        }
    }
}
