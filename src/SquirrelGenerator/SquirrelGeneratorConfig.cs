using UnityEngine;
using TUNING;

namespace SquirrelGenerator
{
    public class SquirrelGeneratorConfig : IBuildingConfig
    {
        public const string ID = "SquirrelGenerator";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 2,
                height: 2,
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER3[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER3[0] },
                construction_materials: new string[] { MATERIALS.METAL, MATERIALS.WOOD },
                noise: NOISE_POLLUTION.NOISY.TIER3,
                anim: "generatormanual_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2,
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.NONE
                );
            buildingDef.GeneratorWattageRating = Config.Get().GeneratorWattageRating;
            buildingDef.GeneratorBaseCapacity = 10000f;
            buildingDef.ViewMode = OverlayModes.Power.ID;
            buildingDef.AudioCategory = "Metal";
            buildingDef.Breakable = true;
            buildingDef.ForegroundLayer = Grid.SceneLayer.BuildingFront;
            buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            buildingDef.SelfHeatKilowattsWhenActive = BUILDINGS.SELF_HEAT_KILOWATTS.TIER2;
            return buildingDef;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<LogicOperationalController>();
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery, false);
            go.AddOrGet<LoopingSounds>();
            Prioritizable.AddRef(go);
            go.AddOrGet<SquirrelGenerator>().powerDistributionOrder = 10;
            var kBatchedAnimController = go.AddOrGet<KBatchedAnimController>();
            kBatchedAnimController.fgLayer = Grid.SceneLayer.BuildingFront;
            kBatchedAnimController.initialAnim = "off";
            Tinkerable.MakePowerTinkerable(go);
        }
    }
}
