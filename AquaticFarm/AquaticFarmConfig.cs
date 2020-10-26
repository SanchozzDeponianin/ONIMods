using TUNING;
using UnityEngine;


namespace AquaticFarm
{
    public class AquaticFarmConfig : IBuildingConfig
    {
        public const string ID = "AquaticFarm";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID, 
                width: 1, 
                height: 1, 
                anim: "farmtileaquaticrotating_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER2, 
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2, 
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER1[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER1[0] }, 
                construction_materials: new string[] { "Metal", "Farmable" }, 
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1, 
                build_location_rule: BuildLocationRule.Tile, 
                decor: BUILDINGS.DECOR.NONE, 
                noise: NOISE_POLLUTION.NONE
                );
            BuildingTemplates.CreateFoundationTileDef(buildingDef);
            buildingDef.Floodable = false;
            buildingDef.Entombable = false;
            buildingDef.Overheatable = false;
            buildingDef.ForegroundLayer = Grid.SceneLayer.BuildingBack;
            buildingDef.AudioCategory = "HollowMetal";
            buildingDef.AudioSize = "small";
            buildingDef.BaseTimeUntilRepair = -1f;
            buildingDef.SceneLayer = Grid.SceneLayer.TileMain;
            buildingDef.ConstructionOffsetFilter = BuildingDef.ConstructionOffsetFilter_OneDown;
            buildingDef.PermittedRotations = PermittedRotations.FlipV;
            buildingDef.isSolidTile = true;
            buildingDef.DragBuild = true;
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
            SimCellOccupier simCellOccupier = go.AddOrGet<SimCellOccupier>();
            simCellOccupier.doReplaceElement = true;
            go.AddOrGet<TileTemperature>();
            Storage storage = BuildingTemplates.CreateDefaultStorage(go, false);
            storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            PlantablePlot plantablePlot = go.AddOrGet<PlantablePlot>();
            plantablePlot.occupyingObjectRelativePosition = new Vector3(0f, 1f);
            plantablePlot.AddDepositTag(GameTags.CropSeed);
            plantablePlot.AddDepositTag(GameTags.WaterSeed);
            plantablePlot.SetFertilizationFlags(true, true);
            CopyBuildingSettings copyBuildingSettings = go.AddOrGet<CopyBuildingSettings>();
            copyBuildingSettings.copyGroupTag = GameTags.Farm;
            go.AddOrGet<AnimTileable>();
            go.AddOrGet<DropAllWorkable>();
            Prioritizable.AddRef(go);

            go.AddOrGet<AquaticFarm>();
            AddPassiveElementConsumer(go, new Vector3(0f,  1f));
            AddPassiveElementConsumer(go, new Vector3(0f, -1f));
            AddPassiveElementConsumer(go, new Vector3(1f,  0f));
            AddPassiveElementConsumer(go, new Vector3(-1f, 0f));
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            GeneratedBuildings.RemoveLoopingSounds(go);
            go.GetComponent<KPrefabID>().AddTag(GameTags.FarmTiles, false);
            FarmTileConfig.SetUpFarmPlotTags(go);
        }

        private PassiveElementConsumer AddPassiveElementConsumer(GameObject go, Vector3 sampleCellOffset)
        {
            PassiveElementConsumer elementConsumer = go.AddComponent<PassiveElementConsumer>();
            elementConsumer.elementToConsume = SimHashes.Vacuum;
            elementConsumer.consumptionRate = 0f;
            elementConsumer.consumptionRadius = 1;
            elementConsumer.capacityKG = 5f;
            elementConsumer.storeOnConsume = true;
            elementConsumer.sampleCellOffset = sampleCellOffset;
            elementConsumer.showDescriptor = false;
            elementConsumer.showInStatusPanel = false;
            elementConsumer.EnableConsumption(false);
            return elementConsumer;
        }
    }
}
