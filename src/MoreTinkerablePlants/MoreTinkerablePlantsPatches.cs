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
        internal const float THROUGHPUT_BASE_VALUE = 1;
        internal const float THROUGHPUT_MULTIPLIER = 3;

        internal static Attribute ColdBreatherThroughput;
        internal static Attribute OxyfernThroughput;

        private static AttributeModifier ColdBreatherThroughputModifier;
        private static AttributeModifier OxyfernThroughputModifier;

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

        // добавляем атрибуты и модификаторы 
        // для более лючшего отображения в интерфейсе
        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            var db = Db.Get();
            var effectFarmTinker = db.effects.Get(TinkerableEffectMonitor.FARMTINKEREFFECTID);

            ColdBreatherThroughput = new Attribute(nameof(ColdBreatherThroughput), false, Attribute.Display.General, false, THROUGHPUT_BASE_VALUE);
            ColdBreatherThroughput.SetFormatter(new PercentAttributeFormatter());
            db.Attributes.Add(ColdBreatherThroughput);

            ColdBreatherThroughputModifier = new AttributeModifier(ColdBreatherThroughput.Id, THROUGHPUT_MULTIPLIER - THROUGHPUT_BASE_VALUE);
            effectFarmTinker.Add(ColdBreatherThroughputModifier);

            OxyfernThroughput = new Attribute(nameof(OxyfernThroughput), false, Attribute.Display.General, false, THROUGHPUT_BASE_VALUE);
            OxyfernThroughput.SetFormatter(new PercentAttributeFormatter());
            db.Attributes.Add(OxyfernThroughput);

            OxyfernThroughputModifier = new AttributeModifier(OxyfernThroughput.Id, THROUGHPUT_MULTIPLIER - THROUGHPUT_BASE_VALUE);
            effectFarmTinker.Add(OxyfernThroughputModifier);
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            MoreTinkerablePlantsOptions.Reload();
            ColdBreatherThroughputModifier.SetValue(MoreTinkerablePlantsOptions.Instance.ColdBreatherThroughputMultiplier - THROUGHPUT_BASE_VALUE);
            OxyfernThroughputModifier.SetValue(MoreTinkerablePlantsOptions.Instance.OxyfernThroughputMultiplier - THROUGHPUT_BASE_VALUE);
        }

        // Оксихрен
        [HarmonyPatch(typeof(OxyfernConfig), nameof(OxyfernConfig.CreatePrefab))]
        internal static class OxyfernConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                __result.AddOrGet<TinkerableOxyfern>();
            }
        }

        [HarmonyPatch(typeof(Oxyfern), nameof(Oxyfern.SetConsumptionRate))]
        internal static class Oxyfern_SetConsumptionRate
        {
            private static void Postfix(Oxyfern __instance, ElementConsumer ___elementConsumer, ElementConverter ___elementConverter)
            {
                float multiplier = __instance.GetAttributes().Get(OxyfernThroughput).GetTotalValue();
                ___elementConsumer.consumptionRate *= multiplier;
                ___elementConsumer.RefreshConsumptionRate();
                ___elementConverter.SetWorkSpeedMultiplier(multiplier);
            }
        }

        // Холодых
        [HarmonyPatch(typeof(ColdBreatherConfig), nameof(ColdBreatherConfig.CreatePrefab))]
        internal static class ColdBreatherConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                __result.AddOrGet<TinkerableColdBreather>();
            }
        }

        [HarmonyPatch(typeof(ColdBreather), "OnReplanted")]
        internal static class ColdBreather_OnReplanted
        {
            private static void Postfix(ColdBreather __instance)
            {
                __instance.GetComponent<TinkerableColdBreather>()?.ApplyModifier();
            }
        }

        // фикс для элементконсумера, чтобы статуситем не исчезал после обновления
        [HarmonyPatch(typeof(ElementConsumer), "UpdateStatusItem")]
        internal static class ElementConsumer_UpdateStatusItem
        {
            private static void Prefix(ElementConsumer __instance, ref System.Guid ___statusHandle, KSelectable ___selectable)
            {
                if (__instance.showInStatusPanel && ___statusHandle != System.Guid.Empty)
                {
                    ___selectable.RemoveStatusItem(___statusHandle);
                    ___statusHandle = System.Guid.Empty;
                }
            }
        }

        // дерево
        [HarmonyPatch(typeof(ForestTreeConfig), nameof(ForestTreeConfig.CreatePrefab))]
        internal static class ForestTreeConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                __result.AddOrGet<TinkerableForestTree>();
            }
        }

        // в ванилле ветка имеет "тинкерабле" и  "эффекты"
        // но её практически никогда не убобряют
        // с модом тоже
        // однако крайне редко удается случайно отловить когда убобряют
        // как то связано с засыханием и ростом новых веток
        // поэтому - грязный хак чтобы не могли убобрить
        [HarmonyPatch(typeof(ForestTreeBranchConfig), "CreatePrefab")]
        internal static class ForestTreeBranchConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                __result.GetComponent<Tinkerable>().tinkerMaterialTag = GameTags.Void;
            }
        }

        // применить эффект при росте новой ветки дерева
        [HarmonyPatch(typeof(TreeBud), "OnSpawn")]
        internal static class TreeBud_OnSpawn
        {
            private static void Prefix(ref TreeBud __instance, Ref<BuddingTrunk> ___buddingTrunk)
            {
                TinkerableForestTree.ApplyModifierToBranch(__instance, ___buddingTrunk.Get());
            }
        }
    }
}
