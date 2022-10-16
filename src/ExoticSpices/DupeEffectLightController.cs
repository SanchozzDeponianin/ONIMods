using System.Collections.Generic;
using UnityEngine;
using Klei.AI;
using TUNING;

namespace ExoticSpices
{
    using static ExoticSpicesAssets;

    // скопипизжено и пересобачено со светлячков (CreatureLightToggleController и LightSymbolTracker)
    // попробуем сделать красявости. плавное включение/выключение свечения при добавлении/убирании эффекта
    public class DupeEffectLightController : GameStateMachine<DupeEffectLightController, DupeEffectLightController.Instance, IStateMachineTarget, DupeEffectLightController.Def>
    {
        public class Def : BaseDef
        {
            public string trackingEffectId = PhosphoRufusSpice.Id;
            public Color Color = Color.cyan;
            public float Range = ExoticSpicesOptions.Instance.phospho_rufus_spice.range;
            public int Lux = ExoticSpicesOptions.Instance.phospho_rufus_spice.lux;
            public float dim_time = 15f;
            public float glow_time = 10f;
        }

        public new class Instance : GameInstance
        {
            private Effects effects;
            private KBatchedAnimController kbac;
            private Light2D light;
            private HashedString targetSymbol = "snapto_headshape";

            public Instance(IStateMachineTarget master, Def def) : base(master, def)
            {
                effects = master.GetComponent<Effects>();
                kbac = master.GetComponent<KBatchedAnimController>();
                light = master.gameObject.AddComponent<Light2D>();
                light.Color = def.Color;
                light.overlayColour = LIGHT2D.LIGHTBUG_OVERLAYCOLOR;
                light.Offset = Vector2.up;
                light.Range = def.Range;
                light.Angle = 0f;
                light.Direction = LIGHT2D.DEFAULT_DIRECTION;
                light.Lux = def.Lux;
                light.shape = LightShape.Circle;
                light.drawOverlay = true;
                light.enabled = false;
            }

            public void SwitchLight(bool on) => light.enabled = on;
            public bool IsOff() => light.Lux <= 0;
            public bool IsOn() => light.Lux >= def.Lux;
            public bool ShouldLight() => effects.HasEffect(def.trackingEffectId);
            public bool ShouldLightAtStart()
            {
                var effect = effects.Get(def.trackingEffectId);
                return (effect != null && effect.effect.duration - effect.timeRemaining > def.glow_time);
            }

            private struct ModifyBrightnessTask : IWorkItem<object>
            {
                private LightGridManager.LightGridEmitter emitter;
                public ModifyBrightnessTask(LightGridManager.LightGridEmitter emitter)
                {
                    this.emitter = emitter;
                    emitter.RemoveFromGrid();
                }
                public void Run(object context) => emitter.UpdateLitCells();
                public void Finish() => emitter.AddToGrid(false);
            }

            private static WorkItemCollection<ModifyBrightnessTask, object> modify_brightness_job =
                new WorkItemCollection<ModifyBrightnessTask, object>();

            public delegate void ModifyLuxDelegate(Instance instance, float dt);

            public static ModifyLuxDelegate dim = (instance, dt) =>
            {
                instance.light.Lux = Mathf.FloorToInt(Mathf.Max(0f, instance.light.Lux - instance.def.Lux / instance.def.dim_time * dt));
            };
            public static ModifyLuxDelegate brighten = (instance, dt) =>
            {
                instance.light.Lux = Mathf.CeilToInt(Mathf.Min(instance.def.Lux, instance.light.Lux + instance.def.Lux / instance.def.glow_time * dt));
            };

            public static void ModifyBrightness(List<UpdateBucketWithUpdater<Instance>.Entry> instances, ModifyLuxDelegate modify_lux, float dt)
            {
                modify_brightness_job.Reset(null);
                for (int i = 0; i < instances.Count; i++)
                {
                    var entry = instances[i];
                    entry.lastUpdateTime = 0f;
                    instances[i] = entry;
                    var instance = entry.data;
                    modify_lux(instance, dt);
                    instance.light.Range = instance.def.Range * instance.light.Lux / instance.def.Lux;
                    instance.light.RefreshShapeAndPosition();
                    if (instance.light.RefreshShapeAndPosition() != Light2D.RefreshResult.None)
                    {
                        modify_brightness_job.Add(new ModifyBrightnessTask(instance.light.emitter));
                    }
                }
                GlobalJobManager.Run(modify_brightness_job);
                for (int j = 0; j < modify_brightness_job.Count; j++)
                {
                    modify_brightness_job.GetWorkItem(j).Finish();
                }
                modify_brightness_job.Reset(null);
            }

            public static void ModifyOffset(List<UpdateBucketWithUpdater<Instance>.Entry> instances, float dt)
            {
                for (int i = 0; i < instances.Count; i++)
                {
                    var entry = instances[i];
                    entry.lastUpdateTime = 0f;
                    instances[i] = entry;
                    var instance = entry.data;
                    instance.light.Offset = (instance.kbac.GetTransformMatrix() * instance.kbac.GetSymbolLocalTransform(instance.targetSymbol, out _)).MultiplyPoint(Vector3.zero) - instance.transform.GetPosition();
                }
            }
        }

#pragma warning disable CS0649
        private class LightOnState : State
        {
            public State turning_on;
            public State normal;
            public State turning_off;
        }
        private State light_off;
        private LightOnState light_on;
#pragma warning restore CS0649

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = light_off;
            serializable = SerializeType.Both_DEPRECATED;
            root
                .EnterTransition(light_on.normal, smi => smi.ShouldLightAtStart());
            light_off
                .Enter(smi => smi.SwitchLight(false))
                .EnterTransition(light_on.turning_on, smi => smi.ShouldLight())
                .EventTransition(GameHashes.EffectAdded, light_on.turning_on, smi => smi.ShouldLight());
            light_on
                .DefaultState(light_on.normal)
                .Enter(smi => smi.SwitchLight(true))
                .Exit(smi => smi.SwitchLight(false))
                .ToggleTag(GameTags.EmitsLight)
                .BatchUpdate((items, dt) => Instance.ModifyOffset(items, dt), UpdateRate.RENDER_EVERY_TICK);
            light_on.turning_on
                .BatchUpdate((items, dt) => Instance.ModifyBrightness(items, Instance.brighten, dt), UpdateRate.SIM_200ms)
                .Transition(light_on.normal, (Instance smi) => smi.IsOn(), UpdateRate.SIM_200ms);
            light_on.normal
                .EnterTransition(light_on.turning_off, smi => !smi.ShouldLight())
                .EventTransition(GameHashes.EffectRemoved, light_on.turning_off, smi => !smi.ShouldLight());
            light_on.turning_off
                .BatchUpdate((items, dt) => Instance.ModifyBrightness(items, Instance.dim, dt), UpdateRate.SIM_200ms)
                .Transition(light_off, (Instance smi) => smi.IsOff(), UpdateRate.SIM_200ms);
        }
    }
}
