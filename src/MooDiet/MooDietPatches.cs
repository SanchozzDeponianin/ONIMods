using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
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
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(MooDietPatches));
            new POptions().RegisterOptions(this, typeof(MooDietOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        // эффект при поедании цветов
        public const string MOO_FLOWER_FED = "MooFlowerFed";

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            var effect = new Effect(MOO_FLOWER_FED, NAME, TOOLTIP, 1f, false, false, true);
            effect.Add(new AttributeModifier(Db.Get().Amounts.Beckoning.deltaAttribute.Id,
                -MooTuning.WELLFED_EFFECT * MooDietOptions.Instance.flower_diet.beckoning_penalty, NAME));
            Db.Get().effects.Add(effect);
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
                public Diet.Info.FoodType food_type;
                public FoodInfo(string id, Tag output_ID, float calories_multiplier = 1f, float output_multiplier = 1f, Diet.Info.FoodType food_type = Diet.Info.FoodType.EatSolid)
                {
                    ID = id;
                    this.output_ID = output_ID;
                    this.calories_multiplier = calories_multiplier;
                    this.output_multiplier = output_multiplier;
                    this.food_type = food_type;
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
                        Traverse.Create(diet_info).Property<Diet.Info.FoodType>(nameof(Diet.Info.foodType)).Value = Diet.Info.FoodType.EatPlantDirectly;
                    }
                    // добавляем новые варианты к существующей диете
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
                            food_type: Diet.Info.FoodType.EatPlantDirectly));
                    }
                    if (MooDietOptions.Instance.palmera_diet.eat_berry && Assets.TryGetPrefab(PalmeraBerry) != null
                        && CROPS.CROP_TYPES.FindIndex(m => m.cropId == PalmeraBerry) != -1)
                    {
                        new_foods.Add(new FoodInfo(PalmeraBerry, ElementLoader.FindElementByHash(SimHashes.Hydrogen).tag,
                            MooDietOptions.Instance.palmera_diet.palmera_per_cow / MooConfig.DAYS_PLANT_GROWTH_EATEN_PER_CYCLE));
                    }
                    var new_diet = ccm_def.diet.infos.ToList();
                    foreach (var food in new_foods)
                    {
                        // сколько корове нужно растений на прокорм в день - фактически уже учтено в диете по умолчанию
                        // просто разделим параметры на среднюю урожайность растения в день
                        // а также, для цветов - поделим калорийность на фактор увеличения числа растений
                        float crop_per_cycle = 1f;
                        if (food.food_type != Diet.Info.FoodType.EatPlantDirectly)
                        {
                            var cropVal = CROPS.CROP_TYPES.Find(m => m.cropId == food.ID);
                            crop_per_cycle = Constants.SECONDS_PER_CYCLE / cropVal.cropDuration * cropVal.numProduced;
                        }
                        new_diet.RemoveAll(info => info.consumedTags.Contains(food.ID)); // удаляем дупликаты от посторонних модов.
                        new_diet.Add(new Diet.Info(
                            consumed_tags: new HashSet<Tag>() { food.ID },
                            produced_element: food.output_ID,
                            calories_per_kg: diet_info.caloriesPerKg / crop_per_cycle / food.calories_multiplier,
                            produced_conversion_rate: diet_info.producedConversionRate / crop_per_cycle / food.calories_multiplier * food.output_multiplier,
                            food_type: food.food_type));
                    }
                    var hybridDiet = new Diet(new_diet.ToArray());
                    ccm_def.diet = hybridDiet;
                    var scm_def = moo_go.GetDef<SolidConsumerMonitor.Def>();
                    if (scm_def != null)
                        scm_def.diet = hybridDiet;
                }
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
