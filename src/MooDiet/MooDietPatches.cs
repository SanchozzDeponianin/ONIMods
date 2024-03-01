using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Klei.AI;
using TUNING;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace MooDiet
{
    using static STRINGS.CREATURES.MODIFIERS.MOOFLOWERFED;

    internal sealed class MooDietPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Utils.LogModVersion();
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(MooDietPatches));
            new POptions().RegisterOptions(this, typeof(MooDietOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        public const string MOO_FLOWER_FED = "MooFlowerFed";

        // эффект при поедании цветов
        [HarmonyPatch(typeof(ModifierSet), "LoadEffects")]
        private static class ModifierSet_LoadEffects
        {
            private static void Postfix(ModifierSet __instance)
            {
                var effect = new Effect(MOO_FLOWER_FED, NAME, TOOLTIP, 1f, false, false, true);
                effect.Add(new AttributeModifier(__instance.Amounts.Beckoning.deltaAttribute.Id,
                    -MooTuning.WELLFED_EFFECT * MooDietOptions.Instance.flower_diet.beckoning_penalty, NAME));
                __instance.effects.Add(effect);
            }
        }

        // расширяем диету
        [HarmonyPatch(typeof(BuildingConfigManager), nameof(BuildingConfigManager.ConfigurePost))]
        private static class BuildingConfigManager_ConfigurePost
        {
            public struct FoodInfo
            {
                public string ID;
                public Tag output_ID;
                public float calories_multiplier;
                public float output_multiplier;
                public bool eatsPlantsDirectly;
                public FoodInfo(string id, Tag output_ID, float calories_multiplier = 1f, float output_multiplier = 1f, bool eatsPlantsDirectly = false)
                {
                    ID = id;
                    this.output_ID = output_ID;
                    this.calories_multiplier = calories_multiplier;
                    this.output_multiplier = output_multiplier;
                    this.eatsPlantsDirectly = eatsPlantsDirectly;
                }
            }
            public const string PalmeraTreePlant = "PalmeraTreePlant";
            public const string PalmeraBerry = "PalmeraBerry";
            private static void Prefix()
            {
                var moo_go = Assets.GetPrefab(MooConfig.ID);
                if (moo_go == null)
                    return;
                var ccm_def = moo_go.GetDef<CreatureCalorieMonitor.Def>();
                if (ccm_def != null && ccm_def.diet != null)
                {
                    var diet_info = ccm_def.diet.GetDietInfo(GasGrassConfig.ID);
                    if (diet_info == null)
                    {
                        // 98% что это изза мода "Very Gassy Moos"
                        // откатим некоторые его манипуляции
                        diet_info = ccm_def.diet.GetDietInfo(GasGrassHarvestedConfig.ID);
                        if (diet_info == null)
                        {
                            PUtil.LogWarning("Alarm! Cow don`t eat Grass!!!");
                            return;
                        }
                        diet_info.consumedTags.Remove(GasGrassHarvestedConfig.ID);
                        diet_info.consumedTags.Add(GasGrassConfig.ID);
                        Traverse.Create(diet_info).Property<bool>(nameof(Diet.Info.eatsPlantsDirectly)).Value = true;
                    }
                    // добавляем новые варианты к существующей диете
                    // потенциально крашабельно, если сторонний мод также понадобовляет диет с травой и цветами
                    var new_foods = new List<FoodInfo>()
                    {
                        new FoodInfo(GasGrassHarvestedConfig.ID, MooConfig.POOP_ELEMENT),
                        new FoodInfo(SwampLilyFlowerConfig.ID, ElementLoader.FindElementByHash(SimHashes.ChlorineGas).tag,
                            MooDietOptions.Instance.flower_diet.lily_per_cow / MooConfig.DAYS_PLANT_GROWTH_EATEN_PER_CYCLE,
                            MooDietOptions.Instance.flower_diet.gas_multiplier),
                    };
                    // Palmera Tree
                    if (MooDietOptions.Instance.palmera_diet.eat_palmera && Assets.TryGetPrefab(PalmeraTreePlant) != null)
                    {
                        new_foods.Add(new FoodInfo(PalmeraTreePlant, ElementLoader.FindElementByHash(SimHashes.Hydrogen).tag,
                            MooDietOptions.Instance.palmera_diet.palmera_per_cow / MooConfig.DAYS_PLANT_GROWTH_EATEN_PER_CYCLE,
                            eatsPlantsDirectly: true));
                    }
                    if (MooDietOptions.Instance.palmera_diet.eat_berry && Assets.TryGetPrefab(PalmeraBerry) != null
                        && CROPS.CROP_TYPES.FindIndex(m => m.cropId == PalmeraBerry) != -1)
                    {
                        new_foods.Add(new FoodInfo(PalmeraBerry, ElementLoader.FindElementByHash(SimHashes.Hydrogen).tag,
                            MooDietOptions.Instance.palmera_diet.palmera_per_cow / MooConfig.DAYS_PLANT_GROWTH_EATEN_PER_CYCLE));
                    }
                    var new_diet = ccm_def.diet.infos;
                    foreach (var food in new_foods)
                    {
                        // сколько корове нужно растений на прокорм в день - фактически уже учтено в диете по умолчанию
                        // просто разделим параметры на среднюю урожайность растения в день
                        // а также, для цветов - поделим калорийность на фактор увеличения числа растений
                        float crop_per_cycle = 1f;
                        if (!food.eatsPlantsDirectly)
                        {
                            var cropVal = CROPS.CROP_TYPES.Find(m => m.cropId == food.ID);
                            crop_per_cycle = Constants.SECONDS_PER_CYCLE / cropVal.cropDuration * cropVal.numProduced;
                        }
                        new_diet = new_diet.AddItem(new Diet.Info(new HashSet<Tag>() { food.ID }, food.output_ID,
                            diet_info.caloriesPerKg / crop_per_cycle / food.calories_multiplier,
                            diet_info.producedConversionRate / crop_per_cycle / food.calories_multiplier * food.output_multiplier)).ToArray();
                    }
                    var hybridDiet = new HybridDiet(new_diet);
                    ccm_def.diet = hybridDiet;
                    var scm_def = moo_go.GetDef<SolidConsumerMonitor.Def>();
                    if (scm_def != null)
                        scm_def.diet = hybridDiet;
                }
            }
        }

        // исправляем поиск еды для жеготных со смешанной диетой
        [HarmonyPatch(typeof(SolidConsumerMonitor), "FindFood")]
        private static class SolidConsumerMonitor_FindFood
        {
            // ох бл
            /*
        --- if (!diet.eatsPlantsDirectly)
        +++ if (!NotNeedCreatureFeeder(diet))
            {
                блабла чтото там CreatureFeeder кормушка
            }
        --- if (diet.eatsPlantsDirectly)
        +++ if (CanEatsPlants(diet))
            {
                блабла чтото там GameScenePartitioner plants живые растения
        +++     if (CanEatsNonPlants(diet)) goto else;
            }
            else
            {
                блабла чтото там GameScenePartitioner pickupablesLayer материалы на полу
            }
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL)
            {
                return TranspilerUtils.Wrap(instructions, original, IL, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions, ILGenerator IL)
            {
                var def_diet = typeof(SolidConsumerMonitor.Def).GetFieldSafe(nameof(SolidConsumerMonitor.Def.diet), false);
                var diet_eatsPlantsDirectly = typeof(Diet).GetFieldSafe(nameof(Diet.eatsPlantsDirectly), false);

                var notNeedCreatureFeeder = typeof(HybridDiet).GetMethodSafe(nameof(HybridDiet.NotNeedCreatureFeeder), true, typeof(Diet));
                var canEatsPlants = typeof(HybridDiet).GetMethodSafe(nameof(HybridDiet.CanEatsPlants), true, typeof(Diet));
                var canEatsNonPlants = typeof(HybridDiet).GetMethodSafe(nameof(HybridDiet.CanEatsNonPlants), true, typeof(Diet));

                if (def_diet == null || diet_eatsPlantsDirectly == null
                    || notNeedCreatureFeeder == null || canEatsPlants == null || canEatsNonPlants == null)
                    return false;

                int diet_idx = instructions.FindIndex(ins => ins.LoadsField(def_diet));
                if (diet_idx == -1 || !instructions[diet_idx + 1].IsStloc())
                    return false;
                var diet_var = TranspilerUtils.GetMatchingLoadInstruction(instructions[diet_idx + 1]);

                int first_idx = instructions.FindIndex(ins => ins.LoadsField(diet_eatsPlantsDirectly));
                if (first_idx == -1)
                    return false;

                int second_idx = instructions.FindIndex(first_idx + 1, ins => ins.LoadsField(diet_eatsPlantsDirectly));
                if (second_idx == -1)
                    return false;

                int br_else_idx = instructions.FindIndex(second_idx + 1, ins => ins.Branches(out _));
                if (br_else_idx == -1)
                    return false;
                instructions[br_else_idx].Branches(out var else_label);

                int else_idx = instructions.FindIndex(br_else_idx, ins => ins.labels.Contains((Label)else_label));
                if (else_idx == -1)
                    return false;

                int end_if_idx = instructions.FindLastIndex(else_idx, ins =>
                    ins.opcode == OpCodes.Br || ins.opcode == OpCodes.Br_S || ins.opcode == OpCodes.Ret);
                if (end_if_idx == -1)
                    return false;

                instructions[first_idx].opcode = OpCodes.Call;
                instructions[first_idx].operand = notNeedCreatureFeeder;
                instructions[second_idx].opcode = OpCodes.Call;
                instructions[second_idx].operand = canEatsPlants;

                instructions.Insert(end_if_idx++, diet_var);
                instructions.Insert(end_if_idx++, new CodeInstruction(OpCodes.Call, canEatsNonPlants));
                instructions.Insert(end_if_idx++, new CodeInstruction(OpCodes.Brtrue, (Label)else_label));
#if DEBUG
                PPatchTools.DumpMethodBody(instructions);
#endif
                return true;
            }
        }

        // применяем эффект если корова сожрала цветы вместо травы
        [HarmonyPatch(typeof(BeckoningMonitor.Instance), nameof(BeckoningMonitor.Instance.OnCaloriesConsumed))]
        private static class BeckoningMonitor_Instance_OnCaloriesConsumed
        {
            private static void Postfix(BeckoningMonitor.Instance __instance, Effects ___effects, object data)
            {
                var @event = (CreatureCalorieMonitor.CaloriesConsumedEvent)data;
                if (@event.tag == SwampLilyFlowerConfig.ID)
                {
                    var effect = ___effects.Get(MOO_FLOWER_FED);
                    if (effect == null)
                        effect = ___effects.Add(MOO_FLOWER_FED, true);
                    effect.timeRemaining += @event.calories / __instance.def.caloriesPerCycle * Constants.SECONDS_PER_CYCLE;
                }
            }
        }

        // исправленный описатель для жеготных со смешанной диетой, стандартный крашится
        [HarmonyPatch(typeof(CreatureCalorieMonitor.Def), nameof(CreatureCalorieMonitor.Def.GetDescriptors))]
        private static class CreatureCalorieMonitor_Def_GetDescriptors
        {
            private static bool Prefix(CreatureCalorieMonitor.Def __instance, GameObject obj, ref List<Descriptor> __result)
            {
                if (__instance.diet is HybridDiet)
                {
                    __result = HybridDiet.GetDescriptors(obj, __instance.diet);
                    return false;
                }
                return true;
            }
        }

        // добавляем в кормушку новый корм для коровы
        [HarmonyPatch(typeof(CreatureFeederConfig), nameof(CreatureFeederConfig.ConfigurePost))]
        private static class CreatureFeederConfig_ConfigurePost
        {
            private static void Postfix(BuildingDef def)
            {
                var storage = def.BuildingComplete.GetComponent<Storage>();
                foreach (var diet in DietManager.CollectDiets(new Tag[] { GameTags.Creatures.Species.MooSpecies }))
                {
                    if (!storage.storageFilters.Contains(diet.Key))
                        storage.storageFilters.Add(diet.Key);
                }
            }
        }

        // todo: добавить в кодекс в раздел молока расширенную информацию об расширенной диете
    }
}
