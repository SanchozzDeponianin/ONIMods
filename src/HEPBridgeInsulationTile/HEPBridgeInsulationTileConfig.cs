using System.Collections.Generic;
using TUNING;
using HarmonyLib;
using UnityEngine;
using static STRINGS.BUILDINGS.PREFABS.HIGHENERGYPARTICLEREDIRECTOR;

namespace HEPBridgeInsulationTile
{
    public class HEPBridgeInsulationTileConfig : IBuildingConfig
    {
        public const string ID = "HighEnergyParticleWallBridgeRedirector"; // legacy
        public static readonly Tag secodary_material = TagManager.Create(MATERIALS.EXTRUDABLE[0]);

        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 3,
                height: 1,
                anim: "radbolt_joint_plate_insulated_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2,
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER4[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0] },
                construction_materials: new string[] { MATERIALS.BUILDABLERAW, secodary_material.Name },
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.Tile,
                decor: BUILDINGS.DECOR.PENALTY.TIER5,
                noise: NOISE_POLLUTION.NONE);
            def.AudioCategory = "Metal";
            def.AudioSize = "small";
            def.Floodable = false;
            def.Overheatable = false;
            def.Entombable = false;
            def.UseStructureTemperature = false;
            def.ThermalConductivity = 0.01f;
            def.BaseTimeUntilRepair = -1f;
            def.ForegroundLayer = Grid.SceneLayer.TileMain;
            def.PermittedRotations = PermittedRotations.R360;
            def.ViewMode = OverlayModes.Radiation.ID;
            def.UseHighEnergyParticleInputPort = true;
            def.HighEnergyParticleInputOffset = new CellOffset(-1, 0);
            def.UseHighEnergyParticleOutputPort = true;
            def.HighEnergyParticleOutputOffset = new CellOffset(1, 0);
            def.LogicInputPorts = new List<LogicPorts.Port>
            {
                LogicPorts.Port.InputPort(HighEnergyParticleRedirector.PORT_ID, new CellOffset(1, 0), LOGIC_PORT, LOGIC_PORT_ACTIVE, LOGIC_PORT_INACTIVE, false, false)
            };
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.RadiationIDs, ID);
            def.Deprecated = !Sim.IsRadiationEnabled();
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
            Prioritizable.AddRef(go);
            go.AddOrGet<BuildingHP>().destroyOnDamaged = true;
            go.AddOrGet<Insulator>();
            go.AddOrGet<TileTemperature>();
            var storage = go.AddOrGet<HighEnergyParticleStorage>();
            storage.autoStore = true;
            storage.showInUI = false;
            storage.capacity = HighEnergyParticleConfig.MAX_PAYLOAD + 1f;
            go.AddOrGet<HighEnergyParticleRedirector>().directorDelay = HighEnergyParticleRedirectorConfig.TRAVEL_DELAY;
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = HighEnergyParticleRedirectorConfig.ID;
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            go.AddOrGet<BuildingCellVisualizer>();
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            go.AddOrGet<BuildingCellVisualizer>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<BuildingCellVisualizer>();
            go.AddOrGetDef<MakeBaseSolid.Def>().solidOffsets = new CellOffset[] { new CellOffset(0, 0) };
            go.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject inst)
            {
                var buildingComplete = inst.GetComponent<BuildingComplete>();
                if (buildingComplete.creationTime >= GameClock.Instance.GetTime())
                {
                    var rotatable = inst.GetComponent<Rotatable>();
                    var redirector = inst.GetComponent<HighEnergyParticleRedirector>();
                    switch (rotatable.Orientation)
                    {
                        case Orientation.Neutral:
                            redirector.Direction = EightDirection.Right;
                            break;
                        case Orientation.R90:
                            redirector.Direction = EightDirection.Down;
                            break;
                        case Orientation.R180:
                            redirector.Direction = EightDirection.Left;
                            break;
                        case Orientation.R270:
                            redirector.Direction = EightDirection.Up;
                            break;
                    }
                }
            };
        }

        public override void ConfigurePost(BuildingDef def)
        {
            var hashes = new SimHashes[]
            {
                SimHashes.Glass,
                SimHashes.Polypropylene,
                SimHashes.SolidViscoGel,
                SimHashes.SolidResin,
            };
            foreach (var hash in hashes)
            {
                var element = ElementLoader.FindElementByHash(hash);
                element.oreTags = element.oreTags.AddToArray(secodary_material);
            }
        }

        public override string[] GetDlcIds()
        {
            return DlcManager.AVAILABLE_EXPANSION1_ONLY;
        }
    }
}
