using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using TUNING;
using HarmonyLib;
using SanchozzONIMods.Lib;

namespace ButcherStation
{
    [HarmonyPatch]
    public class FishingStationConfig : IBuildingConfig
    {
        public const string ID = "FishingStation";
        public const string ANIM = "fishingstation_kanim";
        public const string INTERACT_ANIM = "anim_interacts_fishingstation_kanim";

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
                construction_materials: new string[] { "Metal", "Algae&" + MATERIALS.RUBBER_OR_PLASTIC },
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
            buildingDef.ForegroundLayer = Grid.SceneLayer.BuildingFront;
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.CRITTER);
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.RANCHING);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
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
            AddGuide(go, false);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            AddGuide(go, false);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            var work_time = Utils.GetAnimDuration(Assets.GetAnim(INTERACT_ANIM), "working_pre", "working_loop", "working_pst");
            var def = go.AddOrGetDef<RanchStation.Def>();
            def.IsCritterEligibleToBeRanchedCb = ButcherStation.IsCreatureEligibleToBeButchedCB;
            def.OnRanchCompleteCb = ButcherStation.ButchCreature;
            def.GetTargetRanchCell = (smi) =>
            {
                if (!smi.IsNullOrStopped() && smi.gameObject.TryGetComponent<FishingStation>(out var fishingStation))
                    return fishingStation.TargetRanchCell;
                return Grid.InvalidCell;
            };
            def.RancherInteractAnim = INTERACT_ANIM;
            def.RancherWipesBrowAnim = false;
            def.RanchedPreAnim = "idle_loop";  //"bitehook";
            def.RanchedLoopAnim = "idle_loop"; //"caught_loop";
            def.RanchedPstAnim = "flop_loop";
            def.RequiresRoom = false;
            def.WorkTime = work_time;
            go.AddOrGet<SkillPerkMissingComplainer>().requiredSkillPerk = Db.Get().SkillPerks.CanWrangleCreatures.Id;
            Prioritizable.AddRef(go);
            go.AddOrGet<FishingStation>();
            var workable = go.AddOrGet<FisherWorkable>();
            workable.workOffsets = new[] { CellOffset.leftup, CellOffset.rightup };
            workable.faceTargetWhenWorking = true;
            workable.workLayer = Grid.SceneLayer.BuildingUse;
            workable.workAnims = new HashedString[] { "working_pre", "working_loop", "working_pst" };
            workable.workingPstComplete = new HashedString[0];
            workable.workingPstFailed = new HashedString[0];
            workable.workAnimPlayMode = KAnim.PlayMode.Once;
            workable.synchronizeAnims = false;
            workable.resetProgressOnStop = true;
            go.AddOrGet<MakeFakeBaseSolid>().floorOffsets = ModOptions.Instance.make_center_solid
                ? new[] { CellOffset.left, CellOffset.none, CellOffset.right }
                : new[] { CellOffset.left, CellOffset.right };
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
            visualizer.RangeMin.y = 1 - FishingStation.MaxDepth;
            visualizer.RangeMax.x = 0;
            visualizer.RangeMax.y = 1 - FishingStation.MinDepth;
            go.GetComponent<KPrefabID>().instantiateFn += gmo =>
                gmo.GetComponent<RangeVisualizer>().BlockingCb = FishingStation.IsCellBlockedCB;
        }

        [HarmonyReversePatch(HarmonyReversePatchType.Original)]
        [HarmonyPatch(typeof(WaterTrapConfig), nameof(WaterTrapConfig.AddGuide))]
        private static void AddGuide(GameObject go, bool occupy_tiles)
        {
#pragma warning disable CS8321
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Ldstr && instr.operand is string s && s == "critter_trap_water_kanim")
                        instr.operand = ANIM;
                }
                return instructions;
            }
#pragma warning restore CS8321

            WaterTrapConfig.AddGuide(go, occupy_tiles);
        }
    }
}
