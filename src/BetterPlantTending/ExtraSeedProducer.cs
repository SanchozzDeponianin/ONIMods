using System.Collections.Generic;
using Klei.AI;
using KSerialization;
using STRINGS;
using UnityEngine;

namespace BetterPlantTending
{
    using static ModAssets;
    using static STRINGS.UI.UISIDESCREENS.PLANTERSIDESCREEN;
    using handler = EventSystem.IntraObjectHandler<ExtraSeedProducer>;

    public class ExtraSeedProducer : KMonoBehaviour, IGameObjectEffectDescriptor
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private SeedProducer seedProducer;
        [MyCmpReq]
        private WiltCondition wilting;
#pragma warning restore CS0649

        [Serialize]
        private bool hasExtraSeedAvailable = false;

        [SerializeField]
        public bool isDecorative = true;
        public bool ExtraSeedAvailable
        {
            get => hasExtraSeedAvailable;
#if DEBUG
            set
            {
                if (value)
                {
                    hasExtraSeedAvailable = true;
                    if (isDecorative)
                        this.AddTag(GameTags.FullyGrown);
                }
                else
                {
                    hasExtraSeedAvailable = false;
                    this.RemoveTag(GameTags.FullyGrown);
                }
            }
#endif
        }
        public bool ShouldFarmTinkerTending => !isDecorative || !hasExtraSeedAvailable;

        private static readonly handler OnUprootedDelegate = new((component, data) => component.ExtractExtraSeed());
        private static readonly handler OnCropTendedDelegate = new((component, data) => component.CreateExtraSeed());

        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            var attributes = this.GetAttributes();
            attributes.Add(ExtraSeedChance);
            if (isDecorative)
                attributes.Add(ExtraSeedChanceDecorativeBaseValue);
            else
                attributes.Add(ExtraSeedChanceNotDecorativeBaseValue);
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.Uprooted, OnUprootedDelegate);
            Subscribe((int)GameHashes.Died, OnUprootedDelegate);
            Subscribe((int)GameHashes.CropTended, OnCropTendedDelegate);
            if (!ModOptions.Instance.extra_seeds.pip_required_to_extract)
                Subscribe((int)GameHashes.EffectRemoved, OnUprootedDelegate);
            this.AddTag(GameTags.GrowingPlant);
            if (hasExtraSeedAvailable && isDecorative)
                this.AddTag(GameTags.FullyGrown);
        }

        public override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.Uprooted, OnUprootedDelegate);
            Unsubscribe((int)GameHashes.Died, OnUprootedDelegate);
            Unsubscribe((int)GameHashes.CropTended, OnCropTendedDelegate);
            if (!ModOptions.Instance.extra_seeds.pip_required_to_extract)
                Unsubscribe((int)GameHashes.EffectRemoved, OnUprootedDelegate);
            base.OnCleanUp();
        }

        public void CreateExtraSeed(WorkerBase worker = null)
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
                {
                    hasExtraSeedAvailable = true;
                    if (isDecorative)
                        this.AddTag(GameTags.FullyGrown);
                }
            }
        }

        public void ExtractExtraSeed()
        {
            if (hasExtraSeedAvailable)
            {
                hasExtraSeedAvailable = false;
                seedProducer.ProduceSeed(seedProducer.seedInfo.seedId, 1, true);
                this.RemoveTag(GameTags.FullyGrown);
            }
        }

        // описатель шансов доп семян.
        public List<Descriptor> GetDescriptors(GameObject go)
        {
            float seedChance = this?.GetAttributes()?.Get(ExtraSeedChance)?.GetTotalValue() ??
                (!isDecorative ? ExtraSeedChanceNotDecorativeBaseValue.Value : ExtraSeedChanceDecorativeBaseValue.Value);
            string percent = GameUtil.GetFormattedPercent(seedChance * 100, GameUtil.TimeSlice.None);
            var affects = new List<string>() { DUPLICANTS.MODIFIERS.FARMTINKER.NAME };
            if (Game.IsDlcActiveForCurrentSave(DlcManager.EXPANSION1_ID))
            {
                affects.Add(CREATURES.SPECIES.DIVERGENT.VARIANT_BEETLE.NAME);
                affects.Add(CREATURES.SPECIES.DIVERGENT.VARIANT_WORM.NAME);
            }
            if (Game.IsDlcActiveForCurrentSave(DlcManager.DLC4_ID))
                affects.Add(CREATURES.SPECIES.BUTTERFLY.NAME);
            for (int i = 0; i < affects.Count; i++)
                affects[i] = UI.FormatAsKeyWord(UI.StripLinkFormatting(affects[i]));
            string pip = ModOptions.Instance.extra_seeds.pip_required_to_extract ? TOOLTIPS.SQUIRREL_NEEDED : string.Empty;
            var descs = new List<Descriptor>
            {
                new Descriptor(
                    txt: string.Format(BONUS_SEEDS, percent),
                    tooltip: string.Format(TOOLTIPS.BONUS_SEEDS, percent, string.Join(", ", affects), pip),
                    descriptorType: Descriptor.DescriptorType.Effect,
                    only_for_simple_info_screen: false)
            };
            return descs;
        }
    }
}
