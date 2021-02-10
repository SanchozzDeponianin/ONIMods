using System.Collections.Generic;
using Klei.AI;
using KSerialization;
using UnityEngine;
using STRINGS;
using static BetterPlantTending.BetterPlantTendingAssets;

namespace BetterPlantTending
{
    public class ExtraSeedProducer : KMonoBehaviour, IGameObjectEffectDescriptor
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
        [SerializeField]
        internal bool isNotDecorative = false;

        public bool ExtraSeedAvailable => hasExtraSeedAvailable;
        public bool ShouldDivergentTending => isNotDecorative || !hasExtraSeedAvailable;
        public bool ShouldFarmTinkerTending => isNotDecorative || (allowFarmTinkerDecorative && !hasExtraSeedAvailable);

        private static readonly EventSystem.IntraObjectHandler<ExtraSeedProducer> OnUprootedDelegate = new EventSystem.IntraObjectHandler<ExtraSeedProducer>(delegate (ExtraSeedProducer component, object data)
        {
            component.ExtractExtraSeed();
        });

        private static readonly EventSystem.IntraObjectHandler<ExtraSeedProducer> OnCropTendedDelegate = new EventSystem.IntraObjectHandler<ExtraSeedProducer>(delegate (ExtraSeedProducer component, object data)
        {
            component.CreateExtraSeed();
        });

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            allowFarmTinkerDecorative = BetterPlantTendingOptions.Instance.AllowFarmTinkerDecorative;
            var attributes = this.GetAttributes();
            attributes.Add(ExtraSeedChance);
            if (isNotDecorative)
                attributes.Add(ExtraSeedChanceNotDecorativeBaseValue);
            else
                attributes.Add(ExtraSeedChanceDecorativeBaseValue);

            // todo: потом убрать
            Debug.Log($"ExtraSeedProducer.OnPrefabInit name={gameObject.name}, isNotDecorative = {isNotDecorative}, allowFarmTinkerDecorative = {allowFarmTinkerDecorative}");
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (!isNotDecorative)
                tinkerable.tinkerMaterialTag = allowFarmTinkerDecorative ? FarmStationConfig.TINKER_TOOLS : GameTags.Void;
            Subscribe((int)GameHashes.Uprooted, OnUprootedDelegate);
            Subscribe((int)GameHashes.Died, OnUprootedDelegate);
#if EXPANSION1
            Subscribe((int)GameHashes.CropTended, OnCropTendedDelegate);
#endif

            // todo: потом убрать
            Debug.Log($"ExtraSeedProducer.OnSpawn name={gameObject.name}, isNotDecorative = {isNotDecorative}, allowFarmTinkerDecorative = {allowFarmTinkerDecorative}");
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.Uprooted, OnUprootedDelegate);
            Unsubscribe((int)GameHashes.Died, OnUprootedDelegate);
#if EXPANSION1
            Unsubscribe((int)GameHashes.CropTended, OnCropTendedDelegate);
#endif
            base.OnCleanUp();
        }

        public void CreateExtraSeed(float seedChanceByWorker = 0)
        {
            // шанс получить семя базовый + за счет эффектов
            float seedChance = this.GetAttributes().Get(ExtraSeedChance).GetTotalValue();
            // ... + за счет навыка фермера
            if (UnityEngine.Random.Range(0f, 1f) <= seedChance + seedChanceByWorker)
                hasExtraSeedAvailable = true;

            // todo: потом убрать
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

        // todo: описатель шансов доп семян. доделать текст
        public List<Descriptor> GetDescriptors(GameObject go)
        {
            float seedChance = this?.GetAttributes()?.Get(ExtraSeedChance)?.GetTotalValue() ??
                (isNotDecorative ? ExtraSeedChanceNotDecorativeBaseValue.Value : ExtraSeedChanceDecorativeBaseValue.Value);
            string percent = GameUtil.GetFormattedPercent(seedChance * 100, GameUtil.TimeSlice.None);
            var descs = new List<Descriptor>
            {
                new Descriptor(
                    txt: string.Format(UI.UISIDESCREENS.PLANTERSIDESCREEN.BONUS_SEEDS, percent),
                    tooltip: string.Format(UI.UISIDESCREENS.PLANTERSIDESCREEN.TOOLTIPS.BONUS_SEEDS, percent),
                    descriptorType: Descriptor.DescriptorType.Effect,
                    only_for_simple_info_screen: false)
            };
            return descs;
        }
    }
}
