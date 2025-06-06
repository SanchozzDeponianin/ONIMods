using System.Collections.Generic;
using Klei.AI;

namespace ExoticSpices
{
    using static ModAssets;

    public class DupeEffectFlatulence : GameStateMachine<DupeEffectFlatulence, DupeEffectFlatulence.Instance>
    {
        public new class Instance : GameInstance
        {
            private Effects effects;
            private Flatulence flatulence;
            private Traits traits;

            public Instance(IStateMachineTarget master) : base(master)
            {
                effects = master.GetComponent<Effects>();
                flatulence = master.gameObject.AddOrGet<Flatulence>();
                traits = master.GetComponent<Traits>();
            }

            public bool ShouldFlatulence() => effects.HasEffect(GASSY_MOO_SPICE);

            public void SwitchFlatulence(bool on)
            {
                if (!traits.HasTrait(FLATULENCE))
                    flatulence.enabled = on;
            }

            public void ApplyImmunities()
            {
                foreach (var effect in sm.immunities)
                    effects.AddImmunity(effect, GASSY_MOO_SPICE, false);
            }

            public void RemoveImmunities()
            {
                foreach (var effect in sm.immunities)
                    effects.RemoveImmunity(effect, GASSY_MOO_SPICE);
            }
        }

#pragma warning disable CS0649
        private State flatulence_off;
        private State flatulence_on;
#pragma warning restore CS0649

        private List<Effect> immunities = new();

        public override void InitializeStates(out BaseState default_state)
        {
            immunities.Add(Db.Get().effects.Get("MinorIrritation"));
            immunities.Add(Db.Get().effects.Get("MajorIrritation"));
            default_state = flatulence_off;
            root
                .EnterTransition(flatulence_on, smi => smi.ShouldFlatulence());
            flatulence_off
                .Enter(smi => smi.SwitchFlatulence(false))
                .EventTransition(GameHashes.EffectAdded, flatulence_on, smi => smi.ShouldFlatulence());
            flatulence_on
                .Enter(smi => smi.SwitchFlatulence(true))
                .Enter(smi => smi.ApplyImmunities())
                .Exit(smi => smi.RemoveImmunities())
                .EventTransition(GameHashes.EffectRemoved, flatulence_off, smi => !smi.ShouldFlatulence())
                .EventHandler(GameHashes.EatCompleteEater, smi => CreateEmoteChore(smi.master, ButtScratchEmote, 1f))
                .EventHandler(GameHashes.SleepFinished, smi => CreateEmoteChore(smi.master, ButtScratchEmote, 0.35f));
        }
    }
}
