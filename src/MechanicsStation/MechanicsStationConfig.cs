using System.Collections.Generic;
using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;
using static MechanicsStation.MechanicsStationAssets;

namespace MechanicsStation
{
    public class MechanicsStationConfig : IBuildingConfig
    {
        public const string ID = "MechanicsStation";
        public static readonly Tag MATERIAL_FOR_TINKER = GameTags.RefinedMetal;
        public static readonly Tag TINKER_TOOLS = MachinePartsConfig.TAG;
        public const float MASS_PER_TINKER = 5f;
        public const float OUTPUT_TEMPERATURE = 308.15f;

        public override string[] GetRequiredDlcIds() => Utils.GetDlcIds(base.GetRequiredDlcIds());

        public override BuildingDef CreateBuildingDef()
        {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 2,
                height: 2,
                anim: "mechanicstation_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2,
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,
                construction_materials: MATERIALS.ALL_METALS,
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.NONE,
                noise: NOISE_POLLUTION.NOISY.TIER1);
            buildingDef.ViewMode = OverlayModes.Rooms.ID;
            buildingDef.Overheatable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "large";
            buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<LoopingSounds>();
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.MachineShopType, false);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<LogicOperationalController>();
            var storage = go.AddOrGet<Storage>();
            storage.capacityKg = 50f;
            storage.showInUI = true;
            storage.storageFilters = new List<Tag> { MATERIAL_FOR_TINKER };
            var tinkerStation = go.AddOrGet<TinkerStation>();
            tinkerStation.overrideAnims = new KAnimFile[1] { Assets.GetAnim("anim_interacts_craftingstation_kanim") };
            tinkerStation.inputMaterial = MATERIAL_FOR_TINKER;
            tinkerStation.massPerTinker = MASS_PER_TINKER;
            tinkerStation.outputPrefab = TINKER_TOOLS;
            tinkerStation.outputTemperature = OUTPUT_TEMPERATURE;
            tinkerStation.requiredSkillPerk = REQUIRED_ROLE_PERK;
            tinkerStation.choreType = Db.Get().ChoreTypes.MachineTinker.IdHash;
            tinkerStation.fetchChoreType = Db.Get().ChoreTypes.MachineFetch.IdHash;
            tinkerStation.useFilteredStorage = true;
            tinkerStation.toolProductionTime = BUILDINGS.WORK_TIME_SECONDS.SHORT_WORK_TIME;
            var roomTracker = go.AddOrGet<RoomTracker>();
            roomTracker.requiredRoomType = Db.Get().RoomTypes.MachineShop.Id;
            roomTracker.requirement = RoomTracker.Requirement.Recommended;
            Prioritizable.AddRef(go);
            go.GetComponent<KPrefabID>().prefabInitFn += delegate (GameObject gameObject)
            {
                if (gameObject.TryGetComponent<TinkerStation>(out var station))
                {
                    station.AttributeConverter = Db.Get().AttributeConverters.MachinerySpeed;
                    station.AttributeExperienceMultiplier = DUPLICANTSTATS.ATTRIBUTE_LEVELING.MOST_DAY_EXPERIENCE;
                    station.SkillExperienceSkillGroup = Db.Get().SkillGroups.Technicals.Id;
                    station.SkillExperienceMultiplier = SKILLS.MOST_DAY_EXPERIENCE;
                    station.toolProductionTime = BUILDINGS.WORK_TIME_SECONDS.SHORT_WORK_TIME;
                }
            };
            SymbolOverrideControllerUtil.AddToPrefab(go);
            go.AddOrGet<MechanicsStation>();
        }
    }
}
