using PeterHan.PLib.Lighting;

namespace SanchozzONIMods.Shared
{
    public class CustomLighting
    {
        public static ILightShape CircleOnCellBelow;

        public static void Register()
        {
            CircleOnCellBelow = new PLightManager().Register("Sanchozz.CustomLighting.CircleOnCellBelow", LightCircleOnCellBelow, LightShape.Circle);
        }

		private static void LightCircleOnCellBelow(LightingArgs arg)
		{
			int cell = Grid.CellBelow(arg.SourceCell);
			int range = arg.Range;
            var octant = new OctantBuilder(arg.Brightness, cell)
            {
                Falloff = 0.5f,
                SmoothLight = true
            };
            octant.AddOctant(range, DiscreteShadowCaster.Octant.E_NE);
			octant.AddOctant(range, DiscreteShadowCaster.Octant.E_SE);
			octant.AddOctant(range, DiscreteShadowCaster.Octant.N_NE);
			octant.AddOctant(range, DiscreteShadowCaster.Octant.N_NW);
			octant.AddOctant(range, DiscreteShadowCaster.Octant.S_SE);
			octant.AddOctant(range, DiscreteShadowCaster.Octant.S_SW);
			octant.AddOctant(range, DiscreteShadowCaster.Octant.W_NW);
			octant.AddOctant(range, DiscreteShadowCaster.Octant.W_SW);
		}
	}
}
