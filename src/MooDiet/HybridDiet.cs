using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Klei.AI;
using TUNING;
using static STRINGS.UI.BUILDINGEFFECTS;

namespace MooDiet
{
    public class HybridDiet : Diet
    {
        // оригинальная Diet фактически не поддерживает смешанную диету из материалов и живых растений
        // так как устанавливает свой флаг eatsPlantsDirectly = true при наличии хотя бы одного infos с диетой из растений
        // а далее логика где проверяется eatsPlantsDirectly как бы приводит к игнорированию других infos с диетой из материалов
        // попробуем разделить на два флага, но нужно будет пропатчить все места где проверяется eatsPlantsDirectly

        private bool canEatsPlants = false;
        private bool canEatsNonPlants = false;

        public HybridDiet(params Info[] infos) : base(infos)
        {
            for (int i = 0; i < infos.Length; i++)
            {
                if (infos[i].eatsPlantsDirectly)
                    canEatsPlants = true;
                else
                    canEatsNonPlants = true;
            }
        }

        public static bool CanEatsPlants(Diet diet)
        {
            if (diet == null)
                return false;
            var hybridDiet = diet as HybridDiet;
            if (hybridDiet != null)
                return hybridDiet.canEatsPlants;
            else
                return diet.eatsPlantsDirectly;
        }

        public static bool CanEatsNonPlants(Diet diet)
        {
            if (diet == null)
                return false;
            var hybridDiet = diet as HybridDiet;
            if (hybridDiet != null)
                return hybridDiet.canEatsNonPlants;
            else
                return !diet.eatsPlantsDirectly;
        }

        public static bool NotNeedCreatureFeeder(Diet diet)
        {
            return !CanEatsNonPlants(diet);
        }

        // скопипизжено из CreatureCalorieMonitor.Def.GetDescriptors
        // и исправлено для поддержки смешанных диет
        public static List<Descriptor> GetDescriptors(GameObject obj, Diet diet)
        {
            List<Descriptor> list = new List<Descriptor> { new Descriptor(DIET_HEADER, TOOLTIPS.DIET_HEADER) };
            float dailyPlantGrowthConsumption = 1f;
            if (diet.consumedTags.Count > 0)
            {
                float calorie_loss_per_second = 0f;
                var trait = Db.Get().traits.Get(obj.GetComponent<Modifiers>().initialTraits[0]);
                foreach (var modifier in trait.SelfModifiers)
                {
                    if (modifier.AttributeId == Db.Get().Amounts.Calories.deltaAttribute.Id)
                    {
                        calorie_loss_per_second = modifier.Value;
                        break;
                    }
                }
                string consumed_txt = string.Join(", ", diet.consumedTags.Select(t => t.Key.ProperName()));
                string consumed_tooltip = string.Join("\n", diet.consumedTags.Select(t =>
                {
                    float mass_per_cycle = -calorie_loss_per_second / t.Value;
                    var diet_info = diet.GetDietInfo(t.Key);
                    if (diet_info.eatsPlantsDirectly)
                    {
                        dailyPlantGrowthConsumption = mass_per_cycle;
                        var prefab = Assets.GetPrefab(t.Key);
                        var crop = prefab.GetComponent<Crop>();
                        var cropVal = CROPS.CROP_TYPES.Find(m => m.cropId == crop.cropId);
                        float plant_growth_per_cycle = Constants.SECONDS_PER_CYCLE / cropVal.cropDuration;
                        return DIET_CONSUMED_ITEM.text.Replace("{Food}", t.Key.ProperName())
                            .Replace("{Amount}", GameUtil.GetFormattedPlantGrowth(mass_per_cycle * plant_growth_per_cycle * 100f, GameUtil.TimeSlice.PerCycle));
                    }
                    else
                    {
                        return DIET_CONSUMED_ITEM.text.Replace("{Food}", t.Key.ProperName())
                            .Replace("{Amount}", GameUtil.GetFormattedMass(mass_per_cycle, GameUtil.TimeSlice.PerCycle, GameUtil.MetricMassFormat.Kilogram));
                    }
                }));
                list.Add(new Descriptor(DIET_CONSUMED.text.Replace("{Foodlist}", consumed_txt),
                    TOOLTIPS.DIET_CONSUMED.text.Replace("{Foodlist}", consumed_tooltip)));
            }
            if (diet.producedTags.Count > 0)
            {
                string produced_txt = string.Join(", ", diet.producedTags.Select(t => t.Key.ProperName()));
                string produced_tooltip = string.Join("\n", diet.producedTags.SelectMany(t => new Info[]
                    {
                        diet.infos.FirstOrDefault(i => i.producedElement == t.Key && i.eatsPlantsDirectly),
                        diet.infos.FirstOrDefault(j => j.producedElement == t.Key && !j.eatsPlantsDirectly),
                    }).Where(t => t != null).Select(info =>
                    {
                        if (info.eatsPlantsDirectly)
                        {
                            return DIET_PRODUCED_ITEM_FROM_PLANT.text.Replace("{Item}", info.producedElement.ProperName())
                            .Replace("{Amount}", GameUtil.GetFormattedMass(info.producedConversionRate * dailyPlantGrowthConsumption, GameUtil.TimeSlice.PerCycle, GameUtil.MetricMassFormat.Kilogram));
                        }
                        else
                        {
                            return DIET_PRODUCED_ITEM.text.Replace("{Item}", info.producedElement.ProperName())
                            .Replace("{Percent}", GameUtil.GetFormattedPercent(info.producedConversionRate * 100f, GameUtil.TimeSlice.None));
                        }
                    }));
                list.Add(new Descriptor(DIET_PRODUCED.text.Replace("{Items}", produced_txt),
                    TOOLTIPS.DIET_PRODUCED.text.Replace("{Items}", produced_tooltip)));
            }
            return list;
        }
    }
}
