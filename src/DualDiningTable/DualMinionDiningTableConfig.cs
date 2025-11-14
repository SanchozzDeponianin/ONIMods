using STRINGS;
using TUNING;
using UnityEngine;

namespace DualDiningTable
{
    public class DualMinionDiningTableConfig : IBuildingConfig
    {
        public const string ID = "DualMinionDiningTable";

        public static readonly MultiMinionDiningTableConfig.Seat[] seats = new[]
        {
            new MultiMinionDiningTableConfig.Seat("anim_eat_table_L_kanim", "anim_bionic_eat_table_L_kanim", "saltshaker_L", CellOffset.none),
            new MultiMinionDiningTableConfig.Seat("anim_eat_table_R_kanim", "anim_bionic_eat_table_R_kanim", "saltshaker_R", CellOffset.none)
        };

        // должно быть согласовано с солью на анимации
        public static readonly Vector3[] AnimsOffsets = new[] { new Vector3(-0.25f, 0f), new Vector3(0.25f, 0f) };

        public static int SeatCount => seats.Length;

        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 3,
                height: 1,
                anim: "dual_dupe_table_kanim",
                hitpoints: TUNING.BUILDINGS.HITPOINTS.TIER0,
                construction_time: TUNING.BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER1,
                construction_mass: TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,
                construction_materials: MATERIALS.PLASTICS,
                melting_point: TUNING.BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: TUNING.BUILDINGS.DECOR.BONUS.TIER2,
                noise: NOISE_POLLUTION.NONE);
            def.WorkTime = 20f;
            def.Overheatable = false;
            def.AudioCategory = "Metal";
            def.AddSearchTerms(SEARCH_TERMS.DINING);
            def.POIUnlockable = true;
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<LoopingSounds>();
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.DiningTableType);
            go.AddOrGetDef<RocketUsageRestriction.Def>();
            go.AddOrGet<DualMinionDiningTable>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.GetComponent<KAnimControllerBase>().initialAnim = "off";
            var storage = BuildingTemplates.CreateDefaultStorage(go, false);
            storage.showInUI = true;
            storage.capacityKg = TableSaltTuning.SALTSHAKERSTORAGEMASS * SeatCount;
            var mdkg = go.AddOrGet<ManualDeliveryKG>();
            mdkg.SetStorage(storage);
            mdkg.RequestedItemTag = TableSaltConfig.ID.ToTag();
            mdkg.capacity = TableSaltTuning.SALTSHAKERSTORAGEMASS * SeatCount;
            mdkg.refillMass = TableSaltTuning.CONSUMABLE_RATE * SeatCount;
            mdkg.choreTypeIDHash = Db.Get().ChoreTypes.FoodFetch.IdHash;
            mdkg.ShowStatusItem = false;
        }
    }
}
