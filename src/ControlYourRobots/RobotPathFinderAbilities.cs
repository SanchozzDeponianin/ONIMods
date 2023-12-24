namespace ControlYourRobots
{
    public class RobotPathFinderAbilities : CreaturePathFinderAbilities
    {
        private const int proxyID = Grid.Restriction.DefaultID;
        private CellOffset[][] transitionVoidOffsets;

        public RobotPathFinderAbilities(Navigator navigator) : base(navigator) { }

        protected override void Refresh(Navigator navigator)
        {
            if (transitionVoidOffsets == null)
            {
                transitionVoidOffsets = new CellOffset[navigator.NavGrid.transitions.Length][];
                for (int i = 0; i < transitionVoidOffsets.Length; i++)
                {
                    transitionVoidOffsets[i] = navigator.NavGrid.transitions[i].voidOffsets;
                }
            }
            base.Refresh(navigator);
        }

        private static bool IsAccessPermitted(int proxyID, int cell, int from_cell, NavType from_nav_type)
        {
            return Grid.HasPermission(cell, proxyID, from_cell, from_nav_type);
        }

        public override bool TraversePath(ref PathFinder.PotentialPath path, int from_cell, NavType from_nav_type, int cost, int transition_id, bool submerged)
        {
            if (!IsAccessPermitted(proxyID, path.cell, from_cell, from_nav_type))
                return false;
            if (transitionVoidOffsets != null)
            {
                foreach (var offset in transitionVoidOffsets[transition_id])
                {
                    int cell = Grid.OffsetCell(from_cell, offset);
                    if (!IsAccessPermitted(proxyID, cell, from_cell, from_nav_type))
                        return false;
                }
            }
            return base.TraversePath(ref path, from_cell, from_nav_type, cost, transition_id, submerged);
        }
    }
}
