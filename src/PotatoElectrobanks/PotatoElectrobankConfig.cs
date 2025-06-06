using System.Collections.Generic;
using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;
using static STRINGS.BUILDINGS.PREFABS;
using static STRINGS.UI;

namespace PotatoElectrobanks
{
    using static STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.ELECTROBANK_POTATO;

    [EntityConfigOrder(3)]
    public class PotatoElectrobankConfig : IMultiEntityConfig
    {
        public const string ID = "PotatoElectrobank_";
        public const string ID_Muckroot = ID + "Muckroot";
        public const string ID_Carrot = ID + "Carrot";
        public const string ID_Sucrose = ID + "Sucrose";
        public const string ID_Shinebug_Egg = ID + "Shinebug_Egg";
        public const string ID_Plugslug_Egg = ID + "Plugslug_Egg";

        public List<GameObject> CreatePrefabs()
        {
            var list = new List<GameObject>();
            if (!DlcManager.IsAllContentSubscribed(Utils.GetDlcIds(DlcManager.DLC3)))
                return list;
            // морковка
            list.Add(CreateElectrobank(ID_Muckroot, BasicForagePlantConfig.ID, 1f,
                "electrobank_muckroot_kanim", DlcManager.DLC3));
            // редиска
            list.Add(CreateElectrobank(ID_Carrot, CarrotConfig.ID, 0.5f,
                "electrobank_plume_squash_kanim", new string[] { DlcManager.DLC3_ID, DlcManager.DLC2_ID }));
            // сахар
            /*
            а) кокос 2000 калорий + сахар 4 кг == варенье 2400 калорий
            следовательно 10 кг == 1000 калорий
            б) тюлень  сахар 30 кг == спирт 40 кг
            редиска спирт 33,7 == 1000 калорий в цикл
            итого 25 кг в цикл
            в) генератор  спирт 2 кг в с == 2 кВт
            бионику надо 200 Вт
            итого 120 кг спирта в цикл
            или 90 кг сахара
            */
            list.Add(CreateElectrobank(ID_Sucrose, "Sucrose", 40f,
                "electrobank_sucrose_kanim", DlcManager.DLC3));
            // светляк
            list.Add(CreateElectrobank(ID_Shinebug_Egg, LightBugConfig.EGG_ID, 5f,
                "electrobank_shinebug_egg_kanim", DlcManager.DLC3));
            // склизьняк
            list.Add(CreateElectrobank(ID_Plugslug_Egg, StaterpillarConfig.EGG_ID, 1f,
                "electrobank_plugslug_egg_kanim", new string[] { DlcManager.DLC3_ID, DlcManager.EXPANSION1_ID }));
            list.RemoveAll(go => go == null);
            return list;
        }

        private GameObject CreateElectrobank(string id, Tag made_from, float amount, string animName,
            string[] requiredDlcIDs = null, string[] forbiddenDlcIds = null, string initialAnim = "object")
        {
            if (!DlcManager.IsCorrectDlcSubscribed(requiredDlcIDs, forbiddenDlcIds))
                return null;
            var prefab = Assets.GetPrefab(made_from);
            float mass = amount * prefab.GetComponent<PrimaryElement>().Mass;
            var name = prefab.GetProperName();
            var template = EntityTemplates.CreateLooseEntity(id,
                string.Format(NAME, StripLinkFormatting(name), ExtractLinkID(name)),
                string.Format(DESC, name),
                ElectrobankConfig.MASS, true, Assets.GetAnim(animName), initialAnim, Grid.SceneLayer.Ore,
                EntityTemplates.CollisionShape.RECTANGLE, 0.5f, 0.8f, true, 0, SimHashes.Creature, new List<Tag>
                {
                    GameTags.ChargedPortableBattery,
                    GameTags.PedestalDisplayable,
                    GameTags.DisposablePortableBattery,
                });
            var potato = template.AddOrGet<PotatoElectrobank>();
            template.AddOrGet<OccupyArea>().SetCellOffsets(EntityTemplates.GenerateOffsets(1, 1));
            template.AddOrGet<DecorProvider>().SetValues(DECOR.PENALTY.TIER0);

            var calories_to_capacity = -ElectrobankConfig.POWER_CAPACITY / DUPLICANTSTATS.STANDARD.BaseStats.CALORIES_BURNED_PER_CYCLE;
            EdiblesManager.FoodInfo rotInfo = null;
            // съедобное
            if (prefab.TryGetComponent(out Edible edible))
            {
                rotInfo = edible.foodInfo;
                potato.maxCapacity = mass * edible.foodInfo.CaloriesPerUnit * calories_to_capacity;
                potato.garbage = RotPileConfig.ID;
                potato.garbageMass = mass;
                potato.keepEmpty = true;
            }
            // для яйц калории от омлета, параметры порчи от сырого
            else if (prefab.HasTag(GameTags.Egg))
            {
                if (Assets.GetPrefab(RawEggConfig.ID).TryGetComponent(out Edible raw))
                    rotInfo = raw.foodInfo;
                if (Assets.GetPrefab(CookedEggConfig.ID).TryGetComponent(out Edible omlet))
                {
                    potato.maxCapacity = mass * (1f - EggShellConfig.EGG_TO_SHELL_RATIO) * omlet.foodInfo.CaloriesPerUnit * calories_to_capacity;
                    potato.garbage = EggShellConfig.ID;
                    potato.garbageMass = mass * EggShellConfig.EGG_TO_SHELL_RATIO;
                    potato.keepEmpty = true;
                }
            }
            // несъедобное
            else
            {
                template.GetComponent<PrimaryElement>().MassPerUnit = ElectrobankConfig.MASS;
            }
            if (rotInfo != null && rotInfo.CanRot)
            {
                var rottable = template.AddOrGetDef<Rottable.Def>();
                rottable.preserveTemperature = rotInfo.PreserveTemperature;
                rottable.rotTemperature = rotInfo.RotTemperature;
                rottable.spoilTime = rotInfo.SpoilTime;
                rottable.staleTime = rotInfo.StaleTime;
            }
            CreateRecipe(made_from, amount, id, requiredDlcIDs);
            potato.charge = potato.maxCapacity;
            return template;
        }

        private void CreateRecipe(Tag input, float amount, Tag output, string[] dlcIds)
        {
            const string fabricator = "CraftingTable";
            if (DlcManager.IsAllContentSubscribed(dlcIds))
            {
                var inputs = new ComplexRecipe.RecipeElement[] { new ComplexRecipe.RecipeElement(input, amount) };
                var outputs = new ComplexRecipe.RecipeElement[] { new ComplexRecipe.RecipeElement(output, 1f,
                    ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature) };
                var id = ComplexRecipeManager.MakeRecipeID(fabricator, inputs, outputs);
                new ComplexRecipe(id, inputs, outputs, dlcIds)
                {
                    time = INDUSTRIAL.RECIPES.STANDARD_FABRICATION_TIME * 2f,
                    description = string.Format(CRAFTINGTABLE.RECIPE_DESCRIPTION, input.ProperName(), output.ProperName()),
                    nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                    fabricators = new List<Tag> { fabricator },
                    sortOrder = -1
                };
            }
        }

        public void OnPrefabInit(GameObject inst) { }
        public void OnSpawn(GameObject inst) { }
    }
}
