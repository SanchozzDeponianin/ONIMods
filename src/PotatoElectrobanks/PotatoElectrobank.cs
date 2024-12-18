using UnityEngine;

namespace PotatoElectrobanks
{
    public class PotatoElectrobank : Electrobank
    {
        [SerializeField]
        public float maxCapacity = ElectrobankConfig.POWER_CAPACITY;

        [SerializeField]
        public Tag garbage;

        [SerializeField]
        public float garbageMass;

        private bool garbaged = false;

        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            charge = maxCapacity;
            Subscribe((int)GameHashes.OnStore, OnStore);
        }

        public override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.OnStore, OnStore);
            base.OnCleanUp();
        }

        private void OnStore(object _)
        {
            if (charge <= 0f && pickupable.storage == null)
                ReplaceWithGarbage();
        }

        public override void OnEmpty(bool dropWhenEmpty)
        {
            if (dropWhenEmpty)
            {
                SpawnGarbage();
                keepEmpty = false;
            }
            else
            {
                this.RemoveTag(GameTags.ChargedPortableBattery);
                this.AddTag(GameTags.EmptyPortableBattery);
            }
            base.OnEmpty(dropWhenEmpty);
        }

        public override void Explode()
        {
            SpawnGarbage();
            base.Explode();
        }

        private void SpawnGarbage()
        {
            if (!garbaged && garbage.IsValid && garbageMass > 0f)
            {
                var go = Util.KInstantiate(Assets.GetPrefab(this.garbage), transform.GetPosition());
                var garbage = go.GetComponent<PrimaryElement>();
                garbage.Mass = garbageMass;
                garbage.Temperature = GetComponent<PrimaryElement>().Temperature;
                go.SetActive(true);
                garbaged = true;
            }
        }

        private void ReplaceWithGarbage()
        {
            SpawnGarbage();
            this.DeleteObject();
        }
    }
}
