using System.Collections.Generic;
using System.Linq;
using Klei.AI;
using KSerialization;
using UnityEngine;
using PeterHan.PLib.Detours;

namespace ButcherStation
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class ButcherStation : KMonoBehaviour, ISim4000ms
    {
        public static readonly Tag ButcherableCreature = TagManager.Create("ButcherableCreature");
        public static readonly Tag FisherableCreature = TagManager.Create("FisherableCreature");

        public Tag creatureEligibleTag = ButcherableCreature;

        public static readonly Tag[] CreatureNotEligibleTags = new Tag[] { GameTags.Creatures.Bagged, GameTags.Trapped, GameTags.Creatures.Die, GameTags.Dead };

        public const int CREATURELIMIT = 40;
        public const float EXTRAMEATPERRANCHINGATTRIBUTE = 0.025f;

        [Serialize]
        internal int creatureLimit = ModOptions.Instance.max_creature_limit;
        private int storedCreatureCount;
        internal List<KPrefabID> CachedCreatures { get; private set; } = new List<KPrefabID>();
        private bool dirty = true;

        [SerializeField]
        public bool isExteriorTargetRanchCell = false;

        [Serialize]
        internal float ageButchThresold = 0.85f;

        [Serialize]
        internal bool wrangleUnSelected = false;// ловить лишних не выбранных в фильтре

        [Serialize]
        internal bool wrangleOldAged = true;    // ловить старых

        [Serialize]
        internal bool wrangleSurplus = false;   // ловить лишних избыточных

        [Serialize]
        internal bool leaveAlive = false;       // оставить живым

        [SerializeField]
        internal bool allowLeaveAlive = false;  // показывать "оставить живым" в сидэскреене

#pragma warning disable CS0649
        [MyCmpReq]
        Operational operational;

        [MyCmpReq]
        TreeFilterable treeFilterable;

        [MySmiReq]
        RanchStation.Instance ranchStation;
#pragma warning restore CS0649

        private static StatusItem capacityStatusItem;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            if (capacityStatusItem == null)
            {
                capacityStatusItem = new StatusItem("CritterCapacity", "BUILDING", string.Empty, StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID);
                string unit1 = Strings.Get("STRINGS.BUILDING.STATUSITEMS.CRITTERCAPACITY.UNIT");
                string unit2 = Strings.Get("STRINGS.BUILDING.STATUSITEMS.CRITTERCAPACITY.UNITS");
                capacityStatusItem.resolveStringCallback = (string str, object data) =>
                {
                    var butcherStation = (ButcherStation)data;
                    string stored = Util.FormatWholeNumber(butcherStation.storedCreatureCount);
                    string capacity = Util.FormatWholeNumber(butcherStation.creatureLimit);
                    return str.Replace("{Stored}", stored).Replace("{Capacity}", capacity)
                        .Replace("{StoredUnits}", (butcherStation.storedCreatureCount == 1) ? unit1 : unit2)
                        .Replace("{CapacityUnits}", (butcherStation.creatureLimit == 1) ? unit1 : unit2);
                };
            }
            GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, capacityStatusItem, this);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.CopySettings, OnCopySettings);
            treeFilterable.OnFilterChanged += OnFilterChanged;
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.CopySettings, OnCopySettings);
            treeFilterable.OnFilterChanged -= OnFilterChanged;
            base.OnCleanUp();
        }

        private void OnCopySettings(object data)
        {
            var go = (GameObject)data;
            if (go != null && go.TryGetComponent<ButcherStation>(out var butcherStation))
            {
                creatureLimit = butcherStation.creatureLimit;
                ageButchThresold = butcherStation.ageButchThresold;
                wrangleUnSelected = butcherStation.wrangleUnSelected;
                wrangleOldAged = butcherStation.wrangleOldAged;
                wrangleSurplus = butcherStation.wrangleSurplus;
                leaveAlive = allowLeaveAlive && butcherStation.leaveAlive;
                dirty = true;
            }
        }

        private void OnFilterChanged(HashSet<Tag> _)
        {
            dirty = true;
            RefreshCreatures();
        }

        public void Sim4000ms(float dt)
        {
            dirty = true;
            // если IsOperational то обновление вызывается из патча RanchStation.Instance.FindRanchable
            if (!operational.IsOperational)
                RefreshCreatures();
        }

        private static readonly IDetouredField<RanchStation.Instance, Room> ranchRoom = PDetours.DetourField<RanchStation.Instance, Room>("ranch");
        private static readonly List<KPrefabID> emptyList = new();

        internal void RefreshCreatures()
        {
            // обновляем число жеготных в комнате
            var cavity = (!isExteriorTargetRanchCell) ? ranchRoom.Get(ranchStation)?.cavity : null;
            cavity ??= Game.Instance.roomProber.GetCavityForCell(ranchStation.GetTargetRanchCell());
            var creatures = cavity?.creatures ?? emptyList;

            int old = storedCreatureCount;
            storedCreatureCount = 0;
            bool filteredCount = ModOptions.Instance.filtered_count;
            foreach (KPrefabID creature in creatures)
            {
                if (!creature.HasAnyTags(CreatureNotEligibleTags) && (!filteredCount || treeFilterable.AcceptedTags.Contains(creature.PrefabTag)))
                {
                    storedCreatureCount++;
                }
            }
            if (old != storedCreatureCount)
                dirty = true;
            if (dirty)
            {
                // для оптимизации очереди убиения упорядочиваем список жеготных.
                // вначале идут не выбранные в фильтре, затем по возрасту.
                CachedCreatures.Clear();
                var Age = Db.Get().Amounts.Age;
                CachedCreatures.AddRange(creatures
                    .Where(creature => creature != null && creature.gameObject != null)
                    .OrderByDescending(delegate (KPrefabID creature)
                    {
                        if (!treeFilterable.ContainsTag(creature.PrefabTag))
                            return 1f;
                        var age = Age.Lookup(creature);
                        if (age == null)
                            return 0;
                        return age.value / age.GetMax();
                    }));
                dirty = false;
                ranchStation.ValidateTargetRanchables();
            }
        }

        public bool IsCreatureEligibleToBeButched(GameObject creature_go)
        {
            creature_go.TryGetComponent<KPrefabID>(out var kPrefabID);
            if (!kPrefabID.HasTag(creatureEligibleTag) || kPrefabID.HasAnyTags(CreatureNotEligibleTags))
                return false;
            bool unSelected = !treeFilterable.ContainsTag(kPrefabID.PrefabTag);
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

        public static bool IsCreatureEligibleToBeButchedCB(GameObject creature_go, RanchStation.Instance ranch_station_smi)
        {
            return !ranch_station_smi.IsNullOrStopped()
                && ranch_station_smi.gameObject.TryGetComponent<ButcherStation>(out var butcherStation)
                    && butcherStation.IsCreatureEligibleToBeButched(creature_go);
        }

        private static readonly HashSet<string> meats = new()
        {
            MeatConfig.ID,
            FishMeatConfig.ID,
            ShellfishMeatConfig.ID,
            TallowConfig.ID,
            DinosaurMeatConfig.ID,
            PrehistoricPacuFilletConfig.ID,
        };

        public static void ButchCreature(GameObject creature_go, WorkerBase worker, bool moveCreatureToButcherStation = false)
        {
            bool kill = true;
            var targetRanchStation = creature_go.GetSMI<RanchableMonitor.Instance>()?.TargetRanchStation;
            if (targetRanchStation != null)
            {
                if (moveCreatureToButcherStation)
                {
                    int cell = Grid.PosToCell(targetRanchStation.transform.GetPosition());
                    creature_go.transform.SetPosition(Grid.CellToPosCCC(cell, Grid.SceneLayer.Creatures));
                }
                if (targetRanchStation.gameObject.TryGetComponent<ButcherStation>(out var butcherStation) && butcherStation.leaveAlive)
                {
                    kill = false;
                    if (creature_go.TryGetComponent<Baggable>(out var baggable))
                        baggable.SetWrangled();
                }
                if (kill)
                {
                    if (worker != null && creature_go.TryGetComponent<Butcherable>(out var butcherable)
                        && butcherable.drops != null && butcherable.drops.Count > 0)
                    {
                        var multiplier = 1 + Patches.RanchingEffectExtraMeat.Lookup(worker).Evaluate();
                        foreach (var drop in butcherable.drops.Keys.ToArray())
                        {
                            if (meats.Contains(drop))
                                butcherable.drops[drop] *= multiplier;
                        }
                    }
                    creature_go.GetSMI<DeathMonitor.Instance>()?.Kill(Db.Get().Deaths.Generic);
                }
            }
            if (creature_go.TryGetComponent<CreatureBrain>(out var brain))
                Game.BrainScheduler.PrioritizeBrain(brain);
        }
    }
}
