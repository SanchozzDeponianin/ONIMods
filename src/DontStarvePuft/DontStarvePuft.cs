using HarmonyLib;
using UnityEngine;
using PeterHan.PLib.Detours;

namespace DontStarvePuft
{
    internal sealed class DontStarvePuftPatches : KMod.UserMod2
    {
        [HarmonyPatch(typeof(GasAndLiquidConsumerMonitor.Instance), nameof(GasAndLiquidConsumerMonitor.Instance.Consume))]
        private static class GasAndLiquidConsumerMonitor_Instance_Consume
        {
            private static readonly IDetouredField<GasAndLiquidConsumerMonitor.Instance, Element> TargetElement
                = PDetours.DetourField<GasAndLiquidConsumerMonitor.Instance, Element>("targetElement");

            private static void Prefix(GasAndLiquidConsumerMonitor.Instance __instance, ref float dt)
            {
                var targetElement = TargetElement.Get(__instance);
                if (targetElement != null && __instance.def.diet != null)
                {
                    var info = __instance.def.diet.GetDietInfo(targetElement.tag);
                    if (info != null)
                    {
                        var calories = Db.Get().Amounts.Calories.Lookup(__instance.gameObject);
                        if (calories != null)
                        {
                            float factor01 = (1f - CreatureCalorieMonitor.Instance.HUNGRY_RATIO) + calories.value / calories.GetMax();
                            factor01 = Mathf.Pow(factor01, 0.1f);
                            float required_mass = info.ConvertCaloriesToConsumptionMass(-calories.GetDelta() * Constants.SECONDS_PER_CYCLE);
                            required_mass = Mathf.Lerp(required_mass, 0, factor01);
                            required_mass = Mathf.Max(__instance.def.consumptionRate, required_mass);
                            dt *= required_mass / __instance.def.consumptionRate;
                        }
                    }
                }
            }
        }
    }
}
