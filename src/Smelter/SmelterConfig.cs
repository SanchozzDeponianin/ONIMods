using System.Collections.Generic;
using System.Linq;
using TUNING;
using Harmony;
using UnityEngine;

namespace Smelter
{
    // частично скопипизжено с электроплавильни
    public class SmelterConfig : IBuildingConfig
    {
        public const string ID = "Smelter";
        private const float LIQUID_COOLED_HEAT_PORTION = 0.8f;
        private static readonly Tag COOLANT_TAG = GameTags.Liquid;
        private const float COOLANT_MASS = 400f;
        private const float FUEL_STORE_CAPACITY = 300f;
        private const float FUEL_CONSUME_RATE = 5f / 3f;
        private static readonly Tag FUEL_TAG = SimHashes.RefinedCarbon.CreateTag();
        internal const float START_FUEL_MASS = BUILDINGS.FABRICATION_TIME_SECONDS.SHORT * FUEL_CONSUME_RATE;
        private const float CO2_EMIT_RATE = 0.05f;
        private const float CO2_OUTPUT_TEMPERATURE = 383.15f;

        private static readonly List<Storage.StoredItemModifier> RefineryStoredItemModifiers = new List<Storage.StoredItemModifier>
        {
            Storage.StoredItemModifier.Hide,
            Storage.StoredItemModifier.Preserve,
            Storage.StoredItemModifier.Insulate,
            Storage.StoredItemModifier.Seal
        };

        public override BuildingDef CreateBuildingDef()
        {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 3,
                height: 3,
                anim: "smelter_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER3,
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER3[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER4[0] },
                construction_materials: new string[] { MATERIALS.METAL, MATERIALS.BUILDABLERAW },
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER2,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.PENALTY.TIER2,
                noise: NOISE_POLLUTION.NOISY.TIER6
                );
            buildingDef.RequiresPowerInput = false;
            buildingDef.SelfHeatKilowattsWhenActive = BUILDINGS.SELF_HEAT_KILOWATTS.TIER4;
            buildingDef.ExhaustKilowattsWhenActive = BUILDINGS.EXHAUST_ENERGY_ACTIVE.TIER8;
            buildingDef.OverheatTemperature = BUILDINGS.OVERHEAT_TEMPERATURES.HIGH_2;
            buildingDef.InputConduitType = ConduitType.Liquid;
            buildingDef.UtilityInputOffset = new CellOffset(1, 0);
            buildingDef.OutputConduitType = ConduitType.None;
            buildingDef.ViewMode = OverlayModes.LiquidConduits.ID;
            buildingDef.AudioCategory = "HollowMetal";
            buildingDef.AudioSize = "large";
            buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<DropAllWorkable>();
            go.AddOrGet<BuildingComplete>().isManuallyOperated = true;
            go.AddOrGet<FabricatorIngredientStatusManager>();
            go.AddOrGet<CopyBuildingSettings>();

            var lcfr = go.AddOrGet<LiquidCooledFueledRefinery>();
            lcfr.duplicantOperated = true;
            lcfr.sideScreenStyle = ComplexFabricatorSideScreen.StyleSetting.ListQueueHybrid;
            lcfr.keepExcessLiquids = true;
            BuildingTemplates.CreateComplexFabricatorStorage(go, lcfr);
            lcfr.coolantTag = COOLANT_TAG;
            lcfr.minCoolantMass = COOLANT_MASS;
            lcfr.maxCoolantMass = COOLANT_MASS * 3;
            lcfr.outStorage.capacityKg = 2000f;
            lcfr.thermalFudge = LIQUID_COOLED_HEAT_PORTION;
            lcfr.fuelTag = FUEL_TAG;
            lcfr.inStorage.SetDefaultStoredItemModifiers(RefineryStoredItemModifiers);
            lcfr.buildStorage.SetDefaultStoredItemModifiers(RefineryStoredItemModifiers);
            lcfr.outStorage.SetDefaultStoredItemModifiers(RefineryStoredItemModifiers);
            lcfr.outputOffset = new Vector3(0.8f, 0.5f);

