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
        private Generator generator;

        [MyCmpReq]
        private ManualGenerator manualGenerator;

        [MyCmpReq]
        private Operational operational;
#pragma warning restore CS0649

        private AttributeModifier modifier;
        private AttributeConverterInstance converter;
        private MeterController meter;

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
        }

        protected override void OnCleanUp()
        {
            if (manualGenerator != null)
                manualGenerator.OnWorkableEventCB -= OnWorkableEvent;
            base.OnCleanUp();
        }

        private void OnWorkableEvent(Workable workable, Workable.WorkableEvent ev)
        {
            Action<object> handler = _ => UpdateModifier();
            if (ev == Workable.WorkableEvent.WorkStarted)
            {
                if (workable != null && workable.worker != null)
                {
                    converter = workable.worker.GetAttributeConverter(AthleticsGeneratorPatches.ManualGeneratorPower.Id);
                    var real_worker = converter.gameObject;
                    real_worker.Subscribe((int)GameHashes.LevelUp, handler);
                    real_worker.Subscribe((int)GameHashes.EffectAdded, handler);
                    real_worker.Subscribe((int)GameHashes.EffectRemoved, handler);
                    real_worker.Subscribe((int)GameHashes.SicknessAdded, handler);
                    real_worker.Subscribe((int)GameHashes.SicknessCured, handler);
                }
                UpdateModifier();
            }
            else if (ev == Workable.WorkableEvent.WorkStopped)
            {
                if (converter != null && !converter.isNull)
                {
                    var real_worker = converter.gameObject;
                    real_worker.Unsubscribe((int)GameHashes.LevelUp, handler);
                    real_worker.Unsubscribe((int)GameHashes.EffectAdded, handler);
                    real_worker.Unsubscribe((int)GameHashes.EffectRemoved, handler);
                    real_worker.Unsubscribe((int)GameHashes.SicknessAdded, handler);
                    real_worker.Unsubscribe((int)GameHashes.SicknessCured, handler);
                    converter = null;
                }
                UpdateModifier();
            }
        }

        private void UpdateModifier()
        {
            if (converter != null && !converter.isNull)
                modifier.SetValue(converter.Evaluate() / generator.BaseWattageRating * 100f);
            else
                modifier.SetValue(0f);
            UpdateMeter();
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
