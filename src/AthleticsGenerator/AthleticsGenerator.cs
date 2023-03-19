using System;
using UnityEngine;
using Klei.AI;
using TUNING;

namespace AthleticsGenerator
{
    [SkipSaveFileSerialization]
    public class AthleticsGenerator : KMonoBehaviour
    {
#pragma warning disable CS0649
        [MyCmpReq]
        Generator generator;

        [MyCmpReq]
        ManualGenerator manualGenerator;

        [MyCmpReq]
        Operational operational;
#pragma warning restore CS0649

        AttributeModifier modifier;
        MeterController meter;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            modifier = new AttributeModifier(Db.Get().Attributes.GeneratorOutput.Id, 0, "deskr", is_readonly: false);
            gameObject.GetAttributes().Add(modifier);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (AthleticsGeneratorOptions.Instance.enable_meter)
            {
                meter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_target", "meter", Meter.Offset.Infront, Grid.SceneLayer.NoLayer, "meter_target", "meter_fill", "meter_frame", "meter_light", "meter_tubing");
                UpdateMeter();
            }
            manualGenerator.OnWorkableEventCB += OnWorkableEvent;
            Subscribe((int)GameHashes.WorkableStopWork, OnWorkableStopWork);
        }

        protected override void OnCleanUp()
        {
            if (manualGenerator != null)
                manualGenerator.OnWorkableEventCB -= OnWorkableEvent;
            Unsubscribe((int)GameHashes.WorkableStopWork, OnWorkableStopWork);
            base.OnCleanUp();
        }

        private void OnWorkableEvent(Workable workable, Workable.WorkableEvent ev)
        {
            Action<object> handler = _ => UpdateModifier();
            if (ev == Workable.WorkableEvent.WorkStarted)
            {
                UpdateModifier();
                if (workable != null && workable.worker != null)
                {
                    workable.worker.Subscribe((int)GameHashes.LevelUp, handler);
                    workable.worker.Subscribe((int)GameHashes.EffectAdded, handler);
                    workable.worker.Subscribe((int)GameHashes.EffectRemoved, handler);
                    workable.worker.Subscribe((int)GameHashes.SicknessAdded, handler);
                    workable.worker.Subscribe((int)GameHashes.SicknessCured, handler);
                }
            }
            else if (ev == Workable.WorkableEvent.WorkStopped)
            {
                if (workable != null && workable.worker != null)
                {
                    workable.worker.Unsubscribe((int)GameHashes.LevelUp, handler);
                    workable.worker.Unsubscribe((int)GameHashes.EffectAdded, handler);
                    workable.worker.Unsubscribe((int)GameHashes.EffectRemoved, handler);
                    workable.worker.Unsubscribe((int)GameHashes.SicknessAdded, handler);
                    workable.worker.Unsubscribe((int)GameHashes.SicknessCured, handler);
                }
                modifier.SetValue(0f);
            }
        }

        private void OnWorkableStopWork(object _) => UpdateMeter();

        private void UpdateModifier()
        {
            if (manualGenerator.worker != null)
            {
                float bonus = AthleticsGeneratorPatches.ManualGeneratorPower.Lookup(manualGenerator.worker).Evaluate();
                modifier.SetValue(bonus / generator.BaseWattageRating * 100f);
                UpdateMeter();
            }
        }

        private void UpdateMeter()
        {
            if (meter != null)
            {
                float rating = !operational.IsActive ? 0f : generator.WattageRating / (generator.BaseWattageRating + DUPLICANTSTATS.ATTRIBUTE_LEVELING.MAX_GAINED_ATTRIBUTE_LEVEL * AthleticsGeneratorOptions.Instance.watts_per_level);
                meter.SetPositionPercent(Mathf.Clamp01(rating));
            }
        }
    }
}
