using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace TravelTubesExpanded
{
    public class TravelTubeCrossBridgeConfig : TravelTubeWallBridgeConfig
    {
        public new const string ID = "TravelTubeCrossBridge";

        public override string[] GetRequiredDlcIds() => Utils.GetDlcIds(base.GetRequiredDlcIds());

        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 1,
                height: 1,
                anim: "tube_tile_cross_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER2,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER0,
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER2,
                construction_materials: MATERIALS.PLASTICS,
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.Tile,
                decor: BUILDINGS.DECOR.PENALTY.TIER0,
                noise: NOISE_POLLUTION.NONE);
            BuildingTemplates.CreateFoundationTileDef(def);
            def.UseStructureTemperature = false;
            def.Overheatable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.ObjectLayer = ObjectLayer.Building;
            def.AudioCategory = "Plastic";
            def.AudioSize = "small";
            def.BaseTimeUntilRepair = -1f;
            def.PermittedRotations = PermittedRotations.Unrotatable;
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
            occupier.notifyOnMelt = true;
            go.AddOrGet<BuildingHP>().destroyOnDamaged = true;
            go.AddOrGet<TileTemperature>();
            go.AddOrGet<TravelTubeBridge>();
            go.AddOrGet<OccupyArea>().objectLayers = new ObjectLayer[] { ObjectLayer.TravelTubeConnection };
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            base.DoPostConfigureComplete(go);
            foreach (var link in go.GetComponents<TravelTubeUtilityNetworkLink>())
                link.visualizeOnly = false;
        }

        public override TravelTubeUtilityNetworkLink AddNetworkLink(GameObject go)
        {
            var first = go.AddOrGet<TravelTubeUtilityNetworkLink>();
            first.link1 = new CellOffset(0, -1);
            first.link2 = new CellOffset(0, 1);
            first.visualizeOnly = true;
            var second = go.AddComponent<TravelTubeUtilityNetworkLink>();
            second.link1 = new CellOffset(-1, 0);
            second.link2 = new CellOffset(1, 0);
            second.visualizeOnly = true;
            return first;
        }
    }
}
