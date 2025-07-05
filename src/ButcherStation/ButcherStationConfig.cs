﻿using System.Collections.Generic;
using UnityEngine;
using TUNING;
using SanchozzONIMods.Lib;

namespace ButcherStation
{
    public class ButcherStationConfig : IBuildingConfig
    {
        public const string ID = "ButcherStation";

        public override string[] GetRequiredDlcIds() => Utils.GetDlcIds(base.GetRequiredDlcIds());

        public override BuildingDef CreateBuildingDef()
        {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 3,
                height: 3,
                anim: "butcher_station_kanim",
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
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.CRITTER);
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.RANCHING);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.RanchStationType, false);
            var storage = go.AddOrGet<Storage>();
            storage.allowItemRemoval = false;
            storage.showDescriptor = false;
            storage.storageFilters = new List<Tag> { ButcherStation.ButcherableCreature };
            storage.allowSettingOnlyFetchMarkedItems = false;
            go.AddOrGet<TreeFilterable>().uiHeight = TreeFilterable.UISideScreenHeight.Short;
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
            def.IsCritterEligibleToBeRanchedCb = ButcherStation.IsCreatureEligibleToBeButchedCB;
            def.OnRanchCompleteCb = (creature_go, worker) => ButcherStation.ButchCreature(creature_go, worker);
            def.RancherInteractAnim = "anim_interacts_shearingstation_kanim";
            def.RanchedPreAnim = "hit";
            def.RanchedLoopAnim = "hit";
            def.RanchedPstAnim = "idle_loop";
            def.WorkTime = 3f;
            go.AddOrGet<SkillPerkMissingComplainer>().requiredSkillPerk = Db.Get().SkillPerks.CanWrangleCreatures.Id;
            Prioritizable.AddRef(go);
        }
    }
}
