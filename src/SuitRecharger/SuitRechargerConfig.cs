using UnityEngine;
using TUNING;

namespace SuitRecharger
{
    // todo: продумать что делать с износом
    // todo: продумать что делать с отходами из костюма
    // todo: при деконструкции нужно чтобы кислород выпадал в виде баллона
    // todo: может быть - ручную доставку в баллонах
    // todo: добавить статуситемы
    // todo: текстовка

    // todo: обнаружена лажа:
    // * при зеркальной постройке керосин не заливается из трубы - косяк у клеев. можно просто поменяться с кислородом

    public class SuitRechargerConfig : IBuildingConfig
    {
        public const string ID = "SuitRecharger";
        public const float O2_CAPACITY = 200f;
        public const float FUEL_CAPACITY = 100f;

        private ConduitPortInfo secondaryInputPort = new ConduitPortInfo(ConduitType.Liquid, new CellOffset(0, 0));

        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 2,
                height: 4,
                anim: "suitrecharger_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2,
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,
                construction_materials: MATERIALS.REFINED_METALS,
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.BONUS.TIER1,
                noise: NOISE_POLLUTION.NOISY.TIER0);
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER3;
            def.InputConduitType = ConduitType.Gas;
            def.UtilityInputOffset = new CellOffset(0, 1);
            def.PermittedRotations = PermittedRotations.FlipH;
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.SuitIDs, ID);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            var o2_consumer = go.AddOrGet<ConduitConsumer>();
            o2_consumer.conduitType = ConduitType.Gas;
            o2_consumer.consumptionRate = ConduitFlow.MAX_GAS_MASS;
            o2_consumer.capacityTag = GameTags.Oxygen;
            o2_consumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            o2_consumer.forceAlwaysSatisfied = true;
            o2_consumer.capacityKG = O2_CAPACITY;

            var storage = go.AddOrGet<Storage>();
            storage.capacityKg = O2_CAPACITY + FUEL_CAPACITY;

            var recharger = go.AddOrGet<SuitRecharger>();
            recharger.workLayer = Grid.SceneLayer.BuildingFront;
            recharger.portInfo = secondaryInputPort;
            var kanim = Assets.GetAnim("anim_interacts_suitrecharger_kanim");
            recharger.overrideAnims = new KAnimFile[] { kanim };
            // привязываемся к длительности анимации
            /*
            working_pre = 4.033333
            working_loop = 2
            working_pst = 4.333333
            */
            var kanim_data = kanim.GetData();
            for (int i = 0; i < kanim_data.animCount; i++)
            {
                var anim = kanim_data.GetAnim(i);
                if (anim.name == "working_pre")
                    SuitRecharger.warmupTime = anim.numFrames / anim.frameRate;
                if (anim.name == "working_loop")
                    SuitRecharger.сhargeTime = 2 * anim.numFrames / anim.frameRate;
            }

            //Prioritizable.AddRef(go);
        }

        private void AttachPort(GameObject go)
        {
            go.AddComponent<ConduitSecondaryInput>().portInfo = secondaryInputPort;
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            base.DoPostConfigurePreview(def, go);
            AttachPort(go);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
            AttachPort(go);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }
    }
}
