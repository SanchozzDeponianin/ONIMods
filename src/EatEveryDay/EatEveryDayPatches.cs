using HarmonyLib;
using SanchozzONIMods.Lib;

namespace EatEveryDay
{
	// обновленная версия мода "Eat every day Mod" от ricvail
	// https://steamcommunity.com/sharedfiles/filedetails/?id=2107702766 
	public sealed class EatEveryDayPatches : KMod.UserMod2
	{
        public override void OnLoad(Harmony harmony)
        {
			Utils.LogModVersion();
            base.OnLoad(harmony);
        }

        [HarmonyPatch(typeof(CalorieMonitor.Instance), nameof(CalorieMonitor.Instance.IsHungry))]
		public static class CalorieMonitor_Instance_IsHungry
		{
			public static void Postfix(CalorieMonitor.Instance __instance, ref bool __result)
			{
				if (__instance.IsSatisfied())
					return;
				float delta = __instance.calories.GetDelta();
				if (delta >= 0f)
					return;
				float consumed_per_cycle = delta * Constants.SECONDS_PER_CYCLE;
				float thresold = __instance.calories.GetMax() + consumed_per_cycle * 0.75f;
				__result = __instance.calories.value < thresold;
			}
		}
	}
}
