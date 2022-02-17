using UnityEngine;

namespace RoverRefueling
{
    public class WhirlPoolFxEffectConfig : IEntityConfig
    {
        public const string ID = "WhirlPoolFx";

        GameObject IEntityConfig.CreatePrefab()
        {
            var go = EntityTemplates.CreateEntity(ID, ID, false);
            var kbac = go.AddOrGet<KBatchedAnimController>();
            kbac.AnimFiles = new KAnimFile[] { Assets.GetAnim("whirlpool_fx_kanim") };
            kbac.materialType = KAnimBatchGroup.MaterialType.Simple;
            kbac.initialAnim = "loop";
            kbac.initialMode = KAnim.PlayMode.Loop;
            kbac.isMovable = true;
            kbac.destroyOnAnimComplete = false;
            kbac.TintColour = ElementLoader.FindElementByHash(SimHashes.Petroleum).substance.colour;
            go.AddOrGet<LoopingSounds>();
            return go;
        }

        string[] IEntityConfig.GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;
        void IEntityConfig.OnPrefabInit(GameObject inst) { }
        void IEntityConfig.OnSpawn(GameObject inst) { }
    }
}
