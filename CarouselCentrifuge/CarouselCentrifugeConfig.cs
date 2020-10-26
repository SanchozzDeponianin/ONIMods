using UnityEngine;
using TUNING;

namespace CarouselCentrifuge
{
    public class CarouselCentrifugeConfig : IBuildingConfig
	{
        public const string ID = "CarouselCentrifuge";

        public override BuildingDef CreateBuildingDef()
		{
			string id = ID;
			int width = 5;
			int height = 5;
            string anim = "centrifuge_kanim";
			int hitpoints = BUILDINGS.HITPOINTS.TIER1;
			float construction_time = TUNING.BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER1;
			float[] tier = BUILDINGS.CONSTRUCTION_MASS_KG.TIER4;
			string[] refined_METALS = MATERIALS.REFINED_METALS;
			float melting_point = BUILDINGS.MELTING_POINT_KELVIN.TIER1;
			BuildLocationRule build_location_rule = BuildLocationRule.OnFloor;
			EffectorValues tier1 = NOISE_POLLUTION.NOISY.TIER1;
			BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(id, width, height, anim, hitpoints, construction_time, tier, refined_METALS, melting_point, build_location_rule, BUILDINGS.DECOR.BONUS.TIER1, tier1);
			buildingDef.Floodable = true;
			buildingDef.Overheatable = true;
			buildingDef.RequiresPowerInput = true;
			buildingDef.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER5;
            buildingDef.ExhaustKilowattsWhenActive = BUILDINGS.EXHAUST_ENERGY_ACTIVE.TIER3;
            buildingDef.SelfHeatKilowattsWhenActive = BUILDINGS.SELF_HEAT_KILOWATTS.TIER4;
            buildingDef.PowerInputOffset = new CellOffset(-2, 0);
            buildingDef.ViewMode = OverlayModes.Power.ID;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "large";
            return buildingDef;
		}

		public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
		{
			go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.RecBuilding, false);
            go.AddOrGet<CarouselCentrifugeWorkable>();
            RoomTracker roomTracker = go.AddOrGet<RoomTracker>();
			roomTracker.requiredRoomType = Db.Get().RoomTypes.RecRoom.Id;
			roomTracker.requirement = RoomTracker.Requirement.Recommended;
        }

		public override void DoPostConfigureComplete(GameObject go)
		{
            go.AddOrGetDef<PoweredActiveController.Def>();
        }
	}
}

