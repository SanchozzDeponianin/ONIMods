using UnityEngine;
using TUNING;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using static STRINGS.SEARCH_TERMS;

namespace SquirrelGenerator
{
    public class SquirrelGeneratorConfig : IBuildingConfig
    {
        public const string ID = "SquirrelGenerator";

        public override string[] GetRequiredDlcIds() => Utils.GetDlcIds(base.GetRequiredDlcIds());

        public override BuildingDef CreateBuildingDef()
        {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 2,
                height: 2,
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER3[0], 1f },
                construction_materials: new string[] { MATERIALS.METAL, GameTags.Seed.ToString() },
                noise: NOISE_POLLUTION.NOISY.TIER3,
                anim: "generatorsquirrel_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2,
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.NONE
                );
            buildingDef.GeneratorWattageRating = ModOptions.Instance.GeneratorWattageRating;
            buildingDef.GeneratorBaseCapacity = 10000f;
            buildingDef.RequiresPowerOutput = true;
            buildingDef.PowerOutputOffset = new CellOffset(0, 0);
            buildingDef.ViewMode = OverlayModes.Power.ID;
            buildingDef.AudioCategory = "Metal";
            buildingDef.Breakable = true;
            buildingDef.ForegroundLayer = Grid.SceneLayer.BuildingFront;
            buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            buildingDef.SelfHeatKilowattsWhenActive = ModOptions.Instance.SelfHeatWatts / Constants.KW2DTU_S;
            PGameUtils.CopySoundsToAnim("generatorsquirrel_kanim", "generatormanual_kanim");
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.POWER);
            buildingDef.AddSearchTerms(GENERATOR);
            buildingDef.AddSearchTerms(CRITTER);
            buildingDef.AddSearchTerms(RANCHING);
            return buildingDef;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<LogicOperationalController>();
            var prefabID = go.GetComponent<KPrefabID>();
            prefabID.AddTag(RoomConstraints.ConstraintTags.GeneratorType, false);
            prefabID.AddTag(RoomConstraints.ConstraintTags.LightDutyGeneratorType, false);
            prefabID.AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery, false);
            go.AddOrGet<LoopingSounds>();
            Prioritizable.AddRef(go);
            go.AddOrGet<SquirrelGenerator>().powerDistributionOrder = 10;
            var kBatchedAnimController = go.AddOrGet<KBatchedAnimController>();
            kBatchedAnimController.fgLayer = Grid.SceneLayer.BuildingFront;
            kBatchedAnimController.initialAnim = "off";
        }
    }
}
