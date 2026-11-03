using System.Collections.Generic;
using Klei.AI;
using UnityEngine;

namespace SanchozzONIMods.Shared
{
    // основа для замораживания таймера истечения эффектов в зависимости от условий
    [SkipSaveFileSerialization]
    public abstract class FreezeEffectDurationBase : KMonoBehaviour
    {
        [SerializeField]
        public List<HashedString> effectsToFreeze = new();

#pragma warning disable CS0649
        [MyCmpReq]
        protected Effects effects;
#pragma warning restore CS0649

        protected virtual bool ShouldFreeze => false;

        protected override void OnPrefabInit()
        {
            Debug.Assert(effectsToFreeze != null, $"{nameof(effectsToFreeze)} is not set");
            base.OnPrefabInit();
            hOnEffectAdded = Subscribe((int)GameHashes.EffectAdded, OnEffectAddedDelegate, this);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe(hOnEffectAdded);
            base.OnCleanUp();
        }

        private int hOnEffectAdded;
        private static readonly System.Action<object, object> OnEffectAddedDelegate = (cmp, data) => ((FreezeEffectDurationBase)cmp).OnEffectAdded(data);
        private void OnEffectAdded(object data)
        {
            if (ShouldFreeze && data is Effect effect && effectsToFreeze.Contains(effect.IdHash))
                Freeze(effect.IdHash);
        }

        protected void Freeze(HashedString effect_id)
        {
            var instance = effects.Get(effect_id);
            if (instance != null)
            {
                var effectsThatExpire = effects.GetTimeLimitedEffects();
                // effectsThatExpire.Remove(instance);
                // этот кусок скопирован из Effects.Remove , удаляет из конца, возможно в этом есть смысл
                for (int i = 0; i < effectsThatExpire.Count; i++)
                {
                    if (effectsThatExpire[i].effect.IdHash == effect_id)
                    {
                        int last = effectsThatExpire.Count - 1;
                        effectsThatExpire[i] = effectsThatExpire[last];
                        effectsThatExpire.RemoveAt(last);
                        if (effectsThatExpire.Count == 0)
                            SimAndRenderScheduler.instance.Remove(effects);
                        break;
                    }
                }
            }
        }

        protected void FreezeAll()
        {
            foreach (var effect_id in effectsToFreeze)
                Freeze(effect_id);
        }

        protected void UnFreeze(HashedString effect_id)
        {
            var instance = effects.Get(effect_id);
            if (instance != null && instance.effect.duration > 0)
            {
                var effectsThatExpire = effects.GetTimeLimitedEffects();
                if (!effectsThatExpire.Contains(instance))
                {
                    effectsThatExpire.Add(instance);
                    if (effectsThatExpire.Count == 1)
                        SimAndRenderScheduler.instance.Add(effects, simRenderLoadBalance);
                    // todo: правильнее было бы использовать effects.simRenderLoadBalance
                }
            }
        }

        protected void UnFreezeAll()
        {
            foreach (var effect_id in effectsToFreeze)
                UnFreeze(effect_id);
        }
    }

    // замораживаем если операцыональ неактивна
    [SkipSaveFileSerialization]
    public class OperationalNotActiveFreezeEffectDuration : FreezeEffectDurationBase
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private Operational operational;
#pragma warning restore CS0649

        protected override bool ShouldFreeze => !operational.IsActive;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            hOnActiveChanged = Subscribe((int)GameHashes.ActiveChanged, OnActiveChangedDelegate, this);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe(ref hOnActiveChanged);
            base.OnCleanUp();
        }

        private int hOnActiveChanged;
        private static readonly System.Action<object, object> OnActiveChangedDelegate
            = (cmp, data) => ((OperationalNotActiveFreezeEffectDuration)cmp).OnActiveChanged(data);
        private void OnActiveChanged(object data)
        {
            if (ShouldFreeze)
                FreezeAll();
            else
                UnFreezeAll();
        }
    }
}