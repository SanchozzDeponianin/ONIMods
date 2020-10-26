using Klei.AI;
using UnityEngine;

namespace DeathReimagined
{
    // при слишком высоком стрессе вместо срыва случается инфаркт с некоторым шансом
    // шанс повышается при увеличении возраста дупла. (на основе логарифма)
    // если уже есть инфаркт и дуплик не на койке или не вылечен - еще раз уронить дуплика и продлить болезнь

    public class HeartAttackMonitor : GameStateMachine<HeartAttackMonitor, HeartAttackMonitor.Instance>
    {
        public const string ATTRIBUTE_ID = "HeartAttackSusceptibility";

        // фактор чуйствительности к инфаркту (умножается на логарифм возраста)
        private const float factor = 0.1f;

        public new class Instance : GameInstance
        {
            private MinionIdentity minionIdentity;
            private Effects effects;
            private Sicknesses sicknesses;
            private Health health;
            private float age => GameClock.Instance.GetCycle() - minionIdentity?.arrivalTime ?? 0;
            public AttributeModifier baseHeartAttackChance; // базовый шанс 

            public Instance(IStateMachineTarget master) : base(master)
            {
                minionIdentity = master.GetComponent<MinionIdentity>();
                effects = master.GetComponent<Effects>();
                sicknesses = master.gameObject.GetSicknesses();
                health = master.GetComponent<Health>();
                baseHeartAttackChance = new AttributeModifier(ATTRIBUTE_ID, 0, STRINGS.DUPLICANTS.ATTRIBUTES.HEARTATTACKSUSCEPTIBILITY.AGE_MODIFIER, false, false, false);
            }

            public void Update()
            {
                float chance = Mathf.Clamp01(Mathf.Log10(age) * factor);
                baseHeartAttackChance.SetValue(chance);
            }

            private void Incapacitate()
            {
                health?.Incapacitate(Db.Get().Deaths.FatalDisease);
                //health.Damage(health.hitPoints);
            }

            // сброс стресса
            private void ResetStress()
            {
                StressBehaviourMonitor.Instance stress_smi = this.GetSMI<StressBehaviourMonitor.Instance>();
                if (stress_smi != null && stress_smi.IsInsideState(stress_smi.sm.stressed))
                {
                    stress_smi.GoTo(stress_smi.sm.stressed);
                }
            }

            public void OnEnter(object data)
            {
                if (sicknesses != null && effects != null && sicknesses.Has(Db.Get().Sicknesses.Get(HeartAttackSickness.ID)) && !effects.HasEffect("MedicalCotDoctored") && !effects.HasEffect("MedicalCot") && !effects.HasEffect("DoctoredOffCotEffect"))
                {
                    Incapacitate();
                }
            }

            public void OnStressed()
            {
                Sickness heartattacksickness = Db.Get().Sicknesses.Get(HeartAttackSickness.ID);

                if (sicknesses != null && effects != null)
                {
                    if (!sicknesses.Has(heartattacksickness))
                    {
                        // новый инфаркт
                        float chance = Db.Get().Attributes.Get(ATTRIBUTE_ID).Lookup(gameObject)?.GetTotalValue() ?? 0;
                        if (Random.value < chance)
                        {
                            sicknesses.Infect(new SicknessExposureInfo(HeartAttackSickness.ID, STRINGS.DUPLICANTS.DISEASES.HEARTATTACKSICKNESS.EXPOSURE));
                            Incapacitate();
                            ResetStress();
                        }
                    }
                    // повторный инфаркт, если поциент не лежит на койке или не вылечен
                    else if (!effects.HasEffect("MedicalCotDoctored"))
                    {
                        sicknesses.Get(heartattacksickness).SetPercentCured(0);
                        Incapacitate();
                        ResetStress();
                    }
                }                
            }
        }

        public override void InitializeStates(out BaseState default_state)
        {
            serializable = false;
            default_state = root;

            root
                .Enter((Instance smi) => smi.Schedule(0.5f, smi.OnEnter))
                .Enter((Instance smi) => smi.Update())
                .Update((Instance smi, float dt) => smi.Update(), UpdateRate.SIM_4000ms)
                .ToggleAttributeModifier(ATTRIBUTE_ID, (Instance smi) => smi.baseHeartAttackChance)
                .EventHandler(GameHashes.StressedHadEnough, (Instance smi) => smi.OnStressed());
                //.EventHandler(GameHashes.SleepFinished, (Instance smi) => smi.OnStressed()); // todo: для отладки! 
        }
    }
}
