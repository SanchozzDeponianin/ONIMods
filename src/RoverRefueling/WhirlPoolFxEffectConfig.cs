﻿using UnityEngine;

namespace RoverRefueling
{
    public class WhirlPoolFxEffectConfig : IEntityConfig
    {
        public const string ID = "WhirlPoolFx";

        public GameObject CreatePrefab()
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

        public virtual string[] GetDlcIds() => null;
        public void OnPrefabInit(GameObject inst) { }
        public void OnSpawn(GameObject inst) { }
    }
}
