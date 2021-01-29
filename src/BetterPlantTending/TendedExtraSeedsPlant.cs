using KSerialization;

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
        private bool allowFarmTinker = false;
        private bool isNotDecorative = false;

        public bool ExtraSeedAvailable => hasExtraSeedAvailable;
        public bool ShouldDivergentTending => isNotDecorative || !hasExtraSeedAvailable;
        public bool ShouldFarmTinkerTending => isNotDecorative || (allowFarmTinker && !hasExtraSeedAvailable);

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            isNotDecorative = this.HasTag(OxyfernConfig.ID) || this.HasTag(ColdBreatherConfig.ID);
            allowFarmTinker = BetterPlantTendingOptions.Instance.AllowFarmTinkerDecorative;
            // todo: убрать
            Debug.Log($"ExtraSeedProducer.OnPrefabInit name={gameObject.name}, isNotDecorative = {isNotDecorative}, allowFarmTinker = {allowFarmTinker}");
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (!isNotDecorative)
                tinkerable.tinkerMaterialTag = allowFarmTinker ? FarmStationConfig.MATERIAL_FOR_TINKER : GameTags.Void;
#if EXPANSION1
            Subscribe((int)GameHashes.CropTended, OnCropTended);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.CropTended, OnCropTended);
            base.OnCleanUp();
#endif
        }

        private void OnCropTended(object data)
        {
            CreateExtraSeed(null);
        }
        // todo: доделать спавн доп семян при убобрении декоративных растений
        public void CreateExtraSeed(Worker worker)
        {
            // todo: поразмыслить над шансами семян
            // todo: для отладки. пока спавним 100% сразу
            hasExtraSeedAvailable = true;
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
