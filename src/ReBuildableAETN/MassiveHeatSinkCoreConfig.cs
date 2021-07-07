using System.Collections.Generic;
using UnityEngine;

namespace ReBuildableAETN
{
    public class MassiveHeatSinkCoreConfig : IEntityConfig
    {
        public const string ID = "MassiveHeatSinkCore";
        public const float MASS = 50f;
        public static Tag tag = TagManager.Create(ID);

        public GameObject CreatePrefab()
        {
            return EntityTemplates.CreateLooseEntity(
                id: ID,
                name: STRINGS.ITEMS.MASSIVE_HEATSINK_CORE.NAME,
                desc: STRINGS.ITEMS.MASSIVE_HEATSINK_CORE.DESC,
                mass: MASS,
                unitMass: true,
                anim: Assets.GetAnim("massiveheatsink_core_kanim"),
                initialAnim: "object",
                sceneLayer: Grid.SceneLayer.Front,
                collisionShape: EntityTemplates.CollisionShape.RECTANGLE,
                width: 0.8f,
                height: 0.9f,
                isPickupable: true,
                element: SimHashes.Unobtanium,
                additionalTags: new List<Tag> { GameTags.IndustrialIngredient }
                );
        }

        public string[] GetDlcIds()
        {
            return DlcManager.AVAILABLE_ALL_VERSIONS;
        }

        public void OnPrefabInit(GameObject inst)
        {
        }

        public void OnSpawn(GameObject inst)
        {
            inst.GetComponent<PrimaryElement>().Temperature = 100f;
        }
    }
}
