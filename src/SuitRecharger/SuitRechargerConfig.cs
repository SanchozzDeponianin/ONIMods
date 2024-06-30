using System.Linq;
using UnityEngine;
using TUNING;
using HarmonyLib;
using SanchozzONIMods.Lib;
using SanchozzONIMods.Shared;

namespace SuitRecharger
{
    public class SuitRechargerConfig : IBuildingConfig
    {
        public const string ID = "SuitRecharger";
        public static float O2_CAPACITY { get; private set; } = 200f;
        public static float FUEL_CAPACITY { get; private set; } = 100f;

        private readonly ConduitPortInfo fuelInputPort = new ConduitPortInfo(ConduitType.Liquid, new CellOffset(0, 2));
        private readonly ConduitPortInfo liquidWasteOutputPort = new ConduitPortInfo(ConduitType.Liquid, new CellOffset(0, 0));
        private readonly ConduitPortInfo gasWasteOutputPort = new ConduitPortInfo(ConduitType.Gas, new CellOffset(1, 0));

        public override string[] GetDlcIds() => Utils.GetDlcIds(base.GetDlcIds());

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
            o2_consumer.consumptionRate = ConduitFlow.MAX_GAS_MASS;
            o2_consumer.capacityTag = GameTags.Oxygen;
            o2_consumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            o2_consumer.forceAlwaysSatisfied = true;
            o2_consumer.OperatingRequirement = Operational.State.Functional;
            o2_consumer.capacityKG = O2_CAPACITY;

            var storage = go.AddOrGet<Storage>();
            storage.capacityKg = O2_CAPACITY + FUEL_CAPACITY;
            storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            go.AddOrGet<StorageDropper>();

            AddManualDeliveryKG(go, GameTags.Oxygen, O2_CAPACITY).SetStorage(storage);
            AddManualDeliveryKG(go, SimHashes.Petroleum.CreateTag(), FUEL_CAPACITY).SetStorage(storage);
            go.GetComponent<KPrefabID>().prefabInitFn += delegate (GameObject inst)
            {
                var mdkgs = inst.GetComponents<ManualDeliveryKG>();
                foreach (var mg in mdkgs)
                {
                    if (mg.allowPause)
                        ManualDeliveryKGPatch.userPaused.Set(mg, true);
                }
            };

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
            foreach (var recipe in ComplexRecipeManager.Get().recipes)
            {
                if (recipe.ingredients[0].material.Name.StartsWith("Worn_"))
                {
                    var suit = recipe.results[0].material;
                    var cost = new SuitRecharger.RepairSuitCost();
                    if (recipe.ingredients.Length > 1)
                    {
                        cost.material = recipe.ingredients[1].material;
                        cost.amount = recipe.ingredients[1].amount;
                    }
                    if (recipe.fabricators != null && recipe.fabricators.Count > 0)
                    {
                        var fabricator = Assets.GetPrefab(recipe.fabricators[0]);
                        cost.energy = (fabricator.GetComponent<Building>()?.Def.EnergyConsumptionWhenActive ?? 0f) * recipe.time;
                    }
                    if (!SuitRecharger.repairSuitCost.ContainsKey(suit))
                        SuitRecharger.repairSuitCost[suit] = new SuitRecharger.RepairSuitCost[0];
                    SuitRecharger.repairSuitCost[suit] = SuitRecharger.repairSuitCost[suit].AddToArray(cost);
                }
            }
            // доставкa материалов для ремонта
            const float refill = 0.2f;
            var go = Assets.GetPrefab(ID);
            var storage = go.AddOrGet<Storage>();
            var all_costs = SuitRecharger.repairSuitCost.Values.SelectMany(x => x);
            SuitRecharger.repairMaterials = all_costs.Select(cost => cost.material).Where(tag => tag.IsValid).Distinct().ToList();
            foreach (var material in SuitRecharger.repairMaterials)
            {
                var amount = all_costs.Where(cost => cost.material == material).Select(cost => cost.amount).Max();
                AddManualDeliveryKG(go, material, amount / refill, refill, false).SetStorage(storage);
            }
        }
    }
}
