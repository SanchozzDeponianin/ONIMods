using Klei.AI;
using KSerialization;
using static BetterPlantTending.BetterPlantTendingAssets;

namespace BetterPlantTending
{
    public class ExtraSeedProducer : KMonoBehaviour
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private SeedProducer seedProducer;
        [MyCmpReq]
        private Tinkerable tinkerable;
#pragma warning restore CS0649

        [Serialize]
        private bool hasExtraSeedAvailable = false;
        private bool allowFarmTinkerDecorative = false;
        private bool isNotDecorative = false;

        public bool ExtraSeedAvailable => hasExtraSeedAvailable;
        public bool ShouldDivergentTending => isNotDecorative || !hasExtraSeedAvailable;
        public bool ShouldFarmTinkerTending => isNotDecorative || (allowFarmTinkerDecorative && !hasExtraSeedAvailable);

        // todo: переписать подписку на EventSystem.IntraObjectHandler
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            isNotDecorative = this.HasTag(OxyfernConfig.ID) || this.HasTag(ColdBreatherConfig.ID);
            allowFarmTinkerDecorative = BetterPlantTendingOptions.Instance.AllowFarmTinkerDecorative;
            var attributes = this.GetAttributes();
            attributes.Add(ExtraSeedChance);
            if (isNotDecorative)
                attributes.Add(ExtraSeedChanceNotDecorativeBaseValue);
            else
                attributes.Add(ExtraSeedChanceDecorativeBaseValue);
            // todo: убрать
            Debug.Log($"ExtraSeedProducer.OnPrefabInit name={gameObject.name}, isNotDecorative = {isNotDecorative}, allowFarmTinkerDecorative = {allowFarmTinkerDecorative}");
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            allowFarmTinkerDecorative = BetterPlantTendingOptions.Instance.AllowFarmTinkerDecorative;
            if (!isNotDecorative)
                tinkerable.tinkerMaterialTag = allowFarmTinkerDecorative ? FarmStationConfig.TINKER_TOOLS : GameTags.Void;
            Debug.Log($"ExtraSeedProducer.OnSpawn name={gameObject.name}, isNotDecorative = {isNotDecorative}, allowFarmTinkerDecorative = {allowFarmTinkerDecorative}");
            Subscribe((int)GameHashes.Uprooted, OnUprooted);
            Subscribe((int)GameHashes.Died, OnUprooted);
#if EXPANSION1
            Subscribe((int)GameHashes.CropTended, OnCropTended);
#endif
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.Uprooted, OnUprooted);
            Unsubscribe((int)GameHashes.Died, OnUprooted);
#if EXPANSION1
            Unsubscribe((int)GameHashes.CropTended, OnCropTended);
#endif
            base.OnCleanUp();
        }

        private void OnCropTended(object data)
        {
            CreateExtraSeed();
        }
        private void OnUprooted(object data)
        {
            ExtractExtraSeed();
        }

        public void CreateExtraSeed(float seedChanceByWorker = 0)
        {
            // шанс получить семя базовый + за счет эффектов
            float seedChance = this.GetAttributes().Get(ExtraSeedChance).GetTotalValue();
            // ... + за счет навыка фермера
            if (UnityEngine.Random.Range(0f, 1f) <= seedChance + seedChanceByWorker)
                hasExtraSeedAvailable = true;


            Debug.Log($"CreateExtraSeed: name={gameObject.name}, seedChance={seedChance}, seedChanceByWorker={seedChanceByWorker}, Total={seedChance + seedChanceByWorker}, hasExtraSeedAvailable={hasExtraSeedAvailable}");
        }

        public void ExtractExtraSeed()
        {
            if (hasExtraSeedAvailable)
            {
                hasExtraSeedAvailable = false;
                seedProducer.ProduceSeed(seedProducer.seedInfo.seedId);
            }
        }

        // todo: описатель шансов доп семян
    }
}
