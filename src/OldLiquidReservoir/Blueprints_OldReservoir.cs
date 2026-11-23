using Database;
using SanchozzONIMods.Lib;

namespace OldLiquidReservoir
{
    public class Blueprints_OldReservoir : BlueprintProvider
    {
        public override void SetupBlueprints()
        {
            Utils.FixKanimBatchTag("liquidreservoir_veryold_kanim", "liquidreservoir_rocketry_kanim");
            AddBuilding(LiquidReservoirConfig.ID, PermitRarity.Universal, "LiquidReservoir_Early_Access", "liquidreservoir_veryold_kanim");
            AddBuilding(LiquidReservoirConfig.ID, PermitRarity.Universal, "LiquidReservoir_Rocketry_Update", "liquidreservoir_rocketry_kanim");
        }
    }
}
