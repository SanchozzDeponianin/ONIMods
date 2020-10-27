using Klei.AI;
using UnityEngine;

namespace DeathReimagined
{
    // TODO: попробывать переписать чтобы сохранялось. нужно чщательно обдумать условия и формулы.
    // TODO: может быть переписать на основе болезней
    // экзистенциальная меланхолия - проявляется случайно периодически, дефабает некоторое время, требуется плакаться на могиле
    public class MelancholyMonitor : GameStateMachine<MelancholyMonitor, MelancholyMonitor.Instance, IStateMachineTarget, MelancholyMonitor.Def>
    {
        public static Thought MelancholyThought;

        public class Def : BaseDef
        {
            public float timeFromLastMinInterval = 10 * Constants.SECONDS_PER_CYCLE;
            public float timeFromLastMaxInterval = 20 * Constants.SECONDS_PER_CYCLE;
        }

        public new class Instance : GameInstance
        {
            public Instance(IStateMachineTarget master, Def def) : base(master, def)
            {
                // костыль. так как оно не сохраняется в сейф, то генерируем случайно от 0 до 10% между интервалами.
                float time = Mathf.Lerp(def.timeFromLastMinInterval, def.timeFromLastMaxInterval, 0.1f);
                sm.timeFromLastMelancholy.Set(Random.Range(0, time), smi);
            }
        }

#pragma warning disable CS0649

        private State satisfied;
        private State melancholy;
        private FloatParameter timeFromLastMelancholy;

#pragma warning restore CS0649

        public override void InitializeStates(out BaseState default_state)
        {
            serializable = true;
            default_state = satisfied;
            satisfied
                .Update((Instance smi, float dt) => smi.sm.timeFromLastMelancholy.Delta(dt, smi), UpdateRate.SIM_4000ms)
                .EventHandler(GameHashes.SleepFinished, ShouldMelancholy)
                .EventHandler(GameHashes.EffectAdded, delegate (Instance smi)
                    {
                        if (IsMelancholy(smi))
                        {
                            smi.GoTo(melancholy);
                        }
                    });
            melancholy
                .ToggleExpression(Db.Get().Expressions.Unhappy)
                .ToggleThought(MelancholyThought)
                .EventHandler(GameHashes.EffectRemoved, delegate (Instance smi)
                    {
                        if (!IsMelancholy(smi))
                        {
                            smi.GoTo(satisfied);
                        }
                    })
                .Exit(ResetTime);
        }

        private void ShouldMelancholy(Instance smi)
        {
            //TODO: проверки на начало меланхолии
            Effects effects = smi.master.GetComponent<Effects>();
            // меланхолия не нужна если есть траур
            // или отсутствуют могилы с трупами
            if (!effects.HasEffect(DeathPatches.MOURNING) && HasNonEmptyGrave())
            {
                if (!effects.HasEffect(DeathPatches.MELANCHOLY))
                {
                    float time = smi.sm.timeFromLastMelancholy.Get(smi);
                    float chance = Mathf.InverseLerp(smi.def.timeFromLastMinInterval, smi.def.timeFromLastMaxInterval, time);
                    if (Random.value < chance)
                    {
                        // случайный период перед плаканием
                        effects.Add(DeathPatches.MELANCHOLY_TRACKING, true).timeRemaining *= Random.Range(0.3f, 0.6f);
                        effects.Add(DeathPatches.MELANCHOLY, true);
                    }
                }
            }
            else
            {
                ResetTime(smi);
            }
        }
        
        private static bool IsMelancholy(Instance smi)
        {
            return smi.master.GetComponent<Effects>().HasEffect(DeathPatches.MELANCHOLY);
        }
        
        private static void ResetTime(Instance smi)
        {
            smi.sm.timeFromLastMelancholy.Set(0, smi);
        }

        private static bool HasNonEmptyGrave()
        {
            return Components.Graves.Count > 0 && Components.Graves.Items.FindAll(grave => grave.burialTime > 0).Count > 0;
        }
    }
}
