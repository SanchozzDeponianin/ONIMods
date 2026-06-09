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

        [SerializeField]
        public bool isAquatic = false;

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
        public bool PipRequiredToExtract => !isAquatic && ModOptions.Instance.extra_seeds.pip_required_to_extract;
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
            if (!PipRequiredToExtract)
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
            if (!PipRequiredToExtract)
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

            var list = ListPool<string, ExtraSeedProducer>.Allocate();
            list.Add(DUPLICANTS.MODIFIERS.FARMTINKER.NAME);
            if (!isAquatic && Game.IsDlcActiveForCurrentSave(DlcManager.EXPANSION1_ID))
            {
                list.Add(CREATURES.SPECIES.DIVERGENT.VARIANT_BEETLE.NAME);
                list.Add(CREATURES.SPECIES.DIVERGENT.VARIANT_WORM.NAME);
            }
            if (!isAquatic && Game.IsDlcActiveForCurrentSave(DlcManager.DLC4_ID))
                list.Add(CREATURES.SPECIES.BUTTERFLY.NAME);
            string affects = string.Join(", ", list);
            list.Clear();

            string extracts;
            if (PipRequiredToExtract)
            {
                list.Add(CREATURES.SPECIES.SQUIRREL.NAME);
                if (Game.IsDlcActiveForCurrentSave(DlcManager.DLC4_ID))
                    list.Add(CREATURES.SPECIES.STEGO.NAME);
                extracts = string.Format(TOOLTIPS.SQUIRREL_NEEDED, string.Join(UI.GAMEOBJECTEFFECTS.REQUIREMETS_OR, list));
            }
            else
                extracts = string.Empty;
            list.Recycle();

            var descs = new List<Descriptor>
            {
                new Descriptor(
                    txt: string.Format(BONUS_SEEDS, percent),
                    tooltip: string.Format(TOOLTIPS.BONUS_SEEDS, percent, affects, extracts),
                    descriptorType: Descriptor.DescriptorType.Effect,
                    only_for_simple_info_screen: false)
            };
            return descs;
        }
    }
}
