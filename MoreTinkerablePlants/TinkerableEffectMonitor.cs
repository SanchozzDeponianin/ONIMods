using System;
using Klei.AI;

namespace MoreTinkerablePlants
{
    public class TinkerableEffectMonitor : KMonoBehaviour
    {
        protected const float PLANTTHROUGHPUTMODIFIER = 3f;
        public const string FARMTINKEREFFECTID = "FarmTinker";

        [MyCmpReq]
        protected Effects effects;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.EffectAdded,   new Action<object>(OnEffectChanged));
            Subscribe((int)GameHashes.EffectRemoved, new Action<object>(OnEffectChanged));
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.EffectAdded,   new Action<object>(OnEffectChanged));
            Unsubscribe((int)GameHashes.EffectRemoved, new Action<object>(OnEffectChanged));
            base.OnCleanUp();
        }

        private void OnEffectChanged(object data)
        {
            ApplyEffect();
        }

        public virtual void ApplyEffect()
        {
        }
    }
}
