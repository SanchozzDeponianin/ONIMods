using System.Collections.Generic;
using Klei.AI;
using KSerialization;
using UnityEngine;
using STRINGS;
using PeterHan.PLib.Detours;
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
        [MyCmpReq]
        private WiltCondition wilting;
#pragma warning restore CS0649

        [Serialize]
        private bool hasExtraSeedAvailable = false;
        
        [SerializeField]
        internal bool isNotDecorative = false;

        private static bool AllowFarmTinkerDecorative => BetterPlantTendingOptions.Instance.allow_tinker_decorative;
        private bool IsWilting => BetterPlantTendingOptions.Instance.prevent_tending_grown_or_wilting && wilting.IsWilting();// todo: косяк ?
        public bool ExtraSeedAvailable => hasExtraSeedAvailable;
        public bool ShouldDivergentTending => (isNotDecorative || !hasExtraSeedAvailable) && !IsWilting;
        public bool ShouldFarmTinkerTending => isNotDecorative || !hasExtraSeedAvailable;

        private static readonly EventSystem.IntraObjectHandler<ExtraSeedProducer> OnUprootedDelegate = new EventSystem.IntraObjectHandler<ExtraSeedProducer>(delegate (ExtraSeedProducer component, object data)
        {
            component.ExtractExtraSeed();
        });

        private static readonly EventSystem.IntraObjectHandler<ExtraSeedProducer> OnCropTendedDelegate = new EventSystem.IntraObjectHandler<ExtraSeedProducer>(delegate (ExtraSeedProducer component, object data)
        {
            component.CreateExtraSeed();
        });

        private static readonly System.Func<SeedProducer, string, int, bool, GameObject> ProduceSeed = 
            typeof(SeedProducer).Detour<System.Func<SeedProducer, string, int, bool, GameObject>>("ProduceSeed");

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            var attributes = this.GetAttributes();
            attributes.Add(ExtraSeedChance);
            if (isNotDecorative)
                attributes.Add(ExtraSeedChanceNotDecorativeBaseValue);
            else
                attributes.Add(ExtraSeedChanceDecorativeBaseValue);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (!isNotDecorative)
                tinkerable.tinkerMaterialTag = AllowFarmTinkerDecorative ? FarmStationConfig.TINKER_TOOLS : GameTags.Void;
            Subscribe((int)GameHashes.Uprooted, OnUprootedDelegate);
            Subscribe((int)GameHashes.Died, OnUprootedDelegate);
            Subscribe((int)GameHashes.CropTended, OnCropTendedDelegate);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.Uprooted, OnUprootedDelegate);
            Unsubscribe((int)GameHashes.Died, OnUprootedDelegate);
            Unsubscribe((int)GameHashes.CropTended, OnCropTendedDelegate);
            base.OnCleanUp();
        }

        public void CreateExtraSeed(Worker worker = null)
        {
            if (!hasExtraSeedAvailable && !wilting.IsWilting())
            {
                // шанс получить семя базовый + за счет эффектов
                float seedChance = this.GetAttributes().Get(ExtraSeedChance).GetTotalValue();
                // шанс получить семя за счет навыка фермера
                if (worker != null)
                {
                    seedChance += worker.GetComponent<AttributeConverters>().Get(
                        Db.Get().AttributeConverters.SeedHarvestChance).Evaluate();
                }
                if (Random.Range(0f, 1f) <= seedChance)
                    hasExtraSeedAvailable = true;
            }
        }

        public void ExtractExtraSeed()
        {
            if (hasExtraSeedAvailable)
            {
                hasExtraSeedAvailable = false;
                ProduceSeed(seedProducer, seedProducer.seedInfo.seedId, 1, true);
            }
        }

        // описатель шансов доп семян.
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
