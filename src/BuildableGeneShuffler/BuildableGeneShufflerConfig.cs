using TUNING;
using UnityEngine;

namespace BuildableGeneShuffler
{
    public class BuildableGeneShufflerConfig : IBuildingConfig
    {
        public const string ID = "BuildableGeneShuffler";
        public static float BRINE_MASS = BUILDINGS.CONSTRUCTION_MASS_KG.TIER7[0] - BUILDINGS.CONSTRUCTION_MASS_KG.TIER6[0] - BUILDINGS.CONSTRUCTION_MASS_KG.TIER4[0];
        // todo: опеределиться с материалами
        public override BuildingDef CreateBuildingDef()
        {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 4,
                height: 3,
                anim: "old_geneshuffler_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER2,
                construction_time: BuildableGeneShufflerOptions.Instance.constructionTime,
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER6[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER4[0] },
                construction_materials: new string[] { SimHashes.Steel.ToString(), SimHashes.Glass.ToString() },
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER2,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.BONUS.TIER0,
                noise: NOISE_POLLUTION.NOISY.TIER0);
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "small";
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddComponent<BuildableGeneShuffler>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }
    }
}
