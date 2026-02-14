namespace NoManualDelivery
{
    // для отслеживания областей досягаемости только рук
    public class TransferArmGroupProber : MinionGroupProber, ISim33ms
    {
        private static new TransferArmGroupProber Instance;
        public static new void DestroyInstance() => Instance = null;
        public static new MinionGroupProber Get() => Instance;

        // иницыализация с избеганием перезаписи base.Instance
        public override void OnPrefabInit()
        {
            Instance = this;
            cells = new int[Grid.CellCount];
        }

        // ультрамикро-оптимизация не дёргать время
        public override void OnSpawn()
        {
            base.OnSpawn();
            if (GameClock.Instance)
                Sim33ms(0f);
        }

        public void Sim33ms(float dt) => AutomatableHolder.CurrentTime = GameClock.Instance.GetTime();
    }
}
