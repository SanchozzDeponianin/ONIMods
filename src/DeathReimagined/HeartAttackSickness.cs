using System.Collections.Generic;
using Klei.AI;
using TUNING;
using UnityEngine;

namespace DeathReimagined
{
    public class HeartAttackSickness : Sickness
    {
        public const string ID = "HeartAttackSickness";
        //public const string RECOVERY_ID = "HeartAttackSicknessRecovery";
        public const int ATTRIBUTE_PENALTY = -7;

        // компонент болезни. для дополнительного описания симптомов.
        public class HeartAttackComponent : SicknessComponent
        {
            public HeartAttackComponent()
            {
            }

            public override object OnInfect(GameObject go, SicknessInstance diseaseInstance)
            {
                return null;
            }

            public override void OnCure(GameObject go, object instance_data)
            {
            }

            public override List<Descriptor> GetSymptoms()
            {
                List<Descriptor> list = new List<Descriptor>()
                {
                    new Descriptor(STRINGS.DUPLICANTS.DISEASES.HEARTATTACKSICKNESS.SYMPTOMS, "tooltip", Descriptor.DescriptorType.Symptom, false)
                };
                return list;
            }
        }

        public HeartAttackSickness() : base(ID, SicknessType.Ailment, Severity.Critical, 0.00025f, new List<InfectionVector>{ InfectionVector.Exposure }, DISEASE.DURATION.NORMAL)
        {
            fatalityDuration = 0.5f * Constants.SECONDS_PER_CYCLE;

            // понижение атрибутов 
            LocString name = STRINGS.DUPLICANTS.DISEASES.HEARTATTACKSICKNESS.NAME;
            AddSicknessComponent(new AttributeModifierSickness(new AttributeModifier[]
            {
                new AttributeModifier(Db.Get().Attributes.Athletics.Id, ATTRIBUTE_PENALTY, name, false, false, true),
                new AttributeModifier(Db.Get().Attributes.Strength.Id, ATTRIBUTE_PENALTY, name, false, false, true),
                new AttributeModifier(Db.Get().Attributes.Digging.Id, ATTRIBUTE_PENALTY, name, false, false, true),
                new AttributeModifier(Db.Get().Attributes.Construction.Id, ATTRIBUTE_PENALTY, name, false, false, true),
                new AttributeModifier(Db.Get().Attributes.Art.Id, ATTRIBUTE_PENALTY, name, false, false, true),
                new AttributeModifier(Db.Get().Attributes.Caring.Id, ATTRIBUTE_PENALTY, name, false, false, true),
                new AttributeModifier(Db.Get().Attributes.Learning.Id, ATTRIBUTE_PENALTY, name, false, false, true),
                new AttributeModifier(Db.Get().Attributes.Machinery.Id, ATTRIBUTE_PENALTY, name, false, false, true),
                new AttributeModifier(Db.Get().Attributes.Cooking.Id, ATTRIBUTE_PENALTY, name, false, false, true),
                new AttributeModifier(Db.Get().Attributes.Botanist.Id, ATTRIBUTE_PENALTY, name, false, false, true),
                new AttributeModifier(Db.Get().Attributes.Ranching.Id, ATTRIBUTE_PENALTY, name, false, false, true),
                new AttributeModifier(Db.Get().Amounts.Stamina.deltaAttribute.Id, ModifierSet.ConvertValue(-100, Units.PerDay), name, false, false, true)
            }));

            // для дополнительного описания симптомов
            AddSicknessComponent(new HeartAttackComponent());

            // анимация :
            AddSicknessComponent(new CommonSickEffectSickness());
            //AddSicknessComponent(new CustomSickEffectSickness("spore_fx_kanim", "working_loop"));

            AddSicknessComponent(new AnimatedSickness(new HashedString[]
            {
                "anim_idle_cold_kanim",
                "anim_loco_run_cold_kanim",
                "anim_loco_walk_cold_kanim"
            }, Db.Get().Expressions.SickCold));

            AddSicknessComponent(new PeriodicEmoteSickness("anim_idle_cold_kanim", new HashedString[]
            {
                "idle_pre",
                "idle_default",
                "idle_pst"
            }, BUILDINGS.WORK_TIME_SECONDS.SHORT_WORK_TIME));
        }
    }
}
