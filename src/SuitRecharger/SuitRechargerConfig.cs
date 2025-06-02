using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TUNING;
using SanchozzONIMods.Lib;
using SanchozzONIMods.Shared;

namespace SuitRecharger
{
    using static SuitRecharger;
    public class SuitRechargerConfig : IBuildingConfig
    {
        public const string ID = "SuitRecharger";
        public static float O2_CAPACITY { get; private set; } = 200f;
        public static float FUEL_CAPACITY { get; private set; } = 100f;

        private readonly ConduitPortInfo fuelInputPort = new(ConduitType.Liquid, new CellOffset(0, 2));
        private readonly ConduitPortInfo liquidWasteOutputPort = new(ConduitType.Liquid, new CellOffset(0, 0));
        private readonly ConduitPortInfo gasWasteOutputPort = new(ConduitType.Gas, new CellOffset(1, 0));

        public override string[] GetRequiredDlcIds() => Utils.GetDlcIds(base.GetRequiredDlcIds());

        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 2,
                height: 4,
                anim: "suitrecharger_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2,
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,
                construction_materials: MATERIALS.REFINED_METALS,
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.BONUS.TIER1,
                noise: NOISE_POLLUTION.NOISY.TIER0);
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER4;
            def.InputConduitType = ConduitType.Gas;
            def.UtilityInputOffset = new CellOffset(1, 2);
            def.PermittedRotations = PermittedRotations.FlipH;
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.SuitIDs, ID);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            O2_CAPACITY = SuitRechargerOptions.Instance.o2_capacity;
            FUEL_CAPACITY = SuitRechargerOptions.Instance.fuel_capacity;

            var o2_consumer = go.AddOrGet<ConduitConsumer>();
            o2_consumer.conduitType = ConduitType.Gas;
            o2_consumer.consumptionRate = ConduitFlow.MAX_LIQUID_MASS; // для софместимости с модами на увеличение потока в трубах
            o2_consumer.capacityTag = GameTags.Oxygen;
            o2_consumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            o2_consumer.forceAlwaysSatisfied = true;
            o2_consumer.OperatingRequirement = Operational.State.Functional;
            o2_consumer.capacityKG = O2_CAPACITY;

            var o2_storage = go.AddOrGet<Storage>();
            o2_storage.capacityKg = O2_CAPACITY + FUEL_CAPACITY;
            o2_storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            o2_storage.storageID = GameTags.Oxygen;
            go.AddOrGet<StorageDropper>();

            AddManualDeliveryKG(go, GameTags.Oxygen, O2_CAPACITY).SetStorage(o2_storage);
            AddManualDeliveryKG(go, SimHashes.Petroleum.CreateTag(), FUEL_CAPACITY).SetStorage(o2_storage);
            go.GetComponent<KPrefabID>().prefabInitFn += delegate (GameObject inst)
            {
                var mdkgs = inst.GetComponents<ManualDeliveryKG>();
                foreach (var mg in mdkgs)
                {
                    if (mg.allowPause)
                        ManualDeliveryKGPatch.userPaused.Set(mg, true);
                }
            };

            var repair_storage = go.AddComponent<Storage>();
            repair_storage.capacityKg = FUEL_CAPACITY;
            repair_storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            repair_storage.storageID = GameTags.NoOxygen;

            var filterable = go.AddOrGet<FlatTagFilterable>();
            filterable.headerText = STRINGS.UI.UISIDESCREENS.SUITRECHARGERSIDESCREEN.FILTER_CATEGORY;
            filterable.displayOnlyDiscoveredTags = false;

            var treeFilterable = go.AddOrGet<TreeFilterable>();
            treeFilterable.storageToFilterTag = GameTags.NoOxygen;
            treeFilterable.dropIncorrectOnFilterChange = false;
            treeFilterable.filterByStorageCategoriesOnSpawn = false;
            treeFilterable.autoSelectStoredOnLoad = false;
            treeFilterable.uiHeight = TreeFilterable.UISideScreenHeight.Short;

            var recharger = go.AddOrGet<SuitRecharger>();
            recharger.fuelPortInfo = fuelInputPort;
            recharger.liquidWastePortInfo = liquidWasteOutputPort;
            recharger.gasWastePortInfo = gasWasteOutputPort;

            go.AddOrGet<CopyBuildingSettings>();
            go.AddOrGetDef<RocketUsageRestriction.Def>().initialControlledStateWhenBuilt = false;
        }

        private ManualDeliveryKG AddManualDeliveryKG(GameObject go, Tag requestedTag, float capacity, float refill = 0.75f, bool allowPause = true)
        {
            var md = go.AddComponent<ManualDeliveryKG>();
            md.capacity = capacity;
            md.refillMass = refill * capacity;
            md.RequestedItemTag = requestedTag;
            md.choreTypeIDHash = Db.Get().ChoreTypes.MachineFetch.IdHash;
            md.operationalRequirement = Operational.State.Functional;
            md.allowPause = allowPause;
            return md;
        }

        private void AttachPort(GameObject go)
        {
            go.AddComponent<ConduitSecondaryInput>().portInfo = fuelInputPort;
            go.AddComponent<ConduitSecondaryOutput>().portInfo = liquidWasteOutputPort;
            go.AddComponent<ConduitSecondaryOutput>().portInfo = gasWasteOutputPort;
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            AttachPort(go);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            AttachPort(go);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }

        public override void ConfigurePost(BuildingDef def)
        {
            // вытаскиваем стоимость ремонта костюмов из рецептов
            var cost = new List<RepairSuitCost>();
            foreach (var recipe in ComplexRecipeManager.Get().preProcessRecipes)
            {
                var worn = recipe.ingredients[0].material;
                if (worn.IsValid && worn.Name.StartsWith("Worn_"))
                {
                    var suit = recipe.results[0].material;
                    float energy = 0f;
                    if (recipe.fabricators != null && recipe.fabricators.Count > 0)
                    {
                        var fabricator = Assets.GetPrefab(recipe.fabricators[0]);
                        energy = (fabricator.GetComponent<Building>().Def.EnergyConsumptionWhenActive) * recipe.time;
                    }
                    if (recipe.ingredients.Length > 1)
                    {
                        var ingredient = recipe.ingredients[1];
                        foreach (var possible in ingredient.possibleMaterials)
                        {
                            if (possible.IsValid)
                                cost.Add(new() { material = possible, amount = ingredient.amount, energy = energy });
                        }
                    }
                    if (cost.Count == 0 && energy > 0)
                    {
                        cost.Add(new() { energy = energy });
                    }
                    if (!AllRepairSuitCost.ContainsKey(suit))
                        AllRepairSuitCost[suit] = new RepairSuitCost[0];
                    AllRepairSuitCost[suit] = AllRepairSuitCost[suit].Append(cost);
                    cost.Clear();
                }
            }
            // доставкa материалов для ремонта
            const float refill = 0.2f;
            var go = Assets.GetPrefab(ID);
            var storage = go.GetComponents<Storage>().FirstOrDefault(storage => storage.storageID == GameTags.NoOxygen);
            var all_costs = AllRepairSuitCost.Values.SelectMany(cost => cost);
            var all_materials = all_costs.Select(cost => cost.material).Where(tag => tag.IsValid).Distinct().ToList();
            foreach (var material in all_materials)
            {
                if (Assets.TryGetPrefab(material) != null)
                {
                    var amount = all_costs.Where(cost => cost.material == material).Select(cost => cost.amount).Max();
                    AddManualDeliveryKG(go, material, amount / refill, refill, false).SetStorage(storage);
                }
            }
        }
    }
}
