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

        private static bool AllowFarmTinkerDecorative => BetterPlantTendingOptions.Instance.AllowFarmTinkerDecorative;
        private bool IsWilting => BetterPlantTendingOptions.Instance.PreventTendingGrownOrWilting && wilting.IsWilting();
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

        //private delegate void QueueUpdateChore(Tinkerable tinkerable);
        //private static readonly QueueUpdateChore UpdateChore = typeof(Tinkerable).Detour<QueueUpdateChore>();

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
#if EXPANSION1
            Subscribe((int)GameHashes.CropTended, OnCropTendedDelegate);
#endif
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
            if (!wilting.IsWilting())
            {
                // шанс получить семя базовый + за счет эффектов
                float seedChance = this.GetAttributes().Get(ExtraSeedChance).GetTotalValue();
                // ... + за счет навыка фермера
                if (Random.Range(0f, 1f) <= seedChance + seedChanceByWorker)
                    hasExtraSeedAvailable = true;
            }
        }

        public void ExtractExtraSeed()
        {
            if (hasExtraSeedAvailable)
            {
                hasExtraSeedAvailable = false;
                seedProducer.ProduceSeed(seedProducer.seedInfo.seedId);
                //UpdateChore(tinkerable);
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
