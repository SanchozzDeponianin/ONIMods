using Klei.AI;

namespace SanchozzONIMods.Lib
{
    public static class EffectsExtensions
    {
        public static EffectInstance AddOrExtend(this Effects effects, string effect_id, bool should_save)
        {
            Effect effect = Db.Get().effects.Get(effect_id);
            return effects.AddOrExtend(effect, should_save);
        }

        public static EffectInstance AddOrExtend(this Effects effects, Effect effect, bool should_save)
        {
            EffectInstance effectInstance = effects.Get(effect);
            if (effectInstance == null)
            {
                effectInstance = effects.Add(effect, should_save);
            }
            else
            {
                effectInstance.timeRemaining += effect.duration;
            }
            return effectInstance;
        }

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
    }
}
	