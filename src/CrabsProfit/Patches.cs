using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace CrabsProfit
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            ModOptions.Reload();
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        private static void AddDrop(GameObject prefab, string drop_id, float count)
        {
            if (count > 0 && !string.IsNullOrEmpty(drop_id))
            {
                var butcherable = prefab.AddOrGet<Butcherable>();
                var drops = butcherable.drops ?? new Dictionary<string, float>();
                if (drops.ContainsKey(drop_id))
                    drops[drop_id] += count;
                else
                    drops[drop_id] = count;
                butcherable.SetDrops(drops);
            }
        }

        private static void FixDrop(GameObject prefab)
        {
            // drops не сериализируется, а ещё и перезаписывается в EntityTemplates.DeathDropFunction
            // перезапишем поверх
            prefab.GetComponent<KPrefabID>().prefabSpawnFn += inst =>
            {
                if (inst.TryGetComponent(out Butcherable inst_b) && prefab.TryGetComponent(out Butcherable prefab_b))
                {
                    Dictionary<string, float> drops = prefab_b.drops == null ? new() : new(prefab_b.drops);
                    inst_b.SetDrops(drops);
                }
            };
        }

        // мясо:
        [HarmonyPatch(typeof(CrabConfig), nameof(CrabConfig.CreateCrab))]
        private static class CrabConfig_CreateCrab
        {
            private static bool Prepare() => ModOptions.Instance.Crab_Meat > 0;
            private static void Postfix(GameObject __result)
            {
                AddDrop(__result, ShellfishMeatConfig.ID, ModOptions.Instance.Crab_Meat);
                FixDrop(__result);
            }
        }

        [HarmonyPatch(typeof(CrabWoodConfig), nameof(CrabWoodConfig.CreateCrabWood))]
        private static class CrabWoodConfig_CreateCrabWood
        {
            private static bool Prepare() => ModOptions.Instance.CrabWood_Meat > 0;
            private static void Postfix(GameObject __result)
            {
                AddDrop(__result, ShellfishMeatConfig.ID, ModOptions.Instance.CrabWood_Meat);
                FixDrop(__result);
            }
        }

        // шкорлупа:
        [HarmonyPatch(typeof(CrabFreshWaterConfig), nameof(CrabFreshWaterConfig.CreatePrefab))]
        private static class CrabFreshWaterConfig_CreatePrefab
        {
            private static bool Prepare() => ModOptions.Instance.CrabFreshWater_Shell_Mass > 0;
            private static void Postfix(GameObject __result)
            {
                AddDrop(__result, CrabFreshWaterShellConfig.ID, 1);
                FixDrop(__result);
            }
        }

        [HarmonyPatch(typeof(BabyCrabFreshWaterConfig), nameof(BabyCrabFreshWaterConfig.CreatePrefab))]
        private static class BabyCrabFreshWaterConfig_CreatePrefab
        {
            private static bool Prepare() => ModOptions.Instance.CrabFreshWater_Shell_Mass > 0;
            private static void Postfix(GameObject __result)
            {
                AddDrop(__result, CrabFreshWaterShellConfig.ID, ModOptions.Instance.BabyShellUnits);
                var def = __result.AddOrGetDef<BabyMonitor.Def>();
                def.onGrowDropID = CrabFreshWaterShellConfig.ID;
                def.onGrowDropUnits = ModOptions.Instance.BabyShellUnits;
                FixDrop(__result);
            }
        }
    }
}
