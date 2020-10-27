using System.Collections.Generic;
using Klei.AI;
using STRINGS;
using UnityEngine;

namespace DeathReimagined
{
    // скелет дупликанта
    // портит декор, накидывает эмоцию и эффект гнилого трупа 
    // можно раздробить в известь
    public class SkeletonConfig : IEntityConfig
    {
        public const string ID = "Skeleton";
        public static readonly Tag TAG = TagManager.Create(ID);
        public const float MASS = 10f;

        public GameObject CreatePrefab()
        {
            GameObject gameObject = EntityTemplates.CreateLooseEntity(
                id: ID,
                name: STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.SKELETON.NAME,
                desc: STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.SKELETON.DESC,
                mass: MASS,
                unitMass: true,
                anim: Assets.GetAnim("bones_kanim"),
                initialAnim: "object",
                sceneLayer: Grid.SceneLayer.Ore,
                collisionShape: EntityTemplates.CollisionShape.RECTANGLE,
                width: 0.9f,
                height: 0.8f,
                isPickupable: true,
                sortOrder: 0,
                element: SimHashes.Creature,
                additionalTags: new List<Tag> { GameTags.IndustrialIngredient, GameTags.Organics });
            gameObject.AddOrGet<SimpleMassStatusItem>();
            gameObject.AddOrGet<OccupyArea>().OccupiedCellsOffsets = EntityTemplates.GenerateOffsets(1, 1);
            DecorProvider decorProvider = gameObject.AddOrGet<DecorProvider>();
            decorProvider.baseDecor = TUNING.DUPLICANTSTATS.CLOTHING.DECOR_MODIFICATION.BASIC;
            decorProvider.baseRadius = 3f;
            gameObject.AddOrGetDef<EffectLineOfSight.Def>().effectName = DeathPatches.OBSERVED_ROTTEN_CORPSE;
            gameObject.AddOrGet<Skeleton>();
            ConfigureRecipes();
            return gameObject;
        }

        public void OnPrefabInit(GameObject inst)
        {
        }

        public void OnSpawn(GameObject inst)
        {
            Attributes attributes = inst.GetAttributes();
            attributes.Add(Decomposition.rottenDecorModifier);
            attributes.Add(Decomposition.rottenDecorRadiusModifier);
        }

        // передроблевание в известь
        private void ConfigureRecipes()
        {
            ComplexRecipe.RecipeElement[] ingredients = new ComplexRecipe.RecipeElement[1]
            {
                new ComplexRecipe.RecipeElement(ID, 1)
            };
            ComplexRecipe.RecipeElement[] results = new ComplexRecipe.RecipeElement[1]
            {
                new ComplexRecipe.RecipeElement(ElementLoader.FindElementByHash(SimHashes.Lime).tag, MASS)
            };
            string recipeID = ComplexRecipeManager.MakeRecipeID("RockCrusher", ingredients, results);
            new ComplexRecipe(recipeID, ingredients, results)
            {
                time = TUNING.BUILDINGS.FABRICATION_TIME_SECONDS.MODERATE,
                description = string.Format(BUILDINGS.PREFABS.ROCKCRUSHER.LIME_RECIPE_DESCRIPTION, SimHashes.Lime.CreateTag().ProperName(), STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.SKELETON.NAME),
                nameDisplay = ComplexRecipe.RecipeNameDisplay.IngredientToResult,
                fabricators = new List<Tag> { TagManager.Create("RockCrusher") }
            };
        }
    }
}
