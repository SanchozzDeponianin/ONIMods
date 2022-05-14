using System.Collections.Generic;
using UnityEngine;
using STRINGS;

namespace CrabsProfit
{
    using static STRINGS.ITEMS.INDUSTRIAL_PRODUCTS;
    public class CrabFreshWaterShellConfig : IEntityConfig
    {
        public const string ID = "CrabFreshWaterShell";
        public static readonly Tag TAG = TagManager.Create(ID, CRAB_SHELL.VARIANT_FRESH_WATER.NAME);

        public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;

        public GameObject CreatePrefab()
        {
            var mass = CrabsProfitOptions.Instance.AdultShellMass;
            var go = EntityTemplates.CreateLooseEntity(
                id: ID,
                name: CRAB_SHELL.VARIANT_FRESH_WATER.NAME,
                desc: CRAB_SHELL.VARIANT_FRESH_WATER.DESC,
                mass: mass,
                unitMass: true,
                anim: Assets.GetAnim("fresh_crabshells_large_kanim"),
                initialAnim: "object",
                sceneLayer: Grid.SceneLayer.Front,
                collisionShape: EntityTemplates.CollisionShape.RECTANGLE,
                width: 0.9f,
                height: 0.6f,
                isPickupable: true,
                sortOrder: 0,
                element: SimHashes.Creature,
                additionalTags: new List<Tag> { GameTags.IndustrialIngredient, GameTags.Organics });
            go.AddOrGet<EntitySplitter>();
            go.AddOrGet<SimpleMassStatusItem>();
            EntityTemplates.CreateAndRegisterCompostableFromPrefab(go);
            AddRecipe(ID, mass);
            return go;
        }

        public void OnPrefabInit(GameObject inst) { }
        public void OnSpawn(GameObject inst) { }

        internal static void AddRecipe(string shell_id, float shell_mass)
        {
            var ingredients = new ComplexRecipe.RecipeElement[] { new ComplexRecipe.RecipeElement(shell_id, 1f) };
            var results = new ComplexRecipe.RecipeElement[] { new ComplexRecipe.RecipeElement(CrabsProfitRandomOreConfig.ID, shell_mass) };
            var id = ComplexRecipeManager.MakeRecipeID(RockCrusherConfig.ID, ingredients, results);
            new ComplexRecipe(id, ingredients, results)
            {
                time = TUNING.BUILDINGS.FABRICATION_TIME_SECONDS.SHORT,
                description = string.Format(BUILDINGS.PREFABS.ROCKCRUSHER.LIME_RECIPE_DESCRIPTION, RANDOMORE.NAME, shell_id.ToTag().ProperName()),
                nameDisplay = ComplexRecipe.RecipeNameDisplay.IngredientToResult,
                fabricators = new List<Tag> { TagManager.Create(RockCrusherConfig.ID) }
            };
        }
    }
}
