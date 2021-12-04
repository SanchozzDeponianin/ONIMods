using TUNING;
using UnityEngine;

namespace LargeTelescope
{
    public class ClusterLargeTelescopeConfig : IBuildingConfig
    {
        public const string ID = "ClusterLargeTelescope";
        public const float OXYGEN_CAPACITY = 10f;

        public override string[] GetDlcIds()
        {
            return DlcManager.AVAILABLE_EXPANSION1_ONLY;
        }

        public override BuildingDef CreateBuildingDef()
        {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 4,
                height: 6,
                anim: "telescope_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2,
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER4[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER3[0] },
                construction_materials: new string[] { MATERIALS.ALL_METALS[0], MATERIALS.GLASSES[0] },
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.NONE,
                noise: NOISE_POLLUTION.NOISY.TIER1
                );
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER4;
            buildingDef.ExhaustKilowattsWhenActive = BUILDINGS.EXHAUST_ENERGY_ACTIVE.TIER1;
            buildingDef.SelfHeatKilowattsWhenActive = BUILDINGS.SELF_HEAT_KILOWATTS.TIER0;
            buildingDef.InputConduitType = ConduitType.Gas;
            buildingDef.UtilityInputOffset = new CellOffset(0, 0);
            buildingDef.ViewMode = OverlayModes.Power.ID;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "large";
            buildingDef.Deprecated = true;
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<KPrefabID>().AddTag(GameTags.NotRocketInteriorBuilding);
            go.AddOrGet<BuildingComplete>().isManuallyOperated = true;
            Prioritizable.AddRef(go);
            go.AddOrGetDef<PoweredController.Def>();
            go.AddOrGet<ClusterLargeTelescopeWorkable>().efficiencyMultiplier = 1f + (LargeTelescopeOptions.Instance.efficiency_multiplier / 100f);
            var def = go.AddOrGetDef<ClusterTelescope.Def>();
            def.clearScanCellRadius = 6;
            def.analyzeClusterRadius = LargeTelescopeOptions.Instance.analyze_cluster_radius;
            def.workableOverrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_telescope_kanim") };
            def.providesOxygen = true;
            var storage = go.AddOrGet<Storage>();
            storage.capacityKg = 1000f;
            storage.showInUI = true;
            var conduitConsumer = go.AddOrGet<ConduitConsumer>();
            conduitConsumer.conduitType = ConduitType.Gas;
            conduitConsumer.consumptionRate = ConduitFlow.MAX_GAS_MASS;
            conduitConsumer.capacityTag = GameTags.Oxygen;
            conduitConsumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            conduitConsumer.capacityKG = OXYGEN_CAPACITY;
            conduitConsumer.forceAlwaysSatisfied = true;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            if (LargeTelescopeOptions.Instance.not_require_gas_pipe)
                go.GetComponent<RequireInputs>().SetRequirements(true, false);
        }
    }
}
