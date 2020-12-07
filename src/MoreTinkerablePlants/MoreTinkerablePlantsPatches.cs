using Harmony;
using Klei.AI;
using UnityEngine;

using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace MoreTinkerablePlants
{
    internal static class MoreTinkerablePlantsPatches
    {
        public static void OnLoad()
        {
            PUtil.InitLibrary();
            PUtil.RegisterPatchClass(typeof(MoreTinkerablePlantsPatches));
            POptions.RegisterOptions(typeof(MoreTinkerablePlantsOptions));
        }

        [PLibMethod(RunAt.AfterModsLoad)]
        private static void InitLocalization()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            MoreTinkerablePlantsOptions.Reload();
            TinkerableColdBreather.ThroughputMultiplier = MoreTinkerablePlantsOptions.Instance.ColdBreatherThroughputMultiplier;
            TinkerableOxyfern.ThroughputMultiplier = MoreTinkerablePlantsOptions.Instance.OxyfernThroughputMultiplier;
        }

        // Оксиферн
        [HarmonyPatch(typeof(OxyfernConfig), "CreatePrefab")]
        internal static class OxyfernConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                __result.AddOrGet<TinkerableOxyfern>();
            }
        }

        [HarmonyPatch(typeof(OxyfernConfig), "OnSpawn")]
        internal static class OxyfernConfig_OnSpawn
        {
            private static void Postfix(GameObject inst)
            {
                inst.GetComponent<TinkerableOxyfern>()?.ApplyEffect();
            }
        }

        // холодых
        [HarmonyPatch(typeof(ColdBreatherConfig), "CreatePrefab")]
        internal static class ColdBreatherConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                __result.AddOrGet<TinkerableColdBreather>();
                __result.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject inst)
                {
                    inst.GetComponent<TinkerableColdBreather>()?.ApplyEffect();
                };
            }
        }

        [HarmonyPatch(typeof(ColdBreather), "OnReplanted")]
        internal static class ColdBreather_OnReplanted
        {
            private static void Postfix(ColdBreather __instance)
            {
                __instance.GetComponent<TinkerableColdBreather>()?.ApplyEffect();
            }
        }

        // дерево
        [HarmonyPatch(typeof(ForestTreeConfig), "CreatePrefab")]
        internal static class ForestTreeConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                __result.AddOrGet<TinkerableForestTree>();
            }
        }

        /*
        [HarmonyPatch(typeof(ForestTreeBranchConfig), "CreatePrefab")]
        internal static class ForestTreeBranchConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                // в ванилле ветка обозначена "тинкерабле", и имеет "эффекты"
                // но её никогда не удобряют
                // чё за херня ??
                // если добавить "эффекты", то начинают удобрять каждую ветку отдельно
                //  убрать "тинкерабле" ????

                //__result.AddOrGet<Effects>();
                //Object.DestroyImmediate(__result.GetComponent<Tinkerable>());
            }
        }   */

        // применить эффект при росте новой ветки дерева
        [HarmonyPatch(typeof(TreeBud), "OnSpawn")]
        internal static class TreeBud_OnSpawn
        {
            private static void Prefix(ref TreeBud __instance, Ref<BuddingTrunk> ___buddingTrunk)
            {
                var parentEffects = ___buddingTrunk?.Get()?.GetComponent<Effects>();
                var effects = __instance.GetComponent<Effects>();
                if (parentEffects != null && effects != null && parentEffects.HasEffect(TinkerableEffectMonitor.FARMTINKEREFFECTID))
                {
                    effects.Add(TinkerableEffectMonitor.FARMTINKEREFFECTID, false).timeRemaining = parentEffects.Get(TinkerableEffectMonitor.FARMTINKEREFFECTID).timeRemaining;
                }
            }
        }
    }
}
