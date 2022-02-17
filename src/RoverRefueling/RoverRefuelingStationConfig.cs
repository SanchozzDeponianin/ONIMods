using TUNING;
using UnityEngine;

namespace RoverRefueling
{
    public class RoverRefuelingStationConfig : IBuildingConfig
    {
        public const string ID = "RoverRefuelingStation";
        // todo: определиться с рейтами
        public const float CHARGE_TIME = 0.1f * Constants.SECONDS_PER_CYCLE;
        public const float CHARGE_MASS = 100f;
        public const float MINIMUM_MASS = 0.1f * CHARGE_MASS;
        public const float CAPACITY = 2 * CHARGE_MASS;

        public override string[] GetDlcIds() => DlcManager.AVAILABLE_EXPANSION1_ONLY;
        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 2,
                height: 3,
                anim: "oxygen_mask_station_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2,
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER2,
                construction_materials: MATERIALS.RAW_METALS,
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.BONUS.TIER1,
                noise: NOISE_POLLUTION.NOISY.TIER0);
            def.OverheatTemperature = BUILDINGS.OVERHEAT_TEMPERATURES.HIGH_2;
            def.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(CellOffset.none);
            def.InputConduitType = ConduitType.Liquid;
            def.UtilityInputOffset = CellOffset.none;
            def.PermittedRotations = PermittedRotations.FlipH;
            def.ViewMode = OverlayModes.LiquidConduits.ID;
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.LiquidVentIDs, ID);
            return def;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery, false);
            var storage = BuildingTemplates.CreateDefaultStorage(go, false);
            storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            Prioritizable.AddRef(go); // todo: ?
            var md = go.AddComponent<ManualDeliveryKG>();
            md.SetStorage(storage);
            md.requestedItemTag = GameTags.CombustibleLiquid;
            md.capacity = CAPACITY;
            md.refillMass = CHARGE_MASS;
            md.choreTypeIDHash = Db.Get().ChoreTypes.MachineFetch.IdHash;
            ConduitConsumer consumer = go.AddOrGet<ConduitConsumer>();
            consumer.conduitType = ConduitType.Liquid;
            consumer.consumptionRate = ConduitFlow.MAX_LIQUID_MASS;
            consumer.capacityKG = CAPACITY;
            consumer.capacityTag = GameTags.CombustibleLiquid;
            consumer.forceAlwaysSatisfied = true;
            consumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            go.AddOrGet<RoverRefuelingStation>();
        }
    }
}
