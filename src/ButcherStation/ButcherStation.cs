using System;
using System.Collections.Generic;
using System.Linq;
using Klei.AI;
using KSerialization;
using STRINGS;
using UnityEngine;

namespace ButcherStation
{
    public class ButcherStation : KMonoBehaviour, ISim4000ms, IIntSliderControl, ISliderControl, IUserControlledCapacity, ICheckboxControl
    {
        public static readonly Tag ButcherableCreature = TagManager.Create("ButcherableCreature");
        public static readonly Tag FisherableCreature = TagManager.Create("FisherableCreature");

        public Tag creatureEligibleTag = ButcherableCreature;

        public const int CREATURELIMIT = 20;
        public const float EXTRAMEATPERRANCHINGATTRIBUTE = 0.025f;

        [Serialize]
        private int creatureLimit = Config.Get().MAXCREATURELIMIT;
        private int storedCreatureCount;
        internal List<KPrefabID> Creatures { get; private set; } = new List<KPrefabID>();
        bool dirty = true;

        [Serialize]
        private float ageButchThresold = 0.85f;

        [Serialize]
        private bool autoButchSurplus = false;

#pragma warning disable CS0649
        [MyCmpReq]
        TreeFilterable treeFilterable;
#pragma warning restore CS0649

        private static StatusItem capacityStatusItem;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            if (capacityStatusItem == null)
            {
                capacityStatusItem = new StatusItem("StorageLocker", "BUILDING", string.Empty, StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID)
                {
                    resolveStringCallback = delegate (string str, object data)
                    {
                        var userControlledCapacity = (IUserControlledCapacity)data;
                        string stored = Util.FormatWholeNumber(Mathf.Floor(userControlledCapacity.AmountStored));
                        string capacity = Util.FormatWholeNumber(userControlledCapacity.UserMaxCapacity);
                        return str.Replace("{Stored}", stored).Replace("{Capacity}", capacity).Replace("{Units}", userControlledCapacity.CapacityUnits);
                    }
                };
            }
            GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, capacityStatusItem, this);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.CopySettings, OnCopySettings);
            treeFilterable.OnFilterChanged += OnFilterChanged;
            RefreshCreatures();
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.CopySettings, OnCopySettings);
            treeFilterable.OnFilterChanged -= OnFilterChanged;
            base.OnCleanUp();
        }

        private void OnCopySettings(object data)
        {
            var butcherStation = ((GameObject)data)?.GetComponent<ButcherStation>();
            if (butcherStation != null)
            {
                creatureLimit = butcherStation.creatureLimit;
                ageButchThresold = butcherStation.ageButchThresold;
                autoButchSurplus = butcherStation.autoButchSurplus;
            }
        }

        private void OnFilterChanged(Tag[] tags)
        {
            dirty = true;
        }

        public void Sim4000ms(float dt)
        {
            RefreshCreatures();
        }

        internal void RefreshCreatures(List<KPrefabID> creatures = null)
        {
            // обновляем число жеготных в комнате
            // список может передаваться из патча для RanchStation.Instance
            if (creatures == null)
            {
                int cell = this.GetSMI<RanchStation.Instance>()?.GetTargetRanchCell() ?? Grid.InvalidCell;
                creatures = Game.Instance.roomProber.GetCavityForCell(cell)?.creatures ?? new List<KPrefabID>();
            }
            int old = storedCreatureCount;
            storedCreatureCount = 0;
            foreach (KPrefabID creature in creatures)
            {
                if (!creature.HasTag(GameTags.Creatures.Bagged) && !creature.HasTag(GameTags.Trapped) && !creature.HasTag(GameTags.Dying))
                {
                    storedCreatureCount++;
                }
            }
            if (old != storedCreatureCount)
                dirty = true;
            if (dirty)
            {
                // для оптимизации упорядочиваем список жеготных. 
                // вначале идут не выбранные в фильтре, затем по возрасту.
                Creatures.Clear();
                var Age = Db.Get().Amounts.Age;
                Creatures.AddRange(creatures
                    .Where(creature => creature != null && creature.gameObject != null)
                    .OrderByDescending(delegate (KPrefabID creature)
                    {
                        if (!treeFilterable?.ContainsTag(creature.PrefabTag) ?? false)
                            return 1f;
                        var age = Age.Lookup(creature);
                        if (age == null)
                            return 0;
                        return age.value / age.GetMax();
                    }));
                dirty = false;
            }
        }

        public bool IsCreatureEligibleToBeButched(GameObject creature_go)
        {
            if (!creature_go.HasTag(creatureEligibleTag))
                return false;
            if (creature_go.HasTag(GameTags.Dying))
                return false;
            bool surplus = !treeFilterable?.ContainsTag(creature_go.GetComponent<KPrefabID>().PrefabTag) ?? false;
            if (autoButchSurplus && (surplus || storedCreatureCount > creatureLimit))
                return true;
            var age = Db.Get().Amounts.Age.Lookup(creature_go);
            if (age != null)
                return !surplus && ageButchThresold < age.value / age.GetMax();
            return false;
        }

        public static void ButchCreature(GameObject creature_go, bool moveCreatureToButcherStation = false)
        {
            var targetRanchStation = creature_go.GetSMI<RanchableMonitor.Instance>()?.targetRanchStation;
            if (targetRanchStation != null)
            {
                if (moveCreatureToButcherStation)
                {
                    creature_go.transform.SetPosition(targetRanchStation.transform.GetPosition());
                }

                var extraMeatSpawner = creature_go.GetComponent<ExtraMeatSpawner>();
                if (extraMeatSpawner != null)
                {
                    var smi = targetRanchStation.GetSMI<RancherChore.RancherChoreStates.Instance>();
                    var rancher = smi.sm.rancher.Get(smi);
                    extraMeatSpawner.onDeathDropMultiplier = rancher.GetAttributes().Get(Db.Get().Attributes.Ranching.Id).GetTotalValue() * Config.Get().EXTRAMEATPERRANCHINGATTRIBUTE;
                }
            }
            var deathMonitor = creature_go.GetSMI<DeathMonitor.Instance>();
            if (deathMonitor != null)
            {
                creature_go.AddTag(GameTags.Dying);
                deathMonitor.Kill(Db.Get().Deaths.Generic);
            }
        }

        // лимит количества жеготных
        float IUserControlledCapacity.UserMaxCapacity { get => creatureLimit; set => creatureLimit = Mathf.RoundToInt(value); }

        float IUserControlledCapacity.AmountStored => storedCreatureCount;
        float IUserControlledCapacity.MinCapacity => 0;
        float IUserControlledCapacity.MaxCapacity => Config.Get().MAXCREATURELIMIT;
        bool IUserControlledCapacity.WholeValues => true;
        LocString IUserControlledCapacity.CapacityUnits => UI.UISIDESCREENS.CAPTURE_POINT_SIDE_SCREEN.UNITS_SUFFIX;

        // ползун настройки максимального возраста
        string ISliderControl.SliderTitleKey => STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.TITLE.key.String;
        string ISliderControl.SliderUnits => UI.UNITSUFFIXES.PERCENT;

        float ISliderControl.GetSliderMax(int index)
        {
            return 100f;
        }

        float ISliderControl.GetSliderMin(int index)
        {
            return 0f;
        }

        string ISliderControl.GetSliderTooltip()
        {
            string s = string.Empty;
            foreach (float max_age in new float[] {
                TUNING.CREATURES.LIFESPAN.TIER1,
                TUNING.CREATURES.LIFESPAN.TIER2,
                TUNING.CREATURES.LIFESPAN.TIER3,
                TUNING.CREATURES.LIFESPAN.TIER4,
                    })
            {
                s = s + "\n" + (ageButchThresold * max_age).ToString("F1") /*Math.Floor(ageButchThresold * max_age)*/ + STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.TOOLTIP_OUTOF + Math.Floor(max_age) + STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.TOOLTIP_CYCLES;
            }
            return string.Format(STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.TOOLTIP, ageButchThresold * 100f) + s;
        }

        string ISliderControl.GetSliderTooltipKey(int index)
        {
            return STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.TOOLTIP.key.String;
        }

        float ISliderControl.GetSliderValue(int index)
        {
            return ageButchThresold * 100f;
        }

        void ISliderControl.SetSliderValue(float percent, int index)
        {
            ageButchThresold = percent / 100f;
        }

        int ISliderControl.SliderDecimalPlaces(int index)
        {
            return 0;
        }

        // флажёк "убивать лишних"
        string ICheckboxControl.CheckboxTitleKey => UI.UISIDESCREENS.CAPTURE_POINT_SIDE_SCREEN.TITLE.key.String;
        string ICheckboxControl.CheckboxLabel => UI.UISIDESCREENS.CAPTURE_POINT_SIDE_SCREEN.AUTOWRANGLE;
        string ICheckboxControl.CheckboxTooltip => UI.UISIDESCREENS.CAPTURE_POINT_SIDE_SCREEN.AUTOWRANGLE_TOOLTIP;

        bool ICheckboxControl.GetCheckboxValue()
        {
            return autoButchSurplus;
        }

        void ICheckboxControl.SetCheckboxValue(bool value)
        {
            autoButchSurplus = value;
        }
    }
}
