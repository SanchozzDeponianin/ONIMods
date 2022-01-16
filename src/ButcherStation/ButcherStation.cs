using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Klei.AI;
using KSerialization;
using STRINGS;
using UnityEngine;

namespace ButcherStation
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class ButcherStation : KMonoBehaviour, ISim4000ms
    {
        public static readonly Tag ButcherableCreature = TagManager.Create("ButcherableCreature");
        public static readonly Tag FisherableCreature = TagManager.Create("FisherableCreature");

        public Tag creatureEligibleTag = ButcherableCreature;

        public const int CREATURELIMIT = 20;
        public const float EXTRAMEATPERRANCHINGATTRIBUTE = 0.025f;

        [Serialize]
        internal int creatureLimit = ButcherStationOptions.Instance.max_creature_limit;
        private int storedCreatureCount;
        internal List<KPrefabID> Creatures { get; private set; } = new List<KPrefabID>();
        private bool dirty = true;

        [Serialize]
        internal float ageButchThresold = 0.85f;

        [Obsolete]
        [Serialize]
        private bool autoButchSurplus = false;

        [Serialize]
        internal bool wrangleUnSelected = false;// ловить лишних не выбранных в фильтре

        [Serialize]
        internal bool wrangleOldAged = true;    // ловить старых

        [Serialize]
        internal bool wrangleSurplus = false;   // ловить лишних избыточных

        [Serialize]
        internal bool leaveAlive = false;       // оставить живым

        [SerializeField]
        internal bool allowLeaveAlive = false;

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
                        var butcherStation = (ButcherStation)data;
                        string stored = Util.FormatWholeNumber(butcherStation.storedCreatureCount);
                        string capacity = Util.FormatWholeNumber(ButcherStationOptions.Instance.max_creature_limit);
                        return str.Replace("{Stored}", stored).Replace("{Capacity}", capacity).Replace("{Units}", UI.UISIDESCREENS.CAPTURE_POINT_SIDE_SCREEN.UNITS_SUFFIX);
                    }
                };
            }
            GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, capacityStatusItem, this);
        }

        // подгружаем старый параметр из прошлых версий
        [OnDeserialized]
        private void OnDeserialized()
        {
#pragma warning disable CS0612
            if (autoButchSurplus)
            {
                wrangleUnSelected = true;
                wrangleSurplus = true;
                autoButchSurplus = false;
            }
#pragma warning restore CS0612
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
                wrangleUnSelected = butcherStation.wrangleUnSelected;
                wrangleOldAged = butcherStation.wrangleOldAged;
                wrangleSurplus = butcherStation.wrangleSurplus;
                leaveAlive = allowLeaveAlive && butcherStation.leaveAlive;
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
                if (!creature.HasTag(GameTags.Creatures.Bagged) && !creature.HasTag(GameTags.Trapped) && !creature.HasTag(GameTags.Creatures.Die) && !creature.HasTag(GameTags.Dead))
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
            if (!creature_go.HasTag(creatureEligibleTag) || creature_go.HasTag(GameTags.Creatures.Die) || creature_go.HasTag(GameTags.Dead))
                return false;
            bool unSelected = !treeFilterable?.ContainsTag(creature_go.GetComponent<KPrefabID>().PrefabTag) ?? false;
            if (unSelected && wrangleUnSelected)
                return true;
            if (!unSelected && wrangleSurplus && storedCreatureCount > creatureLimit)
                return true;
            if (!unSelected && wrangleOldAged)
            {
                var age = Db.Get().Amounts.Age.Lookup(creature_go);
                if (age != null)
                    return ageButchThresold < age.value / age.GetMax();
            }
            return false;
        }

        public static void ButchCreature(GameObject creature_go, bool moveCreatureToButcherStation = false)
        {
            bool kill = true;
            var targetRanchStation = creature_go.GetSMI<RanchableMonitor.Instance>()?.targetRanchStation;
            if (targetRanchStation != null)
            {
                if (moveCreatureToButcherStation)
                {
                    int cell = Grid.PosToCell(targetRanchStation.transform.GetPosition());
                    creature_go.transform.SetPosition(Grid.CellToPosCCC(cell, Grid.SceneLayer.Creatures));
                }
                var extraMeatSpawner = creature_go.GetComponent<ExtraMeatSpawner>();
                if (extraMeatSpawner != null)
                {
                    var smi = targetRanchStation.GetSMI<RancherChore.RancherChoreStates.Instance>();
                    var rancher = smi.sm.rancher.Get(smi);
                    extraMeatSpawner.onDeathDropMultiplier = rancher.GetAttributes().Get(Db.Get().Attributes.Ranching.Id).GetTotalValue() * ButcherStationOptions.Instance.extra_meat_per_ranching_attribute / 100f;
                }
                var butcherStation = targetRanchStation.GetComponent<ButcherStation>();
                if (butcherStation != null && butcherStation.leaveAlive)
                {
                    creature_go.GetComponent<Baggable>()?.SetWrangled();
                    kill = false;
                }
            }
            if (kill)
                creature_go.GetSMI<DeathMonitor.Instance>()?.Kill(Db.Get().Deaths.Generic);
        }
    }
}
