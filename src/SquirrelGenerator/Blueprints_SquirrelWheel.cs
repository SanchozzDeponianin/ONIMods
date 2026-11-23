using Database;
using SanchozzONIMods.Lib;

namespace SquirrelGenerator
{
    public class Blueprints_SquirrelWheel : BlueprintProvider
    {
        public override void SetupBlueprints()
        {
            Utils.FixKanimBatchTag("generatorsquirrel_kanim", "generatorsquirrel_a_kanim", "generatorsquirrel_b_kanim");
            var a = blueprintCollection.buildingFacades.Find(info => info.id == "permit_generatormanual_a");
            if (a != null)
                Strings.Add("STRINGS.BLUEPRINTS.SQUIRRELWHEEL_A.DESC", a.desc);
            var b = blueprintCollection.buildingFacades.Find(info => info.id == "permit_generatormanual_b");
            if (b != null)
                Strings.Add("STRINGS.BLUEPRINTS.SQUIRRELWHEEL_B.DESC", b.desc);
            AddBuilding(SquirrelGeneratorConfig.ID, PermitRarity.Universal, "SquirrelWheel_A", "generatorsquirrel_a_kanim");
            AddBuilding(SquirrelGeneratorConfig.ID, PermitRarity.Universal, "SquirrelWheel_B", "generatorsquirrel_b_kanim");
        }
    }
}
