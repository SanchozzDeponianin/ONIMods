namespace BetterPlantTending
{
    using handler = EventSystem.IntraObjectHandler<TendedPlant>;
    public abstract class TendedPlant : KMonoBehaviour
    {
        private static readonly handler OnEffectChangedDelegate = new((component, data) => component.ApplyModifier());
        private static readonly handler OnGrowDelegate = new((component, data) => component.QueueApplyModifier());

        private SchedulerHandle updateHandle;

        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe((int)GameHashes.EffectAdded, OnEffectChangedDelegate);
            Subscribe((int)GameHashes.EffectRemoved, OnEffectChangedDelegate);
            Subscribe((int)GameHashes.Grow, OnGrowDelegate);
            Subscribe((int)GameHashes.Wilt, OnGrowDelegate);
            Subscribe((int)GameHashes.WiltRecover, OnGrowDelegate);
        }

        public override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.Grow, OnGrowDelegate);
            Unsubscribe((int)GameHashes.Wilt, OnGrowDelegate);
            Unsubscribe((int)GameHashes.WiltRecover, OnGrowDelegate);
            Unsubscribe((int)GameHashes.EffectAdded, OnEffectChangedDelegate);
            Unsubscribe((int)GameHashes.EffectRemoved, OnEffectChangedDelegate);
            if (updateHandle.IsValid)
                updateHandle.ClearScheduler();
            base.OnCleanUp();
        }

        public virtual void ApplyModifier() { }

        private void ApplyModifierCallback(object _) => ApplyModifier();

        public void QueueApplyModifier()
        {
            if (updateHandle.IsValid)
                updateHandle.ClearScheduler();
            updateHandle = GameScheduler.Instance.Schedule("QueueApplyModifier", 2 * UpdateManager.SecondsPerSimTick, ApplyModifierCallback);
        }
    }
}
