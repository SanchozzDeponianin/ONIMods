using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace BuildableGeneShuffler
{
    public class BuildableGeneShufflerConfig : IBuildingConfig
    {
        public const string ID = "BuildableGeneShuffler";
        public const string anim = "old_geneshuffler_kanim";
        private static readonly float total_mass = BUILDINGS.CONSTRUCTION_MASS_KG.TIER7[0];
        private static readonly float metal_mass = BUILDINGS.CONSTRUCTION_MASS_KG.TIER6[0];
        private static readonly float glass_mass = BUILDINGS.CONSTRUCTION_MASS_KG.TIER4[0];
        public static readonly float brine_mass = total_mass - metal_mass - glass_mass;

        public override string[] GetDlcIds() => Utils.GetDlcIds(base.GetDlcIds());

        public override BuildingDef CreateBuildingDef()
        {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 4,
                height: 3,
                anim: anim,
                hitpoints: BUILDINGS.HITPOINTS.TIER2,
                construction_time: BuildableGeneShufflerOptions.Instance.constructionTime,
                construction_mass: new float[] { metal_mass, glass_mass },
                construction_materials: new string[] { MATERIALS.REFINED_METAL, MATERIALS.GLASS },
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER2,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.BONUS.TIER0,
                noise: NOISE_POLLUTION.NOISY.TIER0);
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "small";
            buildingDef.Breakable = false;
            buildingDef.Invincible = true;
            buildingDef.Overheatable = false;
            buildingDef.Repairable = false;
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<KPrefabID>().AddTag(GameTags.NotRocketInteriorBuilding);
            var storage = go.AddOrGet<Storage>();
            storage.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);

            var md = go.AddOrGet<ManualDeliveryKG>();
            md.capacity = brine_mass;
            md.refillMass = brine_mass;
            md.RequestedItemTag = SimHashes.Brine.CreateTag();
            md.choreTypeIDHash = Db.Get().ChoreTypes.DoctorFetch.IdHash;
            md.operationalRequirement = Operational.State.Functional;
            md.SetStorage(storage);

            go.AddOrGet<BuildableGeneShuffler>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }
    }
}
