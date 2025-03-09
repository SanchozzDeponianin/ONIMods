using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Klei.AI;
using STRINGS;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;

namespace BetterPlantTending
{
    using AttributeInstanceParameter = StateMachinesExtensions.NonSerializedObjectParameter
        <SpaceTreePlant, SpaceTreePlant.Instance, IStateMachineTarget, SpaceTreePlant.Def, AttributeInstance>;

    internal static class TreesPatches
    {
        // деревянное дерево
        // разблокируем возможность мутации
        [HarmonyPatch(typeof(ForestTreeSeedMonitor), nameof(ForestTreeSeedMonitor.ExtractExtraSeed))]
        private static class ForestTreeSeedMonitor_ExtractExtraSeed
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            private static GameObject AddMutation(GameObject seed, ForestTreeSeedMonitor trunk)
            {
                if (BetterPlantTendingOptions.Instance.tree_unlock_mutation
                    && seed.TryGetComponent(out MutantPlant seed_mutant)
                    && trunk.TryGetComponent(out SeedProducer producer) && producer.RollForMutation())
                {
                    seed_mutant.Mutate();
                }
                return seed;
            }
            /*
            ...
                Util.KInstantiate(Assets.GetPrefab("ForestTreeSeed"), position)
            +++     .AddMutation(this)
                    .SetActive(true);
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Transpile(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var kInstantiate = typeof(Util).GetMethodSafe(nameof(Util.KInstantiate), true, typeof(GameObject), typeof(Vector3));
                var addMutation = typeof(ForestTreeSeedMonitor_ExtractExtraSeed)
                    .GetMethodSafe(nameof(AddMutation), true, typeof(GameObject), typeof(ForestTreeSeedMonitor));
                if (kInstantiate != null && addMutation != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(kInstantiate))
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, addMutation));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // мутантовое дерево не даёт семена
        [HarmonyPatch(typeof(ForestTreeSeedMonitor), nameof(ForestTreeSeedMonitor.TryRollNewSeed))]
        private static class ForestTreeSeedMonitor_TryRollNewSeed
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            private static bool Prefix(ForestTreeSeedMonitor __instance)
            {
                return !(__instance.TryGetComponent(out MutantPlant trunk_mutant) && !trunk_mutant.IsOriginal);
            }
        }

        // сиропное дерево
        // разблокируем возможность мутации
        // проверяем и запоминаем когда тюлень поел
        private static CreaturePoopLoot.BoolParameter should_mutate;
        private static CreaturePoopLoot.BoolParameter no_drop_seed;

        [HarmonyPatch(typeof(CreaturePoopLoot), nameof(CreaturePoopLoot.InitializeStates))]
        private static class CreaturePoopLoot_InitializeStates
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            private static void Postfix(CreaturePoopLoot __instance)
            {
                should_mutate = __instance.AddParameter(nameof(should_mutate), new CreaturePoopLoot.BoolParameter());
                no_drop_seed = __instance.AddParameter(nameof(no_drop_seed), new CreaturePoopLoot.BoolParameter());
                __instance.root.EventHandler(GameHashes.EatSolidComplete, OnEatSolidComplete);
            }

            private static void OnEatSolidComplete(CreaturePoopLoot.Instance smi, object data)
            {
                var plant = data as KPrefabID;
                if (BetterPlantTendingOptions.Instance.space_tree_unlock_mutation
                    && plant != null && plant.TryGetComponent(out MutantPlant mutant) && plant.TryGetComponent(out SeedProducer producer))
                {
                    if (mutant.IsOriginal)
                    {
                        if (producer.RollForMutation())
                            should_mutate.Set(true, smi);
                    }
                    else
                        no_drop_seed.Set(true, smi);
                }
            }
        }

        // применяем мутацию
        [HarmonyPatch(typeof(CreaturePoopLoot), nameof(CreaturePoopLoot.RollForLoot))]
        private static class CreaturePoopLoot_RollForLoot
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            // мутантовое дерево не даёт семена
            // TODO: пока так сойдёт, если в CreaturePoopLoot добавят чтото кроме семян, надо будет менять алгоритм
            private static bool Prefix(CreaturePoopLoot.Instance smi)
            {
                return !no_drop_seed.Get(smi);
            }

            private static void Postfix(CreaturePoopLoot.Instance smi)
            {
                should_mutate.Set(false, smi);
                no_drop_seed.Set(false, smi);
            }

            private static GameObject AddMutation(GameObject seed, CreaturePoopLoot.Instance smi)
            {
                if (BetterPlantTendingOptions.Instance.space_tree_unlock_mutation
                    && should_mutate.Get(smi) && seed.TryGetComponent(out MutantPlant seed_mutant))
                {
                    seed_mutant.Mutate();
                }
                return seed;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Transpile(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var kInstantiate = typeof(Util).GetMethodSafe(nameof(Util.KInstantiate), true, typeof(GameObject), typeof(Vector3));
                var addMutation = typeof(CreaturePoopLoot_RollForLoot)
                    .GetMethodSafe(nameof(AddMutation), true, typeof(GameObject), typeof(CreaturePoopLoot.Instance));
                if (kInstantiate != null && addMutation != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(kInstantiate))
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, addMutation));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // ускоряем производство сиропа пропорционально мутациям
        // добавляем недостающий атрибут
        [HarmonyPatch(typeof(SpaceTreeConfig), nameof(SpaceTreeConfig.CreatePrefab))]
        private static class SpaceTreeConfig_CreatePrefab
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            private static void Postfix(GameObject __result)
            {
                var modifiers = __result.GetComponent<Modifiers>();
                modifiers.initialAttributes.Add(Db.Get().PlantAttributes.YieldAmount.Id);
                var trait = Db.Get().traits.Get(modifiers.initialTraits[0]);
                trait.Add(new AttributeModifier(Db.Get().PlantAttributes.YieldAmount.Id, SpaceTreeConfig.SUGAR_WATER_CAPACITY,
                    trait.description, false, false, true));
            }
        }

        // чтобы не дёргать GetComponent каждый тик
        private static AttributeInstanceParameter max_maturity;
        private static AttributeInstanceParameter yield_amount;

        [HarmonyPatch(typeof(SpaceTreePlant), nameof(SpaceTreePlant.InitializeStates))]
        private static class SpaceTreePlant_InitializeStates_Mutant
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            private static void Postfix(SpaceTreePlant __instance)
            {
                max_maturity = __instance.AddParameter(nameof(max_maturity), new AttributeInstanceParameter());
                yield_amount = __instance.AddParameter(nameof(yield_amount), new AttributeInstanceParameter());
                __instance.root.Enter(InitAttributes);
            }

            private static void InitAttributes(SpaceTreePlant.Instance smi)
            {
                max_maturity.Set(Db.Get().Amounts.Maturity.maxAttribute.Lookup(smi.gameObject), smi);
                yield_amount.Set(Db.Get().PlantAttributes.YieldAmount.Lookup(smi.gameObject), smi);
            }
        }

        [HarmonyPatch(typeof(SpaceTreePlant.Instance), nameof(SpaceTreePlant.Instance.GetProductionSpeed))]
        private static class SpaceTreePlant_Instance_GetProductionSpeed
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            private static void Postfix(SpaceTreePlant.Instance __instance, ref float __result)
            {
                ModifyValueByAttributeMultiplierModifiers(yield_amount.Get(__instance), ref __result);
            }
        }

        [HarmonyPatch(typeof(SpaceTreePlant.Instance), nameof(SpaceTreePlant.Instance.OptimalProductionDuration), MethodType.Getter)]
        private static class SpaceTreePlant_Instance_OptimalProductionDuration
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            private static void Postfix(SpaceTreePlant.Instance __instance, ref float __result)
            {
                ModifyValueByAttributeMultiplierModifiers(max_maturity.Get(__instance), ref __result);
            }
        }

        // меняем шыло на мыло
        // в основном для починки странного поведения, они почти одинаковые, но первое использует == вместо >= 
        [HarmonyPatch(typeof(SpaceTreePlant.Instance), nameof(SpaceTreePlant.Instance.IsMature), MethodType.Getter)]
        private static class SpaceTreePlant_Instance_IsMature
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Transpile(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var IsGrown = typeof(Growing).GetMethodSafe(nameof(Growing.IsGrown), false);
                var ReachedNextHarvest = typeof(Growing).GetMethodSafe(nameof(Growing.ReachedNextHarvest), false);
                if (IsGrown != null && ReachedNextHarvest != null)
                {
                    instructions = PPatchTools.ReplaceMethodCallSafe(instructions, IsGrown, ReachedNextHarvest).ToList();
                    return true;
                }
                return false;
            }
        }

        // ускоряем производство сиропа пропорционально баффам
        // просто посчитаем мультиплеры к атрибуту скорости роста
        [HarmonyPatch(typeof(SpaceTreeBranch.Instance), nameof(SpaceTreeBranch.Instance.Productivity), MethodType.Getter)]
        private static class SpaceTreeBranch_Instance_Productivity
        {
            private static void Postfix(SpaceTreeBranch.Instance __instance, ref float __result, AmountInstance ___maturity)
            {
                if (__result > 0f && BetterPlantTendingOptions.Instance.space_tree_adjust_productivity && ___maturity != null)
                {
                    ModifyValueByAttributeMultiplierModifiers(___maturity.deltaAttribute, ref __result);
                }
            }
        }

        private static void ModifyValueByAttributeMultiplierModifiers(AttributeInstance attribute, ref float value)
        {
            if (attribute == null)
                return;
            var modifiers = attribute.Modifiers;
            float mult = 0f;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (!modifier.UIOnly && modifier.IsMultiplier)
                    mult += modifier.Value;
            }
            if (mult != 0f)
                value += Mathf.Abs(value) * mult;
        }

        private static string GetTooltipForAttributeMultiplierModifiers(AttributeInstance attribute, bool inverted = false)
        {
            if (attribute == null)
                return null;
            var modifiers = attribute.Modifiers;
            string text = string.Empty;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier.IsMultiplier)
                {
                    // инферсия для того случая когда нужное нам значение фактически является делителем
                    float value = inverted ? 1f / (1f + modifier.Value) - 1f : modifier.Value;
                    string formatted = GameUtil.GetFormattedPercent(value * 100f);
                    formatted = GameUtil.AddPositiveSign(formatted, value > 0f);
                    text += string.Format(DUPLICANTS.ATTRIBUTES.MODIFIER_ENTRY, modifier.GetDescription(), formatted);
                }
            }
            return text;
        }

        // добавляем больше сведений в тоолтипы

        private static IDetouredField<SpaceTreeBranch.Instance, AmountInstance> Maturity
            = PDetours.DetourField<SpaceTreeBranch.Instance, AmountInstance>("maturity");

        internal static void SpaceTree_ResolveTooltipCallback_Patch()
        {
            var statusItem_brach = Db.Get().CreatureStatusItems.SpaceTreeBranchLightStatus;
            var originCB_brach = statusItem_brach.resolveTooltipCallback;
            statusItem_brach.resolveTooltipCallback = (str, data) =>
            {
                var tooltip = originCB_brach(str, data);
                if (BetterPlantTendingOptions.Instance.space_tree_adjust_productivity)
                {
                    string text = GetTooltipForAttributeMultiplierModifiers(Maturity.Get((SpaceTreeBranch.Instance)data).deltaAttribute);
                    if (!string.IsNullOrEmpty(text))
                        tooltip = tooltip + "\n" + text;
                }
                return tooltip;
            };
            if (!DlcManager.FeaturePlantMutationsEnabled()) return;
            var statusItem_tree = Db.Get().CreatureStatusItems.ProducingSugarWater;
            var originCB_tree = statusItem_tree.resolveTooltipCallback;
            statusItem_tree.resolveTooltipCallback = (str, data) =>
            {
                var tooltip = originCB_tree(str, data);
                var smi = (SpaceTreePlant.Instance)data;
                string text = GetTooltipForAttributeMultiplierModifiers(yield_amount.Get(smi));
                if (!string.IsNullOrEmpty(text))
                    tooltip = tooltip + "\n" + text;
                text = GetTooltipForAttributeMultiplierModifiers(max_maturity.Get(smi), true);
                if (!string.IsNullOrEmpty(text))
                    tooltip = tooltip + "\n" + text;
                return tooltip;
            };
        }

        // чиним добычу сиропа вручную если дерево в ящике
        [HarmonyPatch(typeof(SpaceTreeSyrupHarvestWorkable), "OnPrefabInit")]
        private static class SpaceTreeSyrupHarvestWorkable_OnPrefabInit
        {
            private static void Postfix(SpaceTreeSyrupHarvestWorkable __instance)
            {
                __instance.SetOffsets(new CellOffset[2] { CellOffset.none, CellOffset.down });
            }
        }

        // ветка сиропового дерева не имеет Growing поэтому не умеет самосброс урожая
        // для мутации "Juicy Fruits" научим её
        private static SpaceTreeBranch.BoolParameter force_self_harvest_on_grown;

        [HarmonyPatch(typeof(SpaceTreeBranch), nameof(SpaceTreeBranch.InitializeStates))]
        private static class SpaceTreeBranch_InitializeStates
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            private static void Postfix(SpaceTreeBranch __instance, SpaceTreeBranch.GrownStates ___grown)
            {
                force_self_harvest_on_grown = __instance.AddParameter(nameof(force_self_harvest_on_grown), new SpaceTreeBranch.BoolParameter(false));
                ___grown.ScheduleAction("TrySelfHarvest", smi => Random.Range(4f, 6f), TrySelfHarvestOnGrown);
            }

            private static void TrySelfHarvestOnGrown(SpaceTreeBranch.Instance smi)
            {
                // некоторое подражание Growing
                if (!smi.IsNullOrStopped() && force_self_harvest_on_grown.Get(smi)
                    && smi.harvestable != null && smi.harvestable.CanBeHarvested)
                {
                    bool harvestWhenReady = false;
                    if (smi.harvestable.harvestDesignatable != null)
                        harvestWhenReady = smi.harvestable.harvestDesignatable.HarvestWhenReady;
                    smi.harvestable.ForceCancelHarvest();
                    smi.harvestable.Harvest();
                    if (harvestWhenReady)
                        smi.harvestable.harvestDesignatable.SetHarvestWhenReady(true);
                }
            }
        }

        [HarmonyPatch(typeof(PlantMutation), "ApplyFunctionalTo")]
        private static class PlantMutation_ApplyFunctionalTo
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            private static void Postfix(MutantPlant target, bool ___forceSelfHarvestOnGrown)
            {
                if (___forceSelfHarvestOnGrown && target != null)
                {
                    var branch = target.GetSMI<SpaceTreeBranch.Instance>();
                    if (!branch.IsNullOrDestroyed())
                        force_self_harvest_on_grown.Set(true, branch);
                }
            }
        }
    }
}
