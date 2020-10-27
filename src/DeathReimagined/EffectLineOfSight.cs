using Klei.AI;
using TUNING;

namespace DeathReimagined
{
    // применение эффекта к живым дупликам в области видимости вокруг объекта. 
    // радиус действия равен радиусу декора
    // неоптимизировано, но для трупов сойдёт. 
    // todo: или всетаки переписать по подобию сенсора дупликов.
    public class EffectLineOfSight : GameStateMachine<EffectLineOfSight, EffectLineOfSight.Instance>
    {
        public class Def : BaseDef
        {
            public string effectName;
        }

        public new class Instance : GameInstance
        {
            private Effect effect;
            private DecorProvider decorProvider;

            public Instance(IStateMachineTarget master, Def def) : base(master, def)
            {
                effect = string.IsNullOrEmpty(def.effectName) ? null : Db.Get().effects.Get(def.effectName);
                decorProvider = master.gameObject.GetComponent<DecorProvider>();
            }

            public void ApplyEffect()
            {
                int cell1 = Grid.PosToCell(this);
                float radius = decorProvider?.decorRadius?.GetTotalValue() ?? BUILDINGS.DECOR.NONE.radius;

                if (effect != null && Grid.IsValidCell(cell1))
                {
                    foreach (MinionIdentity minionIdentity in Components.LiveMinionIdentities)
                    {
                        int cell2 = Grid.PosToCell(minionIdentity);
                        if (Grid.IsValidCell(cell2) && Grid.GetCellRange(cell1, cell2) <= radius && Grid.VisibilityTest(cell1, cell2))
                        {
                            minionIdentity.GetComponent<Effects>().Add(effect, true);
                        }
                    }
                }
            }
        }

#pragma warning disable CS0649
        private State idle;
#pragma warning restore CS0649

        public override void InitializeStates(out BaseState default_state)
        {
            serializable = false;
            default_state = idle;
            idle.Update((Instance smi, float dt) => smi.ApplyEffect(), UpdateRate.SIM_200ms);
        }
    }
}
