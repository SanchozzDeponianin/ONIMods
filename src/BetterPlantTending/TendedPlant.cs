using System;
using SanchozzONIMods.Lib;

namespace BetterPlantTending
{
    public abstract class TendedPlant : KMonoBehaviour
    {
        private static readonly Action<object, object> OnEffectChangedDelegate = (cmp, data) => ((TendedPlant)cmp).ApplyModifier();
        private static readonly Action<object, object> OnGrowDelegate = (cmp, data) => ((TendedPlant)cmp).QueueApplyModifier();

        private SchedulerHandle updateHandle;

        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            ApplyModifierCallback = ApplyModifier;
            this.SubscribeX(GameHashes.EffectAdded, OnEffectChangedDelegate);
            this.SubscribeX(GameHashes.EffectRemoved, OnEffectChangedDelegate);
            this.SubscribeX(GameHashes.Grow, OnGrowDelegate);
            this.SubscribeX(GameHashes.Wilt, OnGrowDelegate);
            this.SubscribeX(GameHashes.WiltRecover, OnGrowDelegate);
        }

        public override void OnCleanUp()
        {
            this.UnsubscribeAll();
            if (updateHandle.IsValid)
                updateHandle.ClearScheduler();
            base.OnCleanUp();
        }

        public virtual void ApplyModifier(object _ = null) { }
        private Action<object> ApplyModifierCallback;

        public void QueueApplyModifier()
        {
            if (updateHandle.IsValid)
                updateHandle.ClearScheduler();
            updateHandle = GameScheduler.Instance.Schedule("QueueApplyModifier", 2 * UpdateManager.SecondsPerSimTick, ApplyModifierCallback);
        }
    }
}
