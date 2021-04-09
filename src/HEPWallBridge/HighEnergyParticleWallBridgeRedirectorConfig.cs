using System.Collections.Generic;
using TUNING;
using UnityEngine;
using static STRINGS.BUILDINGS.PREFABS.HIGHENERGYPARTICLEREDIRECTOR;

namespace HEPWallBridge
{
    public class HighEnergyParticleWallBridgeRedirectorConfig : IBuildingConfig
    {
        public const string ID = "HighEnergyParticleWallBridgeRedirector";

        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 3,
                height: 1,
                anim: "wallbridge_orb_transporter_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER1,
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER4[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0] },
                construction_materials: new string[] { MATERIALS.BUILDABLERAW, MATERIALS.METAL },
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.NotInTiles,
                decor: BUILDINGS.DECOR.PENALTY.TIER1,
                noise: NOISE_POLLUTION.NONE);
            def.RequiredDlcId = DlcManager.EXPANSION1_ID;
            def.Floodable = false;
            def.AudioCategory = "Metal";
            def.Overheatable = false;
            def.PermittedRotations = PermittedRotations.R360;
            def.ViewMode = OverlayModes.Radiation.ID;
            def.UseHighEnergyParticleInputPort = true;
            def.HighEnergyParticleInputOffset = new CellOffset(-1, 0);
            def.UseHighEnergyParticleOutputPort = true;
            def.HighEnergyParticleOutputOffset = new CellOffset(1, 0);
            def.RequiresPowerInput = true;
            def.PowerInputOffset = new CellOffset(-1, 0);
            def.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER3;
            def.ExhaustKilowattsWhenActive = BUILDINGS.EXHAUST_ENERGY_ACTIVE.TIER4;
            def.SelfHeatKilowattsWhenActive = BUILDINGS.SELF_HEAT_KILOWATTS.TIER4;
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
            var storage = go.AddOrGet<HighEnergyParticleStorage>();
            storage.autoStore = true;
            storage.showInUI = false;
            storage.capacity = HighEnergyParticleConfig.MAX_PAYLOAD + 1f;
            var redirector = go.AddOrGet<HighEnergyParticleRedirector>();
            redirector.directorDelay = HighEnergyParticleRedirectorConfig.TRAVEL_DELAY;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGetDef<MakeBaseSolid.Def>();
        }
    }
}
