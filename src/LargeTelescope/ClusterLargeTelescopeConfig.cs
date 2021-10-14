using TUNING;
using UnityEngine;

namespace LargeTelescope
{
    public class ClusterLargeTelescopeConfig : IBuildingConfig
    {
        public const string ID = "ClusterLargeTelescope";

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
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
                construction_materials: MATERIALS.ALL_METALS,
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.NONE,
                noise: NOISE_POLLUTION.NOISY.TIER1
                );
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER3;
            buildingDef.ExhaustKilowattsWhenActive = BUILDINGS.EXHAUST_ENERGY_ACTIVE.TIER1;
            buildingDef.SelfHeatKilowattsWhenActive = BUILDINGS.SELF_HEAT_KILOWATTS.TIER0;
            buildingDef.InputConduitType = ConduitType.Gas;
            buildingDef.UtilityInputOffset = new CellOffset(0, 0);
            buildingDef.ViewMode = OverlayModes.Power.ID;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "large";
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            // todo: размеры хранилищь уточнить и прописать
            go.AddOrGet<BuildingComplete>().isManuallyOperated = true;
            Prioritizable.AddRef(go);
            var def = go.AddOrGetDef<ClusterTelescope.Def>();
            def.clearScanCellRadius = 5;
            def.analyzeClusterRadius = 4;
            go.GetComponent<KPrefabID>().prefabSpawnFn += OnSpawnFn;
            var storage = go.AddOrGet<Storage>();
            storage.capacityKg = 1000f;
            storage.showInUI = true;
            var conduitConsumer = go.AddOrGet<ConduitConsumer>();
            conduitConsumer.conduitType = ConduitType.Gas;
            conduitConsumer.consumptionRate = ConduitFlow.MAX_GAS_MASS;
            conduitConsumer.capacityTag = ElementLoader.FindElementByHash(SimHashes.Oxygen).tag;
            conduitConsumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            conduitConsumer.capacityKG = 10f;
            conduitConsumer.forceAlwaysSatisfied = true;
            go.AddOrGet<TelescopeGasProvider>();
            go.AddOrGetDef<PoweredController.Def>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }

        private void OnSpawnFn(GameObject go)
        {
            var workable = go.GetComponent<ClusterTelescope.ClusterTelescopeWorkable>();
            workable.overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_telescope_kanim") };
            workable.workLayer = Grid.SceneLayer.BuildingFront;
        }
    }
}
