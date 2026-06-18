using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;

namespace TravelTubesExpanded
{
    public class TravelTubeRibbedFirePoleBridgeConfig : TravelTubeWallBridgeConfig
    {
        public new const string ID = "TravelTubeRibbedFirePoleBridge";

        public override string[] GetRequiredDlcIds() => Utils.GetDlcIds(base.GetRequiredDlcIds());

        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 1,
                height: 1,
                anim: "tube_tile_ribbed_firepole_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER2,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER1,
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0] + BUILDINGS.CONSTRUCTION_MASS_KG.TIER0[0],
                    BUILDINGS.CONSTRUCTION_MASS_KG.TIER0[0] },
                construction_materials: new string[] { MATERIALS.PLASTICS[0], "Steel" },
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
            def.AddSearchTerms(global::STRINGS.SEARCH_TERMS.TRANSPORT);
            def.Deprecated = PPatchTools.GetTypeSafe("RibbedFirePole.RibbedFirePoleConfig", "RibbedFirePole") == null;
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
            go.AddOrGet<OccupyArea>().objectLayers = new ObjectLayer[] { ObjectLayer.TravelTubeConnection };
        }

        public override void ConfigurePost(BuildingDef def)
        {
            var ribbed = Assets.GetBuildingDef("RibbedFirePole");
            if (ribbed != null)
            {
                var ladder = def.BuildingComplete.AddOrGet<Ladder>();
                var ribbed_ladder = ribbed.BuildingComplete.AddOrGet<Ladder>();
                ladder.upwardsMovementSpeedMultiplier = ribbed_ladder.upwardsMovementSpeedMultiplier;
                ladder.downwardsMovementSpeedMultiplier = ribbed_ladder.downwardsMovementSpeedMultiplier;
            }
        }
    }
}
