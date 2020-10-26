namespace DeathReimagined
{
    // дебаф за наличие на карте незакопанных трупов
    public class UnburiedCorpseMonitor : GameStateMachine<UnburiedCorpseMonitor, UnburiedCorpseMonitor.Instance>
    {
        public new class Instance : GameInstance
        {
            public Instance(IStateMachineTarget master) : base(master)
            {
            }
        }

#pragma warning disable CS0649

        private State satisfied;
        private State unburiedcorpse;

#pragma warning restore CS0649

        public override void InitializeStates(out BaseState default_state)
        {
            serializable = true;
            default_state = satisfied;
            satisfied
                .Transition(unburiedcorpse, IsCorpseUnburied, UpdateRate.SIM_200ms);
            unburiedcorpse
                .Transition(satisfied, Not(IsCorpseUnburied), UpdateRate.SIM_200ms)
                .ToggleEffect(DeathPatches.UNBURIED_CORPSE)
                .ToggleExpression(Db.Get().Expressions.Unhappy)
                .ToggleThought(MelancholyMonitor.MelancholyThought);
        }

        private static bool IsCorpseUnburied(Instance smi)
        {
            return Components.LiveMinionIdentities.Count != Components.MinionIdentities.Count;
        }
    }
}
