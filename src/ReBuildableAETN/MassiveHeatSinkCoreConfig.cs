﻿using System.Collections.Generic;
using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace ReBuildableAETN
{
    [EntityConfigOrder(2)]
    public class MassiveHeatSinkCoreConfig : IEntityConfig, IHasDlcRestrictions
    {
        public const string ID = "MassiveHeatSinkCore";
        public static float MASS = 1f; // ArtifactConfig.ARTIFACT_MASS;
        public const float TEMPERATURE = 100f;
        public static Tag TAG = TagManager.Create(ID);
        public static Tag MaterialBuildingTag = TagManager.Create("BuildingNeutroniumCore");

        public virtual string[] GetDlcIds() => null;
        public string[] GetRequiredDlcIds() => Utils.GetDlcIds();
        public string[] GetForbiddenDlcIds() => null;

        public static readonly ArtifactTier TIER_CORE = new(STRINGS.UI.SPACEARTIFACTS.ARTIFACTTIERS.TIER_CORE.key, DECOR.BONUS.TIER7, 0);

        public GameObject CreatePrefab()
        {
            var go = EntityTemplates.CreateLooseEntity(
                id: ID,
                name: STRINGS.ITEMS.MASSIVE_HEATSINK_CORE.NAME,
                desc: STRINGS.ITEMS.MASSIVE_HEATSINK_CORE.DESC,
                mass: MASS,
                unitMass: true,
                anim: Assets.GetAnim("massiveheatsink_core_kanim"),
                initialAnim: "idle_crystal",
                sceneLayer: Grid.SceneLayer.Ore,
                collisionShape: EntityTemplates.CollisionShape.RECTANGLE,
                width: 1f,
                height: 1f,
                isPickupable: true,
                sortOrder: SORTORDER.BUILDINGELEMENTS,
                element: SimHashes.Unobtanium,
                additionalTags: new List<Tag> {
                    GameTags.IndustrialIngredient,
                    GameTags.PedestalDisplayable,
                    GameTags.Artifact,
                    MaterialBuildingTag,
                });

            // это частично спокировано из ArtifactConfig.CreateArtifact, надо поглядывать если что то поменяют
            go.AddOrGet<OccupyArea>().SetCellOffsets(EntityTemplates.GenerateOffsets(1, 1));
            var decorProvider = go.AddOrGet<DecorProvider>();
            decorProvider.SetValues(TIER_CORE.decorValues);
            decorProvider.overrideName = STRINGS.ITEMS.MASSIVE_HEATSINK_CORE.NAME;
            var spaceArtifact = go.AddOrGet<SpaceArtifact>();
            spaceArtifact.SetUIAnim("ui_crystal");
            spaceArtifact.SetArtifactTier(TIER_CORE);
            spaceArtifact.uniqueAnimNameFragment = "idle_crystal";
            spaceArtifact.artifactType = ArtifactType.Any;
            go.AddOrGet<KSelectable>();
            go.GetComponent<KBatchedAnimController>().initialMode = KAnim.PlayMode.Loop;

            var pe = go.GetComponent<PrimaryElement>();
            pe.Mass = MASS;
            pe.Temperature = TEMPERATURE;

            // добавляем в список артифактов только в ваниле, воизбежание непредвиденных последствий на длц
            if (DlcManager.IsPureVanilla())
                ArtifactConfig.artifactItems[ArtifactType.Any].Add(go.name);
            return go;
        }

        public void OnPrefabInit(GameObject inst)
        {
        }

        public void OnSpawn(GameObject inst)
        {
            inst.GetComponent<SpaceArtifact>().SetArtifactTier(TIER_CORE);
        }
    }
}
