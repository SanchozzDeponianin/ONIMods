using System.Collections.Generic;

namespace BuildableGeneShuffler
{
    public class BuildableGeneShuffler : KMonoBehaviour
    {
        [MyCmpReq]
        Building building;

        [MyCmpReq]
        Deconstructable deconstructable;

        //[MyCmpReq]
        //Storage storage;


        protected override void OnSpawn()
        {
            base.OnSpawn();
            SpawnGeneShuffler();
        }

        private void SpawnGeneShuffler()
        {
            // todo: ряд настроечных манипуляций со свеже построеным генечтототам
            var geneShuffler = GameUtil.KInstantiate(Assets.GetPrefab("GeneShuffler"), gameObject.transform.GetPosition(), Grid.SceneLayer.Building);
            geneShuffler.GetComponent<GeneShuffler>().IsConsumed = true;
            var builded = geneShuffler.GetComponent<BuildedGeneShuffler>();
            builded.isBuilded = true;

            var l = new List<Tuple<Tag, float>>();
            float mass = 0f;
            for (int i = deconstructable.constructionElements.Length; i < deconstructable.constructionElements.Length; i++)
            {
                l.Add(new Tuple<Tag, float>(deconstructable.constructionElements[i], building.Def.Mass[i]));
                mass += building.Def.Mass[i];
            }
            // todo: добавить рассол или все куски в хранилищще
            l.Add(new Tuple<Tag, float>(SimHashes.Brine.CreateTag(), BuildableGeneShufflerConfig.BRINE_MASS));
            mass += BuildableGeneShufflerConfig.BRINE_MASS;
            builded.constructionElements = l.ToArray();
            // todo: скорректировать массу и элемент и микробав
            var geneShufflerPE = geneShuffler.GetComponent<PrimaryElement>();
            var MyPE = GetComponent<PrimaryElement>();
            geneShufflerPE.SetElement(MyPE.ElementID);
            geneShufflerPE.Mass = mass;
            geneShuffler.SetActive(true);
            gameObject.DeleteObject();
        }
    }
}
