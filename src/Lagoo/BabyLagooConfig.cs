using UnityEngine;
using SanchozzONIMods.Lib;

namespace Lagoo
{
    using static LagooPatches;
    using static STRINGS.CREATURES.SPECIES.SQUIRREL.VARIANT_LAGOO;

    [EntityConfigOrder(3)]
    public class BabyLagooConfig : IEntityConfig, IHasDlcRestrictions
    {
        public virtual string[] GetDlcIds() => null;
        public string[] GetRequiredDlcIds() => Utils.GetDlcIds();
        public string[] GetForbiddenDlcIds() => null;

        public GameObject CreatePrefab()
        {
            var prefab = LagooConfig.CreateSquirrelLagoo(LagooConfig.BABY_ID, BABY.NAME, BABY.DESC, baby_squirrel_kanim, true);
            EntityTemplates.ExtendEntityToBeingABaby(prefab, LagooConfig.ID);
            return prefab;
        }

        public void OnPrefabInit(GameObject prefab) { }

        public void OnSpawn(GameObject inst)
        {
            if (inst.TryGetComponent(out KBatchedAnimController kbac))
                kbac.AddAnimOverrides(Assets.GetAnim(baby_lagoo_kanim), 1);
        }
    }
}
