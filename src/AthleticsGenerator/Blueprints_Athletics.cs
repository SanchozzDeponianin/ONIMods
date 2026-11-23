using SanchozzONIMods.Lib;

namespace AthleticsGenerator
{
    public class Blueprints_Athletics : BlueprintProvider
    {
        public override void SetupBlueprints()
        {
            if (ModOptions.Instance.enable_meter)
            {
                Utils.FixKanimBatchTag("generator_athletic_kanim", "generator_athletic_a_kanim", "generator_athletic_b_kanim");
                var a = blueprintCollection.buildingFacades.Find(info => info.id == "permit_generatormanual_a");
                if (a != null)
                    a.animFile = "generator_athletic_a_kanim";
                var b = blueprintCollection.buildingFacades.Find(info => info.id == "permit_generatormanual_b");
                if (b != null)
                    b.animFile = "generator_athletic_b_kanim";
            }
        }
    }
}
