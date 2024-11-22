using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace CrabsProfit
{
    using static STRINGS.ITEMS.INDUSTRIAL_PRODUCTS;
    public class CrabsProfitRandomOreConfig : IEntityConfig
    {
        public const string ID = "CrabsProfitRandomOre";
        public static readonly Tag TAG = TagManager.Create(ID);

        public string[] GetDlcIds() => Utils.GetDlcIds(DlcManager.AVAILABLE_ALL_VERSIONS);

        public GameObject CreatePrefab()
        {
            InitWeights();
            var go = EntityTemplates.CreateLooseEntity(
                id: ID,
                name: RANDOMORE.NAME,
                desc: RANDOMORE.DESC,
                mass: 1f,
                unitMass: false,
                anim: Assets.GetAnim("crabs_random_ore_kanim"),
                initialAnim: "idle1",
                sceneLayer: Grid.SceneLayer.Front,
                collisionShape: EntityTemplates.CollisionShape.CIRCLE,
                width: 0.5f,
                height: 0.5f,
                isPickupable: false,
                sortOrder: 0,
                element: SimHashes.Creature,
                additionalTags: null);
            return go;
        }

        public void OnPrefabInit(GameObject inst) { }
        public void OnSpawn(GameObject inst)
        {
            var ore = GameUtil.KInstantiate(Assets.GetPrefab(GetRandomOreId()), inst.transform.position, Grid.SceneLayer.Ore);
            var ore_pe = ore.GetComponent<PrimaryElement>();
            var my_pe = inst.GetComponent<PrimaryElement>();
            ore_pe.Units = my_pe.Units;
            ore_pe.Temperature = my_pe.Temperature;
            ore.SetActive(true);
            ore_pe.AddDisease(my_pe.DiseaseIdx, my_pe.DiseaseCount, string.Empty);
            Util.KDestroyGameObject(inst);
        }

        private Dictionary<SimHashes, float> weights;
        private float total_weight;

        private void InitWeights()
        {
            total_weight = 0;
            var opt = CrabsProfitOptions.Instance.Ore_Weights;
            weights = new Dictionary<SimHashes, float>()
            {
                { SimHashes.AluminumOre,    opt.AluminumOre},
                { SimHashes.Cinnabar,       opt.Cinnabar},
                { SimHashes.Cobaltite,      opt.Cobaltite},
                { SimHashes.Cuprite,        opt.Cuprite},
                { SimHashes.Electrum,       opt.Electrum},
                { SimHashes.FoolsGold,      opt.FoolsGold},
                { SimHashes.GoldAmalgam,    opt.GoldAmalgam},
                { SimHashes.IronOre,        opt.IronOre},
                { SimHashes.Lead,           opt.Lead},
                { SimHashes.Radium,         opt.Radium},
                { SimHashes.Rust,           opt.Rust},
                { SimHashes.UraniumOre,     opt.UraniumOre},
                { SimHashes.Wolframite,     opt.Wolframite},
                // из Chemical Processing:
                { (SimHashes)Hash.SDBMLower(nameof(opt.ArgentiteOre)),     opt.ArgentiteOre},
                { (SimHashes)Hash.SDBMLower(nameof(opt.AurichalciteOre)),  opt.AurichalciteOre},
            };
            weights.Keys.ToList().ForEach(hash =>
            {
                if (weights[hash] <= 0f || ElementLoader.FindElementByHash(hash) == null)
                    weights.Remove(hash);
            });
            weights.Keys.ToList().ForEach(hash => total_weight += weights[hash]);
            Debug.Assert(weights.Count > 0, "Random Ore weights table is empty!");
        }

        private Tag GetRandomOreId()
        {
            float weight = Random.value * total_weight;
            foreach (var hash in weights.Keys.ToList())
            {
                weight -= weights[hash];
                if (weight <= 0)
                    return hash.CreateTag();
            }
            Debug.Log("theoretically, this piece of code should not be executed");
            return SimHashes.Dirt.CreateTag();
        }
    }
}
