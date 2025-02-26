using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace TravelTubesExpanded
{
    public class TravelTubeFirePoleBridgeConfig : TravelTubeWallBridgeConfig
    {
        public new const string ID = "TravelTubeFirePoleBridge";

        public override string[] GetRequiredDlcIds() => Utils.GetDlcIds(base.GetRequiredDlcIds());

        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 1,
                height: 1,
                anim: "tube_tile_firepole_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER2,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER1,
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER1[0] },
                construction_materials: new string[] { MATERIALS.ALL_METALS[0], MATERIALS.PLASTICS[0] },
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.Anywhere,
                decor: BUILDINGS.DECOR.PENALTY.TIER0,
                noise: NOISE_POLLUTION.NONE);
            BuildingTemplates.CreateLadderDef(def);
            def.Overheatable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.ObjectLayer = ObjectLayer.Building;
            def.AudioCategory = "Metal";
            def.AudioSize = "small";
            def.BaseTimeUntilRepair = -1f;
            def.UtilityInputOffset = new CellOffset(0, 0);
            def.UtilityOutputOffset = new CellOffset(0, 2);
            def.SceneLayer = Grid.SceneLayer.BuildingFront;
            def.ForegroundLayer = Grid.SceneLayer.BuildingFront;
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            var ladder = go.AddOrGet<Ladder>();
            ladder.isPole = true;
            ladder.upwardsMovementSpeedMultiplier = 0.25f;
            ladder.downwardsMovementSpeedMultiplier = 4f;
            go.AddOrGet<AnimTileable>();
            go.AddOrGet<BuildingHP>().destroyOnDamaged = false;
            go.AddOrGet<TravelTubeBridge>();
            go.AddOrGet<OccupyArea>().objectLayers = new ObjectLayer[] { ObjectLayer.FoundationTile };
        }

        public override void ConfigurePost(BuildingDef def)
        {
            var tags = new Tag[] { FirePoleConfig.ID, ID };
            def.BuildingComplete.AddOrGet<AnimTileable>().tags = tags;
            Assets.GetBuildingDef(FirePoleConfig.ID).BuildingComplete.AddOrGet<AnimTileable>().tags = tags;
        }
    }
}