            var manualDeliveryKG = go.AddOrGet<ManualDeliveryKG>();
            manualDeliveryKG.SetStorage(lcfr.outStorage);
            manualDeliveryKG.requestedItemTag = FUEL_TAG;
            manualDeliveryKG.capacity = FUEL_STORE_CAPACITY;
            manualDeliveryKG.refillMass = FUEL_STORE_CAPACITY / 2;
            manualDeliveryKG.choreTypeIDHash = Db.Get().ChoreTypes.MachineFetch.IdHash;
            manualDeliveryKG.operationalRequirement = FetchOrder2.OperationalRequirement.Functional;

            var workable = go.AddOrGet<SmelterWorkable>();
            workable.overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_smelter_kanim") };
            workable.AnimOffset = Vector3.left;

            ConduitConsumer conduitConsumer = go.AddOrGet<ConduitConsumer>();
            conduitConsumer.capacityTag = GameTags.Liquid;
            conduitConsumer.capacityKG = COOLANT_MASS * 2;
            conduitConsumer.storage = lcfr.inStorage;
            conduitConsumer.alwaysConsume = true;
            conduitConsumer.forceAlwaysSatisfied = true;

            var elementConverter = go.AddOrGet<ElementConverter>();
            elementConverter.consumedElements = new ElementConverter.ConsumedElement[]
            {
                new ElementConverter.ConsumedElement(FUEL_TAG, FUEL_CONSUME_RATE)
            };
            elementConverter.outputElements = new ElementConverter.OutputElement[]
            {
                new ElementConverter.OutputElement(CO2_EMIT_RATE, SimHashes.CarbonDioxide, CO2_OUTPUT_TEMPERATURE, false, false, 1f, 2f)
            };

            var smelterWorkableEmpty = go.AddOrGet<SmelterWorkableEmpty>();
            smelterWorkableEmpty.workTime = BUILDINGS.WORK_TIME_SECONDS.SHORT_WORK_TIME;
            smelterWorkableEmpty.workLayer = Grid.SceneLayer.BuildingFront;

