using System.Collections.Generic;
using UnityEngine;
using TUNING;
using SanchozzONIMods.Lib;

namespace ButcherStation
{
    public class FishingStationConfig : IBuildingConfig
    {
        public const string ID = "FishingStation";
        public const string ANIM = "fishingstation_kanim";

        public override string[] GetRequiredDlcIds() => Utils.GetDlcIds(base.GetRequiredDlcIds());

        public override BuildingDef CreateBuildingDef()
        {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 3,
                height: 2,
                anim: ANIM,
                hitpoints: BUILDINGS.HITPOINTS.TIER2,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER1,
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER4[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER1[0] },
                construction_materials: new string[] { "Metal", "Algae" },
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.OnBackWall,
                decor: BUILDINGS.DECOR.NONE,
                noise: NOISE_POLLUTION.NOISY.TIER1
                );
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER2;
            buildingDef.ExhaustKilowattsWhenActive = BUILDINGS.EXHAUST_ENERGY_ACTIVE.TIER1;
            buildingDef.SelfHeatKilowattsWhenActive = BUILDINGS.SELF_HEAT_KILOWATTS.TIER1;
            buildingDef.Floodable = false;
            buildingDef.Entombable = true;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "large";
            buildingDef.UtilityInputOffset = new CellOffset(0, 0);
            buildingDef.UtilityOutputOffset = new CellOffset(0, 0);
            buildingDef.DefaultAnimState = "off";
            buildingDef.SceneLayer = Grid.SceneLayer.BuildingBack;
            buildingDef.ForegroundLayer = Grid.SceneLayer.Front;
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
            storage.storageFilters = new List<Tag> { Patches.FisherableCreature };
            storage.allowSettingOnlyFetchMarkedItems = false;
            go.AddOrGet<TreeFilterable>().uiHeight = TreeFilterable.UISideScreenHeight.Short;
            var butcherStation = go.AddOrGet<ButcherStation>();
            butcherStation.creatureEligibleTag = Patches.FisherableCreature;
            butcherStation.allowLeaveAlive = true;
            go.AddOrGet<LoopingSounds>();
            go.AddOrGet<BuildingComplete>().isManuallyOperated = true;
            var roomTracker = go.AddOrGet<RoomTracker>();
            roomTracker.requiredRoomType = Db.Get().RoomTypes.CreaturePen.Id;
            roomTracker.requirement = RoomTracker.Requirement.TrackingOnly;
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            //go.AddOrGet<FishingStationGuide>().type = FishingStationGuide.GuideType.Preview;
            AddVisualizer(go);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            go.AddOrGet<FishingStationGuide>().type = FishingStationGuide.GuideType.UnderConstruction;
            AddVisualizer(go);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            var work_time = Utils.GetAnimDuration(Assets.GetAnim(ANIM), "working_pre", "working_loop");
            var def = go.AddOrGetDef<RanchStation.Def>();
            def.IsCritterEligibleToBeRanchedCb = ButcherStation.IsCreatureEligibleToBeButchedCB;
            def.OnRanchCompleteCb = (creature_go, worker) => ButcherStation.ButchCreature(creature_go, worker, true);
            def.GetTargetRanchCell = (smi) =>
            {
                if (!smi.IsNullOrStopped() && smi.gameObject.TryGetComponent<FishingStationGuide>(out var fishingStation))
                    return fishingStation.TargetRanchCell;
                return Grid.InvalidCell;
            };
            def.RancherInteractAnim = "anim_interacts_fishingstation_kanim";
            def.RancherWipesBrowAnim = false;
            def.RanchedPreAnim = "bitehook";
            def.RanchedLoopAnim = "caught_loop";
            def.RanchedPstAnim = "flop_loop";
            def.RequiresRoom = false;
            def.WorkTime = work_time;
            go.AddOrGet<SkillPerkMissingComplainer>().requiredSkillPerk = Db.Get().SkillPerks.CanWrangleCreatures.Id;
            Prioritizable.AddRef(go);
            go.AddOrGet<FishingStationGuide>().type = FishingStationGuide.GuideType.Complete;
            go.AddOrGet<FisherWorkable>().workOffset = CellOffset.up;
            go.AddOrGet<MakeFakeBaseSolid>();
            go.AddOrGet<HalfFloodable>();
            AddVisualizer(go);
            // Начиная с У52, BuildingDef.PostProcess() перезаписывает ProperName тэгов из MaterialCategory
            // вернём водоросли как было
            var algae = ElementLoader.GetElement(GameTags.Algae);
            TagManager.Create(algae.tag.ToString(), algae.name);
        }

        private static void AddVisualizer(GameObject go)
        {
            var visualizer = go.AddOrGet<RangeVisualizer>();
            visualizer.OriginOffset = new Vector2I(0, -1);
            visualizer.RangeMin.x = 0;
            visualizer.RangeMin.y = -FishingStationGuide.MaxDepth;
            visualizer.RangeMax.x = 0;
            visualizer.RangeMax.y = -FishingStationGuide.MinDepth;
            go.GetComponent<KPrefabID>().instantiateFn += gmo =>
                gmo.GetComponent<RangeVisualizer>().BlockingCb = FishingStationGuide.IsCellBlockedCB;
        }
    }
}
