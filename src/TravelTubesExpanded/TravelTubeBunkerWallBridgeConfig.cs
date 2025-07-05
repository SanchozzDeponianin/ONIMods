using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace TravelTubesExpanded
{
    public class TravelTubeBunkerWallBridgeConfig : TravelTubeWallBridgeConfig
    {
        public new const string ID = "TravelTubeBunkerWallBridge";

        public override string[] GetRequiredDlcIds() => Utils.GetDlcIds(base.GetRequiredDlcIds());

        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 1,
                height: 1,
                anim: "tube_tile_bunker_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER4,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER3,
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER1[0] },
                construction_materials: new string[] { SimHashes.Steel.ToString(), MATERIALS.PLASTICS[0] },
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.Tile,
                decor: BUILDINGS.DECOR.NONE,
                noise: NOISE_POLLUTION.NONE);
            BuildingTemplates.CreateFoundationTileDef(def);
            def.UseStructureTemperature = false;
            def.Overheatable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.ObjectLayer = ObjectLayer.Building;
            def.AudioCategory = "Metal";
            def.AudioSize = "small";
            def.BaseTimeUntilRepair = -1f;
            def.PermittedRotations = PermittedRotations.R90;
            def.UtilityInputOffset = new CellOffset(0, 0);
            def.UtilityOutputOffset = new CellOffset(0, 2);
            def.SceneLayer = Grid.SceneLayer.BuildingFront;
            def.ForegroundLayer = Grid.SceneLayer.TileMain;
            def.AddSearchTerms(global::STRINGS.SEARCH_TERMS.TRANSPORT);
            def.AddSearchTerms(global::STRINGS.SEARCH_TERMS.TILE);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            var occupier = go.AddOrGet<SimCellOccupier>();
            occupier.doReplaceElement = true;
            occupier.movementSpeedMultiplier = DUPLICANTSTATS.MOVEMENT_MODIFIERS.PENALTY_3;
            occupier.strengthMultiplier = 10f;
            occupier.notifyOnMelt = true;
            go.AddOrGet<BuildingHP>().destroyOnDamaged = true;
            go.AddOrGet<TileTemperature>();
            go.AddOrGet<TravelTubeBridge>();
            go.AddOrGet<KPrefabID>().AddTag(GameTags.Bunker);
        }
    }
}
