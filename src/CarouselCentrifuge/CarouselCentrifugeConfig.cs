using UnityEngine;
using TUNING;

namespace CarouselCentrifuge
{
    public class CarouselCentrifugeConfig : IBuildingConfig
	{
        public const string ID = "CarouselCentrifuge";

        public override BuildingDef CreateBuildingDef()
		{
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
				id: ID, 
				width: 5, 
				height: 5, 
				anim: "centrifuge_kanim", 
				hitpoints: BUILDINGS.HITPOINTS.TIER1, 
				construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER1, 
				construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER4, 
				construction_materials: MATERIALS.REFINED_METALS, 
				melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1, 
				build_location_rule: BuildLocationRule.OnFloor, 
				decor: BUILDINGS.DECOR.BONUS.TIER1, 
				noise: NOISE_POLLUTION.NOISY.TIER1
				);
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

