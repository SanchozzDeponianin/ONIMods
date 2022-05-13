using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace CrabsProfit
{
    internal sealed class CrabsProfitPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(CrabsProfitPatches));
            new POptions().RegisterOptions(this, typeof(CrabsProfitOptions));
            CrabsProfitOptions.Reload();
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        private static void AddDrop(GameObject prefab, string drop_id, int count)
        {
            if (count > 0)
            {
                var butcherable = prefab.AddOrGet<Butcherable>();
                var new_drop = new string[count];
                for (int i = 0; i < count; i++)
                    new_drop[i] = drop_id;
                butcherable.SetDrops(butcherable.drops.AddRangeToArray(new_drop));
            }
        }

        // мясо:
        [HarmonyPatch(typeof(CrabConfig), nameof(CrabConfig.CreateCrab))]
        private static class CrabConfig_CreateCrab
        {
            private static void Postfix(GameObject __result)
            {
                AddDrop(__result, ShellfishMeatConfig.ID, CrabsProfitOptions.Instance.Crab_Meat);
            }
        }

        [HarmonyPatch(typeof(CrabWoodConfig), nameof(CrabWoodConfig.CreateCrabWood))]
        private static class CrabWoodConfig_CreateCrabWood
        {
            private static void Postfix(GameObject __result)
            {
                AddDrop(__result, ShellfishMeatConfig.ID, CrabsProfitOptions.Instance.CrabWood_Meat);
            }
        }

        // шкорлупа:
        [HarmonyPatch(typeof(CrabFreshWaterConfig), nameof(CrabFreshWaterConfig.CreatePrefab))]
        private static class CrabFreshWaterConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                AddDrop(__result, CrabFreshWaterShellConfig.ID, 1);
            }
        }

        [HarmonyPatch(typeof(BabyCrabFreshWaterConfig), nameof(BabyCrabFreshWaterConfig.CreatePrefab))]
        private static class BabyCrabFreshWaterConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                AddDrop(__result, BabyCrabFreshWaterShellConfig.ID, 1);
                __result.AddOrGetDef<BabyMonitor.Def>().onGrowDropID = BabyCrabFreshWaterShellConfig.ID;
            }
        }
    }
}
