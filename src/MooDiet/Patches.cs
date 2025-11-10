using System.Linq;
using HarmonyLib;
using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace MooDiet
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbPostProcess)]
        private static void AfterDbPostProcess()
        {
            var moo = Assets.GetPrefab(MooConfig.ID);
            if (moo != null)
                ExpandDiet(moo, MooConfig.POOP_ELEMENT);
            var diesel = Assets.GetPrefab(DieselMooConfig.ID);
            if (diesel != null)
                ExpandDiet(diesel, DieselMooConfig.POOP_ELEMENT);
            // добавляем в кормушку новый корм для коровы
            var storage = Assets.GetBuildingDef(CreatureFeederConfig.ID).BuildingComplete.GetComponent<Storage>();
            foreach (var diet in DietManager.CollectDiets(new Tag[] { GameTags.Creatures.Species.MooSpecies }))
            {
                if (diet.Value.CanEatAnySolid && !storage.storageFilters.Contains(diet.Key))
                    storage.storageFilters.Add(diet.Key);
            }
            // todo: превращять траву в солому если мод специй не включен ???
        }

        public const string PalmeraTreePlant = "PalmeraTreePlant";
        public const string PalmeraBerry = "PalmeraBerry";

        private static void ExpandDiet(GameObject prefab, Tag producedTag)
        {
            var ccm_def = prefab.GetDef<CreatureCalorieMonitor.Def>();
            if (ccm_def != null && ccm_def.diet != null)
            {
                var diet = ccm_def.diet;
                // вытащим размер выхлопа на случай если VeryGassyMoos их увеличил
                float poop_size;
                var info = diet.GetDietInfo(GasGrassConfig.ID);
                if (info != null)
                    poop_size = info.producedConversionRate * MooTuning.CALORIES_PER_DAY_OF_PLANT_EATEN / info.caloriesPerKg;
                else
                    poop_size = MooTuning.KG_POOP_PER_DAY_OF_PLANT;

                // Harvested Gas Grass / Exotic Spices
                var producer = Assets.GetPrefab(GasGrassConfig.ID).GetComponent<PlantFiberProducer>();
                if (producer != null && producer.amount > 0f)
                {
                    var fiber = CROPS.CROP_TYPES.Find(crop => crop.cropId == PlantFiberConfig.ID);
                    float grass_per_day = producer.amount / fiber.cropDuration * Constants.SECONDS_PER_CYCLE
                        * MooTuning.DAYS_PLANT_GROWTH_EATEN_PER_CYCLE / CROPS.WILD_GROWTH_RATE_MODIFIER;
                    diet = ExpandDiet(diet, prefab, GasGrassHarvestedConfig.ID, producedTag, grass_per_day, poop_size, Diet.Info.FoodType.EatSolid);
                }

                // Balm Lily
                if (ModOptions.Instance.flower_diet.eat_lily)
                {
                    float lily_per_day = ModOptions.Instance.flower_diet.lily_per_cow;
                    diet = ExpandDiet(diet, prefab, SwampLilyConfig.ID, SimHashes.ChlorineGas.CreateTag(), lily_per_day, poop_size, Diet.Info.FoodType.EatPlantDirectly);
                }
                if (ModOptions.Instance.flower_diet.eat_flower)
                {
                    var flover = CROPS.CROP_TYPES.Find(crop => crop.cropId == SwampLilyFlowerConfig.ID);
                    float flover_per_day = flover.numProduced / flover.cropDuration * Constants.SECONDS_PER_CYCLE
                        * ModOptions.Instance.flower_diet.lily_per_cow;
                    diet = ExpandDiet(diet, prefab, SwampLilyFlowerConfig.ID, SimHashes.ChlorineGas.CreateTag(), flover_per_day, poop_size, Diet.Info.FoodType.EatSolid);
                }

                // Palmera Tree
                if (ModOptions.Instance.palmera_diet.eat_palmera && Assets.TryGetPrefab(PalmeraTreePlant) != null)
                {
                    float palmera_per_day = ModOptions.Instance.palmera_diet.palmera_per_cow;
                    diet = ExpandDiet(diet, prefab, PalmeraTreePlant, SimHashes.Hydrogen.CreateTag(), palmera_per_day, poop_size, Diet.Info.FoodType.EatPlantDirectly);
                }
                if (ModOptions.Instance.palmera_diet.eat_berry && Assets.TryGetPrefab(PalmeraBerry) != null
                    && CROPS.CROP_TYPES.FindIndex(m => m.cropId == PalmeraBerry) != -1)
                {
                    var berry = CROPS.CROP_TYPES.Find(crop => crop.cropId == PalmeraBerry);
                    float berry_per_day = berry.numProduced / berry.cropDuration * Constants.SECONDS_PER_CYCLE
                        * ModOptions.Instance.palmera_diet.palmera_per_cow;
                    diet = ExpandDiet(diet, prefab, PalmeraBerry, SimHashes.Hydrogen.CreateTag(), berry_per_day, poop_size, Diet.Info.FoodType.EatSolid);
                }
                ccm_def.diet = diet;
                var scm_def = prefab.GetDef<SolidConsumerMonitor.Def>();
                if (scm_def != null)
                    scm_def.diet = diet;
            }
        }

        private static Diet ExpandDiet(Diet diet, GameObject prefab, Tag consumed_tag, Tag producedTag,
            float eaten_per_day, float poop_size, Diet.Info.FoodType foodType)
        {
            // удаляем дупликаты от посторонних модов
            var dupe = diet.GetDietInfo(consumed_tag);
            if (dupe != null)
            {
                PUtil.LogWarning("{0} ({1}) has duplicate diet entry: {2} ({3}), removing.".F(
                    prefab.PrefabID().ProperNameStripLink(), prefab.PrefabID().ToString(), 
                    consumed_tag.ProperNameStripLink(), consumed_tag.ToString()));
                var infos = diet.infos.ToList();
                dupe.consumedTags.Remove(consumed_tag);
                if (dupe.consumedTags.Count == 0)
                    infos.Remove(dupe);
                diet = new Diet(infos.ToArray());
            }
            return BaseMooConfig.ExpandDiet(diet, prefab, consumed_tag, producedTag,
                caloriesPerKg: MooTuning.STANDARD_CALORIES_PER_CYCLE / eaten_per_day,
                producedConversionRate: poop_size * MooTuning.DAYS_PLANT_GROWTH_EATEN_PER_CYCLE / eaten_per_day,
                foodType, MooTuning.MIN_POOP_SIZE_IN_KG);
        }
    }
}
