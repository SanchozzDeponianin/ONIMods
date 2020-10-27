using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TUNING;
using UnityEngine;

namespace SeismicactiveGeology
{
    public class BurnerConfig : IBuildingConfig
    {
        public static string ID = "Burner";

        public static readonly LogicPorts.Port OUTPUT_PORT = LogicPorts.Port.OutputPort(LogicSwitch.PORT_ID, new CellOffset(0, 0), STRINGS.BUILDINGS.PREFABS.LOGICELEMENTSENSORGAS.LOGIC_PORT, STRINGS.BUILDINGS.PREFABS.LOGICELEMENTSENSORGAS.LOGIC_PORT_ACTIVE, STRINGS.BUILDINGS.PREFABS.LOGICELEMENTSENSORGAS.LOGIC_PORT_INACTIVE, true, false);

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID, 
                width: 1, 
                height: 2, 
                anim: "burner_kanim", 
                hitpoints: BUILDINGS.HITPOINTS.TIER1, 
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2, 
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0] },
                construction_materials: new string[] { "RefinedMetal", "Katairite" },
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER4,
                build_location_rule: BuildLocationRule.BuildingAttachPoint,
                decor: BUILDINGS.DECOR.NONE,
                noise: NOISE_POLLUTION.NONE);
            buildingDef.Overheatable = false;
            buildingDef.Floodable = false;
            buildingDef.Entombable = true;
            buildingDef.SceneLayer = Grid.SceneLayer.BuildingFront;
            buildingDef.ObjectLayer = ObjectLayer.AttachableBuilding;
            buildingDef.AttachmentSlotTag = SeismicactiveGeologyPatches.GeyserTag;
            //buildingDef.ViewMode = OverlayModes.Logic.ID;
            buildingDef.AudioCategory = "Metal";
            buildingDef.SceneLayer = Grid.SceneLayer.Building;
            //SoundEventVolumeCache.instance.AddVolume("world_element_sensor_kanim", "PowerSwitch_on", NOISE_POLLUTION.NOISY.TIER3);
            //SoundEventVolumeCache.instance.AddVolume("world_element_sensor_kanim", "PowerSwitch_off", NOISE_POLLUTION.NOISY.TIER3);
            //GeneratedBuildings.RegisterWithOverlay(OverlayModes.Logic.HighlightItemIDs, ID);
            return buildingDef;
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            GeneratedBuildings.RegisterLogicPorts(go, OUTPUT_PORT);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            GeneratedBuildings.RegisterLogicPorts(go, OUTPUT_PORT);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            GeneratedBuildings.RegisterLogicPorts(go, OUTPUT_PORT);
        }


    }
}
