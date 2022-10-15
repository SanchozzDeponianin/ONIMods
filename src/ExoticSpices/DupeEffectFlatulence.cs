using Klei.AI;

namespace ExoticSpices
{
    using static ExoticSpicesAssets;

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

            public bool ShouldFlatulence() => effects.HasEffect(GassyMooSpice.Id);

            public void SwitchFlatulence(bool on)
            {
                if (!traits.HasTrait(FLATULENCE))
                    flatulence.enabled = on;
            }
        }

#pragma warning disable CS0649
        State flatulence_off;
        State flatulence_on;
#pragma warning restore CS0649

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = flatulence_off;
            root
                .EnterTransition(flatulence_on, smi => smi.ShouldFlatulence());
            flatulence_off
                .Enter(smi => smi.SwitchFlatulence(false))
                .EventTransition(GameHashes.EffectAdded, flatulence_on, smi => smi.ShouldFlatulence());
            flatulence_on
                .Enter(smi => smi.SwitchFlatulence(true))
                .EventTransition(GameHashes.EffectRemoved, flatulence_off, smi => !smi.ShouldFlatulence())
                .EventHandler(GameHashes.EatCompleteEater, smi => CreateEmoteChore(smi.master, ButtScratchEmote, 1f))
                .EventHandler(GameHashes.SleepFinished, smi => CreateEmoteChore(smi.master, ButtScratchEmote, 0.35f));
        }
    }
}
