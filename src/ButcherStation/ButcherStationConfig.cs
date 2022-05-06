using System.Collections.Generic;
using UnityEngine;
using TUNING;

namespace ButcherStation
{
    public class ButcherStationConfig : IBuildingConfig
    {
        public const string ID = "ButcherStation";

        public override BuildingDef CreateBuildingDef()
        {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 3,
                height: 3,
                anim: "butcher_station_kanim",   //  "metalreclaimer_kanim"
                hitpoints: BUILDINGS.HITPOINTS.TIER2,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER1,
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
                construction_materials: MATERIALS.ALL_METALS,
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.PENALTY.TIER4,
                noise: NOISE_POLLUTION.NOISY.TIER1);
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER3;
            buildingDef.ExhaustKilowattsWhenActive = BUILDINGS.EXHAUST_ENERGY_ACTIVE.TIER2;
            buildingDef.SelfHeatKilowattsWhenActive = BUILDINGS.SELF_HEAT_KILOWATTS.TIER2;
            buildingDef.Floodable = true;
            buildingDef.Entombable = true;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "large";
            buildingDef.OverheatTemperature = BUILDINGS.OVERHEAT_TEMPERATURES.HIGH_1;
            buildingDef.UtilityInputOffset = new CellOffset(0, 0);
            buildingDef.UtilityOutputOffset = new CellOffset(0, 0);
            buildingDef.DefaultAnimState = "off";
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            var prefabID = go.GetComponent<KPrefabID>();
            prefabID.AddTag(RoomConstraints.ConstraintTags.CreatureRelocator, false);
            prefabID.AddTag(RoomConstraints.ConstraintTags.RanchStation, false);
            var storage = go.AddOrGet<Storage>();
            storage.allowItemRemoval = false;
            storage.showDescriptor = false;
            storage.storageFilters = new List<Tag> { ButcherStation.ButcherableCreature };
            storage.allowSettingOnlyFetchMarkedItems = false;
            go.AddOrGet<TreeFilterable>();
            var butcherStation = go.AddOrGet<ButcherStation>();
            butcherStation.creatureEligibleTag = ButcherStation.ButcherableCreature;
            go.AddOrGet<LoopingSounds>();
            go.AddOrGet<BuildingComplete>().isManuallyOperated = true;
            var roomTracker = go.AddOrGet<RoomTracker>();
            roomTracker.requiredRoomType = Db.Get().RoomTypes.CreaturePen.Id;
            roomTracker.requirement = RoomTracker.Requirement.Required;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            var def = go.AddOrGetDef<RanchStation.Def>();
            def.isCreatureEligibleToBeRanchedCb = delegate (GameObject creature_go, RanchStation.Instance ranch_station_smi)
            {
                var butcherStation = ranch_station_smi.GetComponent<ButcherStation>();
                return butcherStation?.IsCreatureEligibleToBeButched(creature_go) ?? false;
            };
            def.onRanchCompleteCb = delegate (GameObject creature_go)
            {
                ButcherStation.ButchCreature(creature_go);
            };
            def.getTargetRanchCell = delegate (RanchStation.Instance smi)
            {
                int num = Grid.InvalidCell;
                if (!smi.IsNullOrStopped())
                {
                    num = Grid.PosToCell(smi.transform.GetPosition());
                    var targetRanchable = smi.targetRanchable;
                    if (!targetRanchable.IsNullOrStopped())
                    {
                        if (targetRanchable.HasTag(GameTags.Creatures.Flyer))
                        {
                            num = Grid.CellAbove(num);
                            if (targetRanchable.HasTag(MooConfig.ID))
                            {
                                num = Grid.CellLeft(num);
                            }
                        }
                    }
                }
                return num;
            };
            def.rancherInteractAnim = "anim_interacts_shearingstation_kanim";
            def.ranchedPreAnim = "grooming_pre";
            //def.ranchedLoopAnim = "grooming_loop";
            def.ranchedLoopAnim = "hit";
            def.ranchedPstAnim = "grooming_pst";
            def.worktime = 3f;
            Prioritizable.AddRef(go);
        }

        public override void ConfigurePost(BuildingDef def)
        {
            foreach (var prefab in Assets.GetPrefabsWithComponent<Butcherable>())
            {
                var b = prefab.GetComponent<Butcherable>();
                if (b.drops != null && b.drops.Length > 0)
                    prefab.AddOrGet<ExtraMeatSpawner>();
            }
        }
    }
}
