using Klei.AI;

namespace SanchozzONIMods.Lib
{
    public static class EffectsExtensions
    {
        public static EffectInstance AddOrExtend(this Effects effects, string id, bool should_save)
        {
            return effects.AddOrExtend((HashedString)id, should_save);
        }

        public static EffectInstance AddOrExtend(this Effects effects, HashedString id, bool should_save)
        {
            var effect = Db.Get().effects.TryGet(id);
            if (effect != null)
                return effects.AddOrExtend(effect, should_save);
            else
            {
                Debug.LogWarningFormat("Could not find Effect: {0}", id);
                return null;
            }
        }

        public static EffectInstance AddOrExtend(this Effects effects, Effect effect, bool should_save)
        {
            var effectInstance = effects.Get(effect);
            if (effectInstance == null)
                effectInstance = effects.Add(effect, should_save);
            else
                effectInstance.timeRemaining += effect.duration;
            return effectInstance;
        }

#if false
        public static void AddEffectToAllLiveMinions(string effect_id, bool should_save, bool extend = false)
        {
            foreach (MinionIdentity minionIdentity in Components.LiveMinionIdentities.Items)
            {
                Effects effects = minionIdentity.GetComponent<Effects>();
                if (extend)
                {
                    effects.AddOrExtend(effect_id, should_save);
                }
                else
                {
                    effects.Add(effect_id, should_save);
                }
            }
        }
#endif
    }
}
