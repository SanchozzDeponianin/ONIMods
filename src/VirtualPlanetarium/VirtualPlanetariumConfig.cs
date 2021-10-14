using UnityEngine;
using TUNING;

namespace VirtualPlanetarium
{
    public class VirtualPlanetariumConfig : IBuildingConfig
    {
        public const string ID = "VirtualPlanetarium";

        public override string[] GetDlcIds()
        {
            return DlcManager.AVAILABLE_EXPANSION1_ONLY;
        }

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 4,
                height: 4,
                anim: "research_space_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2,
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
            buildingDef.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER3;
            buildingDef.ExhaustKilowattsWhenActive = BUILDINGS.EXHAUST_ENERGY_ACTIVE.TIER3;
            buildingDef.SelfHeatKilowattsWhenActive = BUILDINGS.SELF_HEAT_KILOWATTS.TIER4;
            buildingDef.ViewMode = OverlayModes.Power.ID;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "large";
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.RecBuilding, false);
            var storage = go.AddOrGet<Storage>();
            storage.capacityKg = 10 * VirtualPlanetariumWorkable.INGREDIENT_MASS_PER_USE;
            storage.SetDefaultStoredItemModifiers(Storage.StandardFabricatorStorage);
            var manualDeliveryKG = go.AddOrGet<ManualDeliveryKG>();
            manualDeliveryKG.SetStorage(storage);
            manualDeliveryKG.requestedItemTag = VirtualPlanetariumWorkable.INGREDIENT_TAG;
            manualDeliveryKG.capacity = 10 * VirtualPlanetariumWorkable.INGREDIENT_MASS_PER_USE;
            manualDeliveryKG.refillMass = 5 * VirtualPlanetariumWorkable.INGREDIENT_MASS_PER_USE;
            manualDeliveryKG.minimumMass = VirtualPlanetariumWorkable.INGREDIENT_MASS_PER_USE;
            manualDeliveryKG.choreTypeIDHash = Db.Get().ChoreTypes.MachineFetch.IdHash;
            go.AddOrGet<VirtualPlanetariumWorkable>();
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

