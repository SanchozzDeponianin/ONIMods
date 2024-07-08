namespace BetterPlantTending
{
    using handler = EventSystem.IntraObjectHandler<TendedPlant>;
    public abstract class TendedPlant : KMonoBehaviour
    {
        private static readonly handler OnEffectChangedDelegate = new handler((component, data) => component.ApplyModifier());

        private SchedulerHandle updateHandle;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.EffectAdded, OnEffectChangedDelegate);
            Subscribe((int)GameHashes.EffectRemoved, OnEffectChangedDelegate);
        }

        protected override void OnCleanUp()
        {
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
