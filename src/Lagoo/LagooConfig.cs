﻿using System.Collections.Generic;
using Klei.AI;
using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;
using static STRINGS.UI;

namespace Lagoo
{
    using static LagooPatches;
    using static LagooTuning;
    using static STRINGS.CREATURES.SPECIES.SQUIRREL.VARIANT_LAGOO;

    [EntityConfigOrder(1)]
    public class LagooConfig : IEntityConfig
    {
        public const string ID = "SquirrelLagoo";
        public const string TRAIT_ID = ID + "BaseTrait";
        public const string BABY_ID = ID + "Baby";
        public const string EGG_ID = ID + "Egg";
        public const string ANIM_PREFIX = "lagoo_";
        private const int EGG_SORT_ORDER = 0;

        public string[] GetDlcIds() => Utils.GetDlcIds(DlcManager.AVAILABLE_ALL_VERSIONS);

        public static GameObject CreateSquirrelLagoo(string id, string name, string desc, string anim_file, bool is_baby)
        {
            var lagoo = BaseSquirrelConfig.BaseSquirrel(id, name, desc, anim_file, TRAIT_ID, is_baby, ANIM_PREFIX, true);
            lagoo = EntityTemplates.ExtendEntityToWildCreature(lagoo, SquirrelTuning.PEN_SIZE_PER_CREATURE_HUG);
            //lagoo.AddOrGet<DecorProvider>().SetValues(DECOR.BONUS.TIER3);
            var trait = Db.Get().CreateTrait(TRAIT_ID, name, name, null, false, null, true, true);
            trait.Add(new AttributeModifier(Db.Get().Amounts.Calories.maxAttribute.Id, SquirrelTuning.STANDARD_STOMACH_SIZE, name));
            trait.Add(new AttributeModifier(Db.Get().Amounts.Calories.deltaAttribute.Id,
                -SquirrelTuning.STANDARD_CALORIES_PER_CYCLE / Constants.SECONDS_PER_CYCLE, TOOLTIPS.BASE_VALUE));
            trait.Add(new AttributeModifier(Db.Get().Amounts.HitPoints.maxAttribute.Id, CREATURES.HITPOINTS.TIER1, name));
            trait.Add(new AttributeModifier(Db.Get().Amounts.Age.maxAttribute.Id, CREATURES.LIFESPAN.TIER3, name));
            // расшыряем диету
            var diet_infos = BaseSquirrelConfig.BasicDiet(EMIT_ELEMENT.CreateTag(),
                CALORIES_PER_DAY_OF_PLANT_EATEN, KG_POOP_PER_DAY_OF_PLANT, GERM_ID_EMMITED_ON_POOP, GERMS_EMMITED_PER_KG_POOPED);
            var consumed = diet_infos[0].consumedTags;
            consumed.Add(ColdWheatConfig.ID);
            if (DlcManager.IsContentSubscribed(DlcManager.DLC2_ID))
            {
                consumed.Add(CarrotPlantConfig.ID);
                consumed.Add(HardSkinBerryPlantConfig.ID);
            }
            Assets.GetPrefab(ColdWheatConfig.ID).AddOrGet<DirectlyEdiblePlant_Growth>();
            // добавим серу
            diet_infos = diet_infos.Append(new Diet.Info(new HashSet<Tag> { SimHashes.Sulfur.CreateTag() }, SimHashes.Gunk.CreateTag(),
                CALORIES_PER_KG_OF_ORE, CREATURES.CONVERSION_EFFICIENCY.GOOD_3, GERM_ID_EMMITED_ON_POOP, GERMS_EMMITED_PER_KG_POOPED));
            BaseSquirrelConfig.SetupDiet(lagoo, diet_infos, MIN_POOP_SIZE_KG);
            lagoo.AddOrGet<DiseaseSourceVisualizer>().alwaysShowDisease = GERM_ID_EMMITED_ON_POOP;
            // обнимашки с дуплями с уменьшенным таймером
            if (!is_baby)
                lagoo.AddOrGetDef<HugMonitor.Def>().hugFrenzyCooldown = 0.5f * HugMonitor.HUGTUNING.HUG_FRENZY_DURATION;
            // отключаем обнимашки с яйцами
            // todo: включить обратно когда будут нарисованы соответсвующие ушы
            var def = lagoo.GetComponent<ChoreConsumer>().choreTable.GetEntry<HugEggStates.Def>().stateMachineDef;
            if (def is HugEggStates.Def egg_def)
                egg_def.behaviourTag = Tag.Invalid;
            // выживаем в холоде
            var temperature_monitor = lagoo.AddOrGetDef<CritterTemperatureMonitor.Def>();
            temperature_monitor.temperatureColdDeadly = -70f + Constants.CELSIUS2KELVIN;
            // это из EntityTemplates.ExtendEntityToBasicCreature, предположительно для мамонтов
            // зделаем тёплым
            const string temperature = "CritterTemperature";
            string proper_name = lagoo.GetProperName();
            lagoo.UpdateComponentRequirement<SimTemperatureTransfer>(false);
            var transfer = lagoo.AddOrGet<CreatureSimTemperatureTransfer>();
            transfer.temperatureAttributeName = temperature;
            transfer.SurfaceArea = CREATURES.TEMPERATURE.SURFACE_AREA;
            transfer.Thickness = CREATURES.TEMPERATURE.SKIN_THICKNESS;
            transfer.GroundTransferScale = CREATURES.TEMPERATURE.GROUND_TRANSFER_SCALE;
            transfer.skinThickness = CREATURES.TEMPERATURE.SKIN_THICKNESS;
            transfer.skinThicknessAttributeModifierName = proper_name;
            var blooded = lagoo.AddOrGet<WarmBlooded>();
            blooded.TemperatureAmountName = temperature;
            blooded.complexity = WarmBlooded.ComplexityType.SimpleHeatProduction;
            blooded.IdealTemperature = temperature_monitor.GetIdealTemperature();
            // todo: понаблюдать
            blooded.BaseGenerationKW = 0.3f;//BellyTuning.KW_GENERATED_TO_WARM_UP;
            blooded.BaseTemperatureModifierDescription = proper_name;
            return lagoo;
        }

        public GameObject CreatePrefab()
        {
            var prefab = CreateSquirrelLagoo(ID, NAME, DESC, squirrel_kanim, false);
            const float fertility_cycles = CREATURES.LIFESPAN.TIER3 * CREATURES.FERTILITY_TIME_BY_LIFESPAN;
            const float incubation_cycles = CREATURES.LIFESPAN.TIER3 * CREATURES.INCUBATION_TIME_BY_LIFESPAN;
            EntityTemplates.ExtendEntityToFertileCreature(prefab, EGG_ID, EGG_NAME, EGG_DESC,
                egg_squirrel_kanim, SquirrelTuning.EGG_MASS, BABY_ID, fertility_cycles, incubation_cycles,
                EGG_CHANCES_LAGOO, GetDlcIds(), EGG_SORT_ORDER);
            SquirrelTuning.EGG_CHANCES_BASE.Add(new FertilityMonitor.BreedingChance() { egg = EGG_ID, weight = 0f });
            SquirrelTuning.EGG_CHANCES_HUG.Add(new FertilityMonitor.BreedingChance() { egg = EGG_ID, weight = 0f });
            return prefab;
        }

        public void OnPrefabInit(GameObject prefab) { }

        public void OnSpawn(GameObject inst)
        {
            if (inst.TryGetComponent(out KBatchedAnimController kbac))
                kbac.AddAnimOverrides(Assets.GetAnim(lagoo_kanim), 1);
        }
    }
}
