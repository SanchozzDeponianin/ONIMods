using System.Collections.Generic;
using UnityEngine;
using STRINGS;
using SanchozzONIMods.Lib;

namespace CrabsProfit
{
    using static STRINGS.ITEMS.INDUSTRIAL_PRODUCTS;
    public class CrabFreshWaterShellConfig : IEntityConfig, IHasDlcRestrictions
    {
        public const string ID = "CrabFreshWaterShell";
        public static readonly Tag TAG = TagManager.Create(ID, CRAB_SHELL.VARIANT_FRESH_WATER.NAME);
        public string[] GetRequiredDlcIds() => Utils.GetDlcIds();
        public string[] GetForbiddenDlcIds() => null;
        public const SimHashes NonRandomCrushedTo = SimHashes.Corallium;
        public const SimHashes SecondaryCrushedTo = SimHashes.Pearl;

        public GameObject CreatePrefab()
        {
            var mass = ModOptions.Instance.AdultShellMass;
            var second_mass = ModOptions.Instance.SecondaryMass;
            var second = SecondaryCrushedTo.CreateTag();
            var ore_mass = mass - second_mass;
            var ore = ModOptions.Instance.DisableRandom ? NonRandomCrushedTo.CreateTag() : RandomOreConfig.ID;
            AddRecipe(TAG, mass, ore, ore_mass, second, second_mass);

            var desk = (ore_mass > 0f && second_mass > 0f) ?
                string.Format(CRAB_SHELL.VARIANT_FRESH_WATER.DESC_TWO, ore.ProperName(), second.ProperName()) :
                string.Format(CRAB_SHELL.VARIANT_FRESH_WATER.DESC, ore_mass > 0f ? ore.ProperName() : second.ProperName());

            var go = EntityTemplates.CreateLooseEntity(
                id: ID,
                name: CRAB_SHELL.VARIANT_FRESH_WATER.NAME,
                desc: desk,
                mass: 1f,
                unitMass: false,
                anim: Assets.GetAnim("fresh_crabshell_kanim"),
                initialAnim: "object",
                sceneLayer: Grid.SceneLayer.Front,
                collisionShape: EntityTemplates.CollisionShape.RECTANGLE,
                width: 0.9f,
                height: 0.6f,
                isPickupable: true,
                sortOrder: 0,
                element: SimHashes.Creature,
                additionalTags: new List<Tag> { GameTags.Organics, GameTags.MoltShell });
            go.AddOrGet<EntitySplitter>();
            go.AddOrGet<SimpleMassStatusItem>();
            // размер шкорлупы:
            var id = (OreSizeVisualizerComponents.TiersSetType)Hash.SDBMLower(ID);
            go.AddOrGet<EntitySizeVisualizer>().TierSetType = id;
            var mass_tiers = new OreSizeVisualizerComponents.MassTier[]
            {
                new OreSizeVisualizerComponents.MassTier
                {
                    animName = "idle1",
                    massRequired = 1.5f * ModOptions.Instance.BabyShellMass,
                    colliderRadius = 0.15f
                },
                new OreSizeVisualizerComponents.MassTier
                {
                    animName = "idle2",
                    massRequired = 1.5f * ModOptions.Instance.AdultShellMass,
                    colliderRadius = 0.2f
                },
                new OreSizeVisualizerComponents.MassTier
                {
                    animName = "idle3",
                    massRequired = float.MaxValue,
                    colliderRadius = 0.25f
                }
            };
            OreSizeVisualizerComponents.TierSets[id] = mass_tiers;
            EntityTemplates.CreateAndRegisterCompostableFromPrefab(go);
            return go;
        }

        public void OnPrefabInit(GameObject inst)
        {
            // скопипизжено из других шкорлуп, наверно для правильной загрузки сейфоф до У56
            if (inst.TryGetComponent(out Compostable compostable))
                compostable.OnDeserializeCb = (KMonoBehaviour kmb) =>
            {
                if (SaveLoader.Instance.GameInfo.IsVersionOlderThan(7, 36))
                {
                    if (kmb.TryGetComponent(out PrimaryElement pe))
                    {
                        pe.MassPerUnit = 1f;
                        pe.Mass = pe.Units * ModOptions.Instance.AdultShellMass;
                    }
                    if (kmb.TryGetComponent(out KPrefabID kPrefabID))
                        kPrefabID.RemoveTag(GameTags.IndustrialIngredient);
                }
            };
        }

        public void OnSpawn(GameObject inst) { }

        internal static void AddRecipe(Tag shell, float shell_mass, Tag ore, float ore_mass, Tag second, float second_mass)
        {
            var ingredients = new ComplexRecipe.RecipeElement[] { new ComplexRecipe.RecipeElement(shell, shell_mass) };
            var results = new ComplexRecipe.RecipeElement[0];
            // чтобы id рецепта не менялся при изменении настроек
            var results_for_id = new[] { new ComplexRecipe.RecipeElement(RandomOreConfig.ID, shell_mass) };
            var id = ComplexRecipeManager.MakeRecipeID(RockCrusherConfig.ID, ingredients, results_for_id);
            if (ore_mass > 0f)
                results = results.Append(new ComplexRecipe.RecipeElement(ore, ore_mass));
            if (second_mass > 0)
                results = results.Append(new ComplexRecipe.RecipeElement(second, second_mass));
            new ComplexRecipe(id, ingredients, results)
            {
                time = TUNING.BUILDINGS.FABRICATION_TIME_SECONDS.SHORT,
                description = results.Length > 1 ?
                    string.Format(BUILDINGS.PREFABS.ROCKCRUSHER.RECIPE_DESCRIPTION_TWO_OUTPUT,
                        ingredients[0].material.ProperName(), results[0].material.ProperName(), results[1].material.ProperName()) :
                    string.Format(BUILDINGS.PREFABS.ROCKCRUSHER.RECIPE_DESCRIPTION,
                        ingredients[0].material.ProperName(), results[0].material.ProperName()),
                nameDisplay = ComplexRecipe.RecipeNameDisplay.IngredientToResult,
                fabricators = new List<Tag> { TagManager.Create(RockCrusherConfig.ID) }
            };
        }
    }
}
