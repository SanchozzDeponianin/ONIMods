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
            if (ModOptions.Instance.enable_meter)
            {
                meter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_target", "meter", Meter.Offset.Infront, Grid.SceneLayer.NoLayer, "meter_target", "meter_fill", "meter_frame", "meter_light", "meter_tubing");
                UpdateMeter();
            }
            manualGenerator.OnWorkableEventCB += OnWorkableEvent;
            hOnWorkableStopWork = Subscribe((int)GameHashes.WorkableStopWork, OnWorkableStopWorkDelegate, this);
        }

        protected override void OnCleanUp()
        {
            if (manualGenerator != null)
                manualGenerator.OnWorkableEventCB -= OnWorkableEvent;
            Unsubscribe(ref hOnWorkableStopWork);
            base.OnCleanUp();
        }

        private static readonly GameHashes[] ModifiersHashes
            = { GameHashes.LevelUp, GameHashes.EffectAdded, GameHashes.EffectRemoved, GameHashes.SicknessAdded, GameHashes.SicknessCured };

        private readonly int[] hOnModifierUpdate = new int[ModifiersHashes.Length];
        private static readonly Action<object, object> OnModifierUpdateDelegate = (cmp, data) => ((AthleticsGenerator)cmp).UpdateModifier();

        private void OnWorkableEvent(Workable workable, Workable.WorkableEvent ev)
        {
            if (ev == Workable.WorkableEvent.WorkStarted)
            {
                if (workable != null && workable.worker != null)
                {
                    converter = workable.worker.GetAttributeConverter(Patches.ManualGeneratorPower.Id);
                    var real_worker = converter.gameObject;
                    for (int i = 0; i < ModifiersHashes.Length; i++)
                        hOnModifierUpdate[i] = real_worker.Subscribe((int)ModifiersHashes[i], OnModifierUpdateDelegate, this);
                }
                UpdateModifier();
            }
            else if (ev == Workable.WorkableEvent.WorkStopped)
            {
                if (converter != null && !converter.isNull)
                {
                    var real_worker = converter.gameObject;
                    for (int i = 0; i < ModifiersHashes.Length; i++)
                        real_worker.Unsubscribe(hOnModifierUpdate[i]);
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

        private int hOnWorkableStopWork;
        private static readonly Action<object, object> OnWorkableStopWorkDelegate = (cmp, data) => ((AthleticsGenerator)cmp).UpdateMeter();
        private void UpdateMeter()
        {
            if (meter != null)
            {
                float rating = !operational.IsActive ? 0f : generator.WattageRating / (generator.BaseWattageRating + DUPLICANTSTATS.ATTRIBUTE_LEVELING.MAX_GAINED_ATTRIBUTE_LEVEL * ModOptions.Instance.watts_per_level);
                meter.SetPositionPercent(Mathf.Clamp01(rating));
            }
        }
    }
}
