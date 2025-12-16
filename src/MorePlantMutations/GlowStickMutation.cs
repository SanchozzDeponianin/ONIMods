using Klei.AI;
using TUNING;
using UnityEngine;

namespace MorePlantMutations
{
    public class GlowStickMutation : StateMachineComponent<GlowStickMutation.StatesInstance>
    {
        public static Attribute EmitRadsMultiplier;
        public static AttributeModifier BaseEmitRadsRate;
        public static AttributeModifier WildEmitRadsRate;
        public static AttributeModifier Luminescence;

        public static void CreateModifiers()
        {
            Luminescence = new AttributeModifier(Db.Get().Attributes.Luminescence.Id, TRAITS.GLOWSTICK_LUX_VALUE, STRINGS.CREATURES.PLANT_MUTATIONS.GLOWSTICK.NAME);

            // в основном всё ради интеграции с BetterPlantTending
            EmitRadsMultiplier = new Attribute(
                id: nameof(GlowStickMutation) + "." + nameof(EmitRadsMultiplier),
                is_trainable: false,
                show_in_ui: Attribute.Display.Details,
                is_profession: false,
                base_value: 0f);
            Db.Get().PlantAttributes.Add(EmitRadsMultiplier);

            BaseEmitRadsRate = new AttributeModifier(
                attribute_id: EmitRadsMultiplier.Id,
                value: 1f,
                is_multiplier: false);

            WildEmitRadsRate = new AttributeModifier(
                attribute_id: EmitRadsMultiplier.Id,
                value: ModOptions.Instance.glowstick.decrease_radiation_by_wildness ? CROPS.WILD_GROWTH_RATE_MODIFIER : 1f,
                is_multiplier: false);

            if (ModOptions.Instance.glowstick.adjust_radiation_by_grow_speed)
            {
                var maturity = Db.Get().Amounts.Maturity.deltaAttribute.Id;
                foreach (var effect in Db.Get().effects.resources)
                {
                    foreach (var modifier in effect.SelfModifiers)
                    {
                        if (modifier.AttributeId == maturity && modifier.IsMultiplier && !modifier.UIOnly)
                        {
                            var rads_rate = new AttributeModifier(
                                attribute_id: EmitRadsMultiplier.Id,
                                value: modifier.Value,
                                is_multiplier: true);
                            effect.Add(rads_rate);
                            break;
                        }
                    }
                }
            }
        }

        public class StatesInstance : GameStateMachine<States, StatesInstance, GlowStickMutation>.GameInstance
        {
            [MyCmpAdd]
            public RadiationEmitter emitter;

            [MyCmpGet]
            public IlluminationVulnerable illumination;

            private bool isBranch;
            private AttributeInstance emitMultiplier;
            private IManageGrowingStates growing;

            public StatesInstance(GlowStickMutation master) : base(master)
            {
                isBranch = HasTag(GameTags.PlantBranch);
                emitter.emitType = RadiationEmitter.RadiationEmitterType.Constant;
                emitter.radiusProportionalToRads = false;
                emitter.emitRadiusX = 6;
                emitter.emitRadiusY = 6;
                emitter.emissionOffset = Vector3.zero;
                emitter.emitRads = ColdBreatherConfig.RADIATION_STRENGTH;
                emitter.SetEmitting(false);
                var attributes = master.GetAttributes();
                emitMultiplier = attributes.Get(EmitRadsMultiplier);
                if (emitMultiplier == null)
                    emitMultiplier = attributes.Add(EmitRadsMultiplier);
                if (attributes.Get(Db.Get().Attributes.Luminescence) == null)
                    attributes.Add(Db.Get().Attributes.Luminescence);
                growing = Get<IManageGrowingStates>();
                if (growing.IsNullOrDestroyed())
                    growing = gameObject.GetSMI<IManageGrowingStates>();
            }

            public bool IsWildPlanted() => !growing.IsNullOrDestroyed() && growing.IsWildPlanted();

            public bool IsPrefersDarkness() => !illumination.IsNullOrDestroyed() && illumination.prefersDarkness;

            public float GetEmitRads()
            {
                var rads = isBranch ? RADIATION.RADIATION_PER_SECOND.TRIVIAL : ColdBreatherConfig.RADIATION_STRENGTH;
                rads *= emitMultiplier.GetTotalValue();
                return rads;
            }

            public void RefreshRads()
            {
                var rads = GetEmitRads();
                if (rads != emitter.emitRads)
                {
                    emitter.emitRads = rads;
                    emitter.Refresh();
                }
            }
        }

        public class States : GameStateMachine<States, StatesInstance, GlowStickMutation>
        {
            public class GrowingStates : State
            {
                public State wild;
                public State planted;
            }

            public GrowingStates growing;
            public State wilting;

            public override void InitializeStates(out BaseState default_state)
            {
                serializable = SerializeType.Never;
                default_state = growing;
                root.ToggleComponent<RadiationEmitter>(false);
                if (ModOptions.Instance.glowstick.emit_light)
                {
                    root.ToggleStateMachine(smi =>
                    {
                        var def = new EntityLuminescence.Def()
                        {
                            lightColor = Color.green,
                            lightRange = smi.HasTag(GameTags.PlantBranch) ? 1f : 2f,
                            lightAngle = 0f,
                            lightDirection = LIGHT2D.DEFAULT_DIRECTION,
                            lightOffset = new Vector2(0.05f, 0.5f),
                            lightShape = LightShape.Circle
                        };
                        return new EntityLuminescence.Instance(smi.master, def);
                    });
                }

                growing
                    .TagTransition(GameTags.Wilting, wilting, false)
                    .ToggleAttributeModifier("Luminescence", smi => Luminescence, smi => !smi.IsPrefersDarkness())
                    .Enter(smi => smi.emitter.SetEmitting(true))
                    .EventHandler(GameHashes.EffectAdded, (smi, data) => smi.RefreshRads())
                    .EventHandler(GameHashes.EffectRemoved, (smi, data) => smi.RefreshRads())
                    .EventTransition(GameHashes.PlanterStorage, growing.wild, smi => smi.IsWildPlanted())
                    .EventTransition(GameHashes.PlanterStorage, growing.planted, smi => !smi.IsWildPlanted())
                    .EventTransition(GameHashes.ReceptacleMonitorChange, growing.wild, smi => smi.IsWildPlanted())
                    .EventTransition(GameHashes.ReceptacleMonitorChange, growing.planted, smi => !smi.IsWildPlanted())
                    .EnterTransition(growing.planted, smi => !smi.IsWildPlanted())
                    .DefaultState(growing.wild)
                    .Exit(smi => smi.emitter.SetEmitting(false));

                growing.wild
                    .ToggleAttributeModifier("Wild", smi => WildEmitRadsRate)
                    .ScheduleAction("", 2 * UpdateManager.SecondsPerSimTick, smi => smi.RefreshRads());

                growing.planted
                    .ToggleAttributeModifier("Planted", smi => BaseEmitRadsRate)
                    .ScheduleAction("", 2 * UpdateManager.SecondsPerSimTick, smi => smi.RefreshRads());

                wilting
                    .TagTransition(GameTags.Wilting, growing, true)
                    .DoNothing();
            }
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            smi.StartSM();
        }
    }
}