            Prioritizable.AddRef(go);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<LogicOperationalController>();
            SymbolOverrideControllerUtil.AddToPrefab(go);
            go.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject gameObject)
            {
                var workable = gameObject.GetComponent<ComplexFabricatorWorkable>();
                workable.WorkerStatusItem = Db.Get().DuplicantStatusItems.Processing;
                workable.AttributeConverter = Db.Get().AttributeConverters.MachinerySpeed;
                workable.AttributeExperienceMultiplier = DUPLICANTSTATS.ATTRIBUTE_LEVELING.PART_DAY_EXPERIENCE;
                workable.SkillExperienceSkillGroup = Db.Get().SkillGroups.Technicals.Id;
                workable.SkillExperienceMultiplier = SKILLS.PART_DAY_EXPERIENCE;
            };
        }

        internal static void ConfigureRecipes()
        {
            const float INPUT_KG = 100f;
            // добавляем переплавку абиссалития в электроплавильню
            if (SmelterOptions.Instance.RecipeKatairiteToTungsten)
            {
                const float PHOSPHORUS = 10f;
                const float SALT = 20f;
                const float TUNGSTEN = INPUT_KG - PHOSPHORUS - SALT;
                const float SALT_TO_CHLORINE_RATIO = 1f / 3f;
                const float CHLORINEGAS = SALT * SALT_TO_CHLORINE_RATIO;
                const float MAGMA = INPUT_KG - TUNGSTEN - CHLORINEGAS;

                var ingredients = new ComplexRecipe.RecipeElement[]
                {
                    new ComplexRecipe.RecipeElement(SimHashes.Katairite.CreateTag(), TUNGSTEN),
                    new ComplexRecipe.RecipeElement(SimHashes.Salt.CreateTag(), SALT),
                    new ComplexRecipe.RecipeElement(SimHashes.Phosphorus.CreateTag(), PHOSPHORUS)
                };
                var results = new ComplexRecipe.RecipeElement[]
                {
                    new ComplexRecipe.RecipeElement(SimHashes.Tungsten.CreateTag(), TUNGSTEN),
                    new ComplexRecipe.RecipeElement(SimHashes.IgneousRock.CreateTag(), MAGMA),
                    new ComplexRecipe.RecipeElement(SimHashes.ChlorineGas.CreateTag(), CHLORINEGAS)
                };
                string obsolete_id = ComplexRecipeManager.MakeObsoleteRecipeID(MetalRefineryConfig.ID, SimHashes.Katairite.CreateTag());
                string id = ComplexRecipeManager.MakeRecipeID(MetalRefineryConfig.ID, ingredients, results);
                new ComplexRecipe(id, ingredients, results)
                {
                    time = BUILDINGS.FABRICATION_TIME_SECONDS.MODERATE,
                    description = string.Format(
                        global::STRINGS.BUILDINGS.PREFABS.METALREFINERY.RECIPE_DESCRIPTION,
                        ElementLoader.FindElementByHash(SimHashes.Tungsten).name,
                        ElementLoader.FindElementByHash(SimHashes.Katairite).name),
                    nameDisplay = ComplexRecipe.RecipeNameDisplay.IngredientToResult,
                    fabricators = new List<Tag> { TagManager.Create(MetalRefineryConfig.ID) }
                };
                ComplexRecipeManager.Get().AddObsoleteIDMapping(obsolete_id, id);
            }

            // добавляем переплавку фосфора в стеклоплавильню
            if (SmelterOptions.Instance.RecipePhosphoriteToPhosphorus)
            {
                var ingredients = new ComplexRecipe.RecipeElement[]
                {
                    new ComplexRecipe.RecipeElement(SimHashes.Phosphorite.CreateTag(), INPUT_KG)
                };
                var results = new ComplexRecipe.RecipeElement[]
                {
                    new ComplexRecipe.RecipeElement(SimHashes.LiquidPhosphorus.CreateTag(), INPUT_KG)
                };
                string obsolete_id = ComplexRecipeManager.MakeObsoleteRecipeID(GlassForgeConfig.ID, SimHashes.Phosphorite.CreateTag());
                string id = ComplexRecipeManager.MakeRecipeID(GlassForgeConfig.ID, ingredients, results);
                new ComplexRecipe(id, ingredients, results)
                {
                    time = BUILDINGS.FABRICATION_TIME_SECONDS.SHORT / 2,
                    description = string.Format(
                        global::STRINGS.BUILDINGS.PREFABS.GLASSFORGE.RECIPE_DESCRIPTION,
                        ElementLoader.FindElementByHash(SimHashes.LiquidPhosphorus).name,
                        ElementLoader.FindElementByHash(SimHashes.Phosphorite).name),
                    nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                    fabricators = new List<Tag> { TagManager.Create(GlassForgeConfig.ID) }
                };
                ComplexRecipeManager.Get().AddObsoleteIDMapping(obsolete_id, id);
            }

            // добавляем копию рецептов из электроплавильни. кроме стали и наёбия
            var metalrefinery_recipes = ComplexRecipeManager.Get().recipes
                .Where((ComplexRecipe recipe) => recipe.fabricators.Contains(TagManager.Create(MetalRefineryConfig.ID)))
                .ToList();
            metalrefinery_recipes
                .DoIf(
                    condition: (ComplexRecipe recipe) => !recipe.id.Contains(SimHashes.Steel.ToString())
                                                      && !recipe.id.Contains(SimHashes.Niobium.ToString()),
                    action: (ComplexRecipe recipe) => recipe.fabricators.Add(TagManager.Create(ID))
                );

            // добавляем сталь с увеличенным временем фабрикации
            const float fabricationTimeMultiplier = 1.3f;

            metalrefinery_recipes
                .Where((ComplexRecipe recipe) => recipe.id.Contains(SimHashes.Steel.ToString()))
                .ToList()
                .Do((ComplexRecipe recipe) =>
                {
                    string obsolete_id = ComplexRecipeManager.MakeObsoleteRecipeID(ID, recipe.ingredients[0].material);
                    string id = ComplexRecipeManager.MakeRecipeID(ID, recipe.ingredients, recipe.results);
                    new ComplexRecipe(id, recipe.ingredients, recipe.results)
                    {
                        time = recipe.time * fabricationTimeMultiplier,
                        description = recipe.description,
                        nameDisplay = recipe.nameDisplay,
                        fabricators = new List<Tag> { TagManager.Create(ID) }
                    };
                    ComplexRecipeManager.Get().AddObsoleteIDMapping(obsolete_id, id);
                });

            // добавляем копию рецептов из стеклоплавильни с увеличенным временем фабрикации
            var glassforge_recipes = ComplexRecipeManager.Get().recipes
                .Where((ComplexRecipe recipe) => recipe.fabricators.Contains(TagManager.Create(GlassForgeConfig.ID)))
                .ToList();
            glassforge_recipes
                .Do((ComplexRecipe recipe) =>
                {
                    var results = new ComplexRecipe.RecipeElement[] {
                        new ComplexRecipe.RecipeElement(ElementLoader.GetElement(recipe.results[0].material).lowTempTransition.tag, recipe.results[0].amount) };
                    string obsolete_id = ComplexRecipeManager.MakeObsoleteRecipeID(ID, recipe.ingredients[0].material);
                    string id = ComplexRecipeManager.MakeRecipeID(ID, recipe.ingredients, results);
                    new ComplexRecipe(id, recipe.ingredients, results)
                    {
                        time = recipe.time * fabricationTimeMultiplier,
                        description = string.Format(global::STRINGS.BUILDINGS.PREFABS.GLASSFORGE.RECIPE_DESCRIPTION, ElementLoader.GetElement(results[0].material).name, ElementLoader.GetElement(recipe.ingredients[0].material).name),
                        nameDisplay = ComplexRecipe.RecipeNameDisplay.IngredientToResult,
                        fabricators = new List<Tag> { TagManager.Create(ID) }
                    };
                    ComplexRecipeManager.Get().AddObsoleteIDMapping(obsolete_id, id);
                });

            // добавляем переплавку  пластика
            if (SmelterOptions.Instance.RecipePlasticToNaphtha)
            {
                var ingredients = new ComplexRecipe.RecipeElement[]
                {
                    new ComplexRecipe.RecipeElement(SimHashes.Polypropylene.CreateTag(), INPUT_KG)
                };
                var results = new ComplexRecipe.RecipeElement[]
                {
                    new ComplexRecipe.RecipeElement(SimHashes.Naphtha.CreateTag(), INPUT_KG)
                };
                string obsolete_id = ComplexRecipeManager.MakeObsoleteRecipeID(ID, SimHashes.Polypropylene.CreateTag());
                string id = ComplexRecipeManager.MakeRecipeID(ID, ingredients, results);
                new ComplexRecipe(id, ingredients, results)
                {
                    time = BUILDINGS.FABRICATION_TIME_SECONDS.SHORT,
                    description = string.Format(
                        global::STRINGS.BUILDINGS.PREFABS.GLASSFORGE.RECIPE_DESCRIPTION,
                        ElementLoader.FindElementByHash(SimHashes.Naphtha).name,
                        ElementLoader.FindElementByHash(SimHashes.Polypropylene).name),
                    nameDisplay = ComplexRecipe.RecipeNameDisplay.IngredientToResult,
                    fabricators = new List<Tag> { TagManager.Create(ID) }
                };
                ComplexRecipeManager.Get().AddObsoleteIDMapping(obsolete_id, id);
            }

            // добавляем древесный уголь в печку
            if (SmelterOptions.Instance.RecipeWoodToCarbon)
            {
                const float WOOD = 200f;
                const float CARBON = 100f;
                const float CO2 = 60f;

                var ingredients = new ComplexRecipe.RecipeElement[]
                {
                    new ComplexRecipe.RecipeElement(WoodLogConfig.TAG, WOOD)
                };
                var results = new ComplexRecipe.RecipeElement[]
                {
                    new ComplexRecipe.RecipeElement(SimHashes.RefinedCarbon.CreateTag(), CARBON),
                    new ComplexRecipe.RecipeElement(SimHashes.CarbonDioxide.CreateTag(), CO2)
                };
                string obsolete_id = ComplexRecipeManager.MakeObsoleteRecipeID(KilnConfig.ID, WoodLogConfig.TAG);
                string id = ComplexRecipeManager.MakeRecipeID(KilnConfig.ID, ingredients, results);
                new ComplexRecipe(id, ingredients, results)
                {
                    time = BUILDINGS.FABRICATION_TIME_SECONDS.SHORT,
                    description = string.Format(
                        global::STRINGS.BUILDINGS.PREFABS.EGGCRACKER.RECIPE_DESCRIPTION,
                        global::STRINGS.UI.FormatAsLink(global::STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.WOOD.NAME, ForestTreeConfig.ID.ToUpperInvariant()),
                        ElementLoader.FindElementByHash(SimHashes.RefinedCarbon).name),
                    nameDisplay = ComplexRecipe.RecipeNameDisplay.IngredientToResult,
                    fabricators = new List<Tag> { TagManager.Create(KilnConfig.ID) }
                };
                ComplexRecipeManager.Get().AddObsoleteIDMapping(obsolete_id, id);
            }
        }
    }
}
