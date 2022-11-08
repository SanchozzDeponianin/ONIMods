using UnityEngine;

namespace GraveyardKeeper
{
    public class PlantSparkleFX : KMonoBehaviour
    {
        private const float duration = 15f;
        private static Vector3 offset = new Vector3(0.5f, 0.2f, 0.1f);
        private GameObject fx;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            fx = Util.KInstantiate(EffectPrefabs.Instance.SparkleStreakFX, transform.GetPosition() + offset);
            fx.SetActive(true);
            GameScheduler.Instance.Schedule(nameof(DestroySelf), duration, DestroySelf);
        }

        protected override void OnCleanUp()
        {
            Util.KDestroyGameObject(fx);
            base.OnCleanUp();
        }

        private void DestroySelf(object _)
        {
            Util.KDestroyGameObject(gameObject);
        }
    }
}
