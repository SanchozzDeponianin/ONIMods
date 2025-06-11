using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Klei.AI;
using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;

namespace BetterPlantTending
{
    using static ModAssets;

    internal static class PlantsPatches
    {
        #region Oxyfern
        // Оксихрен
        [HarmonyPatch(typeof(OxyfernConfig), nameof(OxyfernConfig.CreatePrefab))]
        private static class OxyfernConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                __result.AddOrGet<TendedOxyfern>();
                if (ModOptions.Instance.oxyfern_fix_output_cell)
                    __result.GetComponent<ElementConverter>().outputElements[0].outputElementOffset.y += 0.5f;
            }
        }

        [HarmonyPatch(typeof(Oxyfern), nameof(Oxyfern.SetConsumptionRate))]
        private static class Oxyfern_SetConsumptionRate
        {
            private static void Postfix(Oxyfern __instance, ElementConsumer ___elementConsumer, ElementConverter ___elementConverter)
            {
                // дикость уже учитывается внутри Oxyfern.SetConsumptionRate
                float multiplier = __instance.GetAttributes().Get(fakeGrowingRate.AttributeId).GetTotalValue() / CROPS.GROWTH_RATE;
                ___elementConsumer.consumptionRate *= multiplier;
                ___elementConsumer.RefreshConsumptionRate();
                ___elementConverter.SetWorkSpeedMultiplier(multiplier);
            }
        }
        #endregion
        #region ColdBreather
        // Холодых
        [HarmonyPatch(typeof(ColdBreatherConfig), nameof(ColdBreatherConfig.CreatePrefab))]
        private static class ColdBreatherConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                __result.AddOrGet<TendedColdBreather>().emitRads = __result.GetComponent<RadiationEmitter>()?.emitRads ?? 0f;
            }
        }

        [HarmonyPatch(typeof(ColdBreather), nameof(ColdBreather.OnReplanted))]
        private static class ColdBreather_OnReplanted
        {
            private static void Postfix(ColdBreather __instance)
            {
                __instance.GetComponent<TendedColdBreather>().ApplyModifier();
            }
        }
        #endregion
        #region SaltPlant
        // солёная лоза, ну и заодно и неиспользуемый кактус, они оба на одной основе сделаны
        [HarmonyPatch]
        private static class SaltPlantConfig_CreatePrefab
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                if (ModOptions.Instance.saltplant_adjust_gas_consumption)
                    yield return typeof(SaltPlantConfig).GetMethodSafe(nameof(SaltPlantConfig.CreatePrefab), false);
                if (DlcManager.IsExpansion1Active() && ModOptions.Instance.hydrocactus_adjust_gas_consumption)
                    yield return typeof(FilterPlantConfig).GetMethodSafe(nameof(FilterPlantConfig.CreatePrefab), false);
            }

            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<TendedSaltPlant>().consumptionRate = __result.GetComponent<ElementConsumer>().consumptionRate;
            }
        }
        #endregion
        #region BlueGrass & Dinofern
        // синяя алоэ и диночтототам, оба два похожи как Ctrl-C Ctrl-V
        [HarmonyPatch(typeof(BlueGrassConfig), nameof(BlueGrassConfig.CreatePrefab))]
        private static class BlueGrassConfig_CreatePrefab
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC2_ID)
                && ModOptions.Instance.blue_grass_adjust_gas_consumption;

            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<TendedBlueGrass>();
            }
        }

        [HarmonyPatch(typeof(DinofernConfig), nameof(DinofernConfig.CreatePrefab))]
        private static class DinofernConfig_CreatePrefab
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC4_ID)
                && ModOptions.Instance.dinofern_adjust_gas_consumption;

            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<TendedDinofern>();
            }
        }

        [HarmonyPatch]
        private static class BlueGrass_Dinofern_SetConsumptionRate
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                if (DlcManager.IsContentSubscribed(DlcManager.DLC2_ID)
                    && ModOptions.Instance.blue_grass_adjust_gas_consumption)
                    yield return typeof(BlueGrass).GetMethodSafe(nameof(BlueGrass.SetConsumptionRate), false);
                if (DlcManager.IsContentSubscribed(DlcManager.DLC4_ID)
                    && ModOptions.Instance.dinofern_adjust_gas_consumption)
                    yield return typeof(Dinofern).GetMethodSafe(nameof(Dinofern.SetConsumptionRate), false);
            }

            private static void Postfix(KMonoBehaviour __instance, ElementConsumer ___elementConsumer, ReceptacleMonitor ___receptacleMonitor)
            {
                // тут дикость учитывается дважды: внутри Growing ххх.SetConsumptionRate
                float base_growth_rate = ___receptacleMonitor.Replanted ? CROPS.GROWTH_RATE : CROPS.WILD_GROWTH_RATE;
                float multiplier = __instance.GetAttributes().Get(fakeGrowingRate.AttributeId).GetTotalValue() / base_growth_rate;
                ___elementConsumer.consumptionRate *= multiplier;
                ___elementConsumer.RefreshConsumptionRate();
            }
        }

        // возможность дафать семена
        [HarmonyPatch(typeof(Dinofern), nameof(Dinofern.OnSpawn))]
        private static class Dinofern_OnSpawn
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC4_ID)
                && ModOptions.Instance.dinofern_can_give_seeds;

            private static void Postfix(Dinofern __instance)
            {
                __instance.GetComponent<SeedProducer>().seedInfo.productionType = SeedProducer.ProductionType.Harvest;
            }
        }
        #endregion
        #region CritterTrapPlant
        // растение-ловушка:
        // производство газа  пропорционально её скорости роста
        [HarmonyPatch(typeof(CritterTrapPlant.StatesInstance), nameof(CritterTrapPlant.StatesInstance.AddGas))]
        private static class CritterTrapPlant_StatesInstance_AddGas
        {
            private static float base_growth_rate;
            private static bool Prepare()
            {
                base_growth_rate = ModOptions.Instance.critter_trap_decrease_gas_production_by_wildness
                    ? CROPS.GROWTH_RATE : CROPS.WILD_GROWTH_RATE;
                return DlcManager.IsExpansion1Active()
                    && ModOptions.Instance.critter_trap_adjust_gas_production;
            }

            private static void Prefix(CritterTrapPlant.StatesInstance __instance, ref float dt)
            {
                var growth_rate = __instance.master.GetAttributes().Get(Db.Get().Amounts.Maturity.deltaAttribute.Id).GetTotalValue();
                dt *= growth_rate / base_growth_rate;
            }
        }

        // возможность дафать семена
        [HarmonyPatch(typeof(CritterTrapPlant), nameof(CritterTrapPlant.OnSpawn))]
        private static class CritterTrapPlant_OnSpawn
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active()
                && ModOptions.Instance.critter_trap_can_give_seeds;

            private static void Postfix(CritterTrapPlant __instance)
            {
                __instance.GetComponent<SeedProducer>().seedInfo.productionType = SeedProducer.ProductionType.Harvest;
            }
        }
        #endregion
        #region SapTree
        // резиногое дерего:
        // делаем дерего убобряемым
        [HarmonyPatch(typeof(SapTreeConfig), nameof(SapTreeConfig.CreatePrefab))]
        private static class SapTreeConfig_CreatePrefab
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active()
                && ModOptions.Instance.allow_tinker_saptree;

            private static void Postfix(GameObject __result)
            {
                Tinkerable.MakeFarmTinkerable(__result);
                var prefabID = __result.GetComponent<KPrefabID>();
                prefabID.AddTag(GameTags.Plant);
                prefabID.prefabInitFn += go =>
                {
                    var attributes = go.GetAttributes();
                    attributes.Add(Db.Get().Amounts.Maturity.deltaAttribute);
                    attributes.Add(fakeGrowingRate);
                };
                prefabID.prefabSpawnFn += go =>
                {
                    // широкое на широкое, иначе дупли получают по  морде и занимаются хернёй
                    if (go.TryGetComponent(out Tinkerable tinkerable))
                    {
                        tinkerable.SetOffsetTable(OffsetGroups.InvertedWideTable);
                        go.GetComponent<Storage>().SetOffsetTable(OffsetGroups.InvertedWideTable);
                    }
                };
            }
        }

        // ускоряем поедание жранины и выделение резины
        [HarmonyPatch]
        private static class SapTree_StatesInstance_EatFoodItem_Ooze
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active()
                && ModOptions.Instance.allow_tinker_saptree;

            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new List<MethodBase>()
                {
                    typeof(SapTree.StatesInstance).GetMethodSafe(nameof(SapTree.StatesInstance.EatFoodItem), false, PPatchTools.AnyArguments),
                    typeof(SapTree.StatesInstance).GetMethodSafe(nameof(SapTree.StatesInstance.Ooze), false, PPatchTools.AnyArguments),
                };
            }

            private static void Prefix(SapTree.StatesInstance __instance, ref float dt)
            {
                dt *= __instance.gameObject.GetAttributes().Get(fakeGrowingRate.AttributeId).GetTotalValue() / CROPS.GROWTH_RATE;
            }
        }

        // ускоряем конверсию жранины в резины
        [HarmonyPatch(typeof(SapTree.StatesInstance), nameof(SapTree.StatesInstance.EatFoodItem))]
        private static class SapTree_StatesInstance_EatFoodItem
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active()
                && ModOptions.Instance.allow_tinker_saptree;

            /*
            тут в норме dt = 1 поэтому можно просто умножить. если клеи поменяют, то придется городить чтото посложнее
            --- float mass = pickupable.GetComponent<Edible>().Calories * 0.001f * base.def.kcalorieToKGConversionRatio;
            +++ float mass = pickupable.GetComponent<Edible>().Calories * 0.001f * base.def.kcalorieToKGConversionRatio * dt;
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var Ratio = typeof(SapTree.Def).GetFieldSafe(nameof(SapTree.Def.kcalorieToKGConversionRatio), false);
                var dt = typeof(SapTree.StatesInstance).GetMethodSafe(nameof(SapTree.StatesInstance.EatFoodItem), false, typeof(float))?.GetParameters()?.First(p => p.ParameterType == typeof(float) && p.Name == "dt");
                if (Ratio != null && dt != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].LoadsField(Ratio))
                        {
                            instructions.Insert(++i, TranspilerUtils.GetLoadArgInstruction(dt));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Mul));
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        #endregion
    }
}
