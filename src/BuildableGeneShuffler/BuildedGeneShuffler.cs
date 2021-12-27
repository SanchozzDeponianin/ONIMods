using KSerialization;

namespace BuildableGeneShuffler
{
    public class BuildedGeneShuffler : KMonoBehaviour
    {
        [Serialize]
        public bool isBuilded = false;

        [Serialize]
        public Tuple<Tag, float>[] constructionElements = new Tuple<Tag, float>[0];

        protected override void OnSpawn()
        {
            base.OnSpawn();

            Debug.Log("BuildedGeneShuffler.OnSpawn");
            Debug.Log("constructionElements:");
            for (int x = 0; x < constructionElements.Length; x++)
                Debug.Log($"{constructionElements[x].first.Name} => {constructionElements[x].second}");


            if (isBuilded)
            {
                GetComponent<KBatchedAnimController>().AnimFiles = new KAnimFile[] { Assets.GetAnim("old_geneshuffler_kanim") };
            }
        }
    }
}
