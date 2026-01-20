using System.Collections.Generic;
using KSerialization;
using STRINGS;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace AutoComposter
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class AutoComposter : KMonoBehaviour, IUserControlledCapacity
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private KSelectable selectable;

        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private Storage storage;

        [MyCmpReq]
        private ManualDeliveryKG delivery;

        [MyCmpReq]
        private TreeFilterable filterable;
#pragma warning restore CS0649

        private FilteredStorage filtered;

        private Storage garbage;

        [Serialize]
        private bool paused = false;

        [Serialize]
        private bool forbidMutantSeeds = false;

        private static StatusItem mutantSeedStatusItem;
        private Tag forbiddenMutantTag = GameTags.MutatedSeed;
        private Tag OutputTag = GameTags.Dirt;

        public bool IsOperational => operational.IsOperational;

        public override void OnPrefabInit()
        {
            if (mutantSeedStatusItem == null)
            {
                mutantSeedStatusItem = new StatusItem("COMPOSTACCEPTSMUTANTSEEDS", "BUILDING", "",
                    StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID, false);
            }
            base.OnPrefabInit();
            delivery.Pause(true, "filtered");
            if (TryGetComponent(out ElementConverter converter) && converter.outputElements.Length > 0)
                OutputTag = converter.outputElements[0].elementHash.CreateTag();
            foreach (var storage in GetComponents<Storage>())
            {
                if (storage.storageID == GameTags.Garbage)
                    garbage = storage;
            }
            storage.OnStorageChange += OnStorageChange;
            garbage.OnStorageChange += OnGarbageChange;
            filterable.AcceptedTags.UnionWith(Patches.DirectlyCompostables);
            filterable.AcceptedTags.UnionWith(Patches.CanBeMarkedCompostables);
            var chore_type = Db.Get().ChoreTypes.Get(delivery.choreTypeIDHash);
            filtered = new(this, null, this, false, chore_type);
            filtered.storage = garbage;
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
            Subscribe((int)GameHashes.OnStorageInteracted, OnStorageInteractedDelegate);
            Subscribe((int)GameHashes.OperationalChanged, filtered.OnFunctionalChanged);
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            ForbidMutantSeeds = ForbidMutantSeeds;
            OnFilterChanged(filterable.AcceptedTags);
            filterable.OnFilterChanged += OnFilterChanged;
            CheckPause();
            DiscoveredResources.Instance.Discover(SimHashes.ToxicSand.CreateTag());
        }

        public override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            Unsubscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
            Unsubscribe((int)GameHashes.OnStorageInteracted, OnStorageInteractedDelegate);
            Unsubscribe((int)GameHashes.OperationalChanged, filtered.OnFunctionalChanged);
            if (storage != null)
                storage.OnStorageChange -= OnStorageChange;
            if (garbage != null)
                garbage.OnStorageChange -= OnGarbageChange;
            if (filterable != null)
                filterable.OnFilterChanged -= OnFilterChanged;
            filtered.CleanUp();
            base.OnCleanUp();
        }

        private bool isBeingProcessedFilter = false;

        private void OnFilterChanged(HashSet<Tag> filter)
        {
            if (isBeingProcessedFilter)
                return;
            isBeingProcessedFilter = true;
            var new_filter = HashSetPool<Tag, TreeFilterable>.Allocate();
            new_filter.UnionWith(filter);
            new_filter.UnionWith(Patches.CanBeMarkedCompostables);
            filterable.UpdateFilters(new_filter);
            new_filter.Recycle();
            isBeingProcessedFilter = false;
            DropUnFilteredItems();
        }

        private void DropUnFilteredItems()
        {
            for (int i = storage.items.Count - 1; i >= 0; i--)
            {
                var go = storage.items[i];
                if (go != null && go.TryGetComponent(out KPrefabID item) && item.PrefabTag != OutputTag
                    && !filterable.ContainsTag(item.PrefabTag))
                {
                    storage.Drop(go, true);
                }
            }
        }

        private static readonly EventSystem.IntraObjectHandler<AutoComposter> OnStorageInteractedDelegate =
            new((component, data) => component.OnStorageInteracted(data));

        private void OnStorageInteracted(object data)
        {
            if (data is Storage storage && storage != null && storage.storageID == GameTags.Compostable)
                CheckPause();
        }

        private void OnStorageChange(object _) => CheckPause();

        private void CheckPause()
        {
            var mass = storage.MassStored();
            if ((!paused && (mass >= delivery.Capacity - storage.storageFullMargin))
                || (paused && (mass < delivery.refillMass)))
            {
                paused = !paused;
                filtered.FilterChanged();
            }
        }

        private void OnGarbageChange(object _) => MarkForCompost();

        private bool isBeingProcessedItems = false;

        private void MarkForCompost()
        {
            if (isBeingProcessedItems)
                return;
            isBeingProcessedItems = true;
            for (int i = garbage.items.Count - 1; i >= 0; i--)
            {
                var go = garbage.items[i];
                if (go != null)
                {
                    if (go.TryGetComponent(out Compostable compostable) && !compostable.isMarkedForCompost
                        && go.TryGetComponent(out Pickupable pickupable))
                    {
                        garbage.Drop(go);
                        var compost = EntitySplitter.Split(pickupable, pickupable.TotalAmount, compostable.compostPrefab);
                        if (compost != null)
                        {
                            DiscoveredResources.Instance.Discover(compost.KPrefabID.PrefabTag);
                            storage.Store(compost.gameObject, true);
                        }
                    }
                    else
                        garbage.Transfer(go, storage, false, true);
                }
            }
            isBeingProcessedItems = false;
        }

        private static readonly EventSystem.IntraObjectHandler<AutoComposter> OnCopySettingsDelegate =
            new((component, data) => component.OnCopySettings(data));

        private void OnCopySettings(object data)
        {
            var go = data as GameObject;
            if (go != null && go.TryGetComponent(out AutoComposter other))
            {
                ForbidMutantSeeds = other.ForbidMutantSeeds;
                OnFilterChanged(other.filterable.AcceptedTags);
            }
        }

        private static readonly EventSystem.IntraObjectHandler<AutoComposter> OnRefreshUserMenuDelegate
            = new((component, data) => component.OnRefreshUserMenu());

        private void OnRefreshUserMenu()
        {
            if (Game.IsDlcActiveForCurrentSave(DlcManager.EXPANSION1_ID))
            {
                Game.Instance.userMenu.AddButton(gameObject, new KIconButtonMenu.ButtonInfo(
                    iconName: "action_switch_toggle",
                    text: ForbidMutantSeeds ? UI.USERMENUACTIONS.ACCEPT_MUTANT_SEEDS.ACCEPT : UI.USERMENUACTIONS.ACCEPT_MUTANT_SEEDS.REJECT,
                    tooltipText: UI.USERMENUACTIONS.ACCEPT_MUTANT_SEEDS.TOOLTIP,
                    on_click: () => { ForbidMutantSeeds = !ForbidMutantSeeds; OnRefreshUserMenu(); },
                    shortcutKey: Utils.MaxAction));
            }
        }

        public bool ForbidMutantSeeds
        {
            get => forbidMutantSeeds;
            set
            {
                forbidMutantSeeds = value;
                if (forbidMutantSeeds)
                {
                    if (storage.GetMassAvailable(forbiddenMutantTag) > 0f)
                        storage.Drop(forbiddenMutantTag);
                    filtered.AddForbiddenTag(forbiddenMutantTag);
                }
                else
                    filtered.RemoveForbiddenTag(forbiddenMutantTag);
                selectable.ToggleStatusItem(mutantSeedStatusItem, Game.IsDlcActiveForCurrentSave(DlcManager.EXPANSION1_ID) && !forbidMutantSeeds);
            }
        }

        // IUserControlledCapacity
        public bool ControlEnabled() => false;
        public float UserMaxCapacity { get => paused ? 0f : delivery.Capacity; set { } }
        public float AmountStored => storage.MassStored();
        public float MinCapacity => 0f;
        public float MaxCapacity => storage.capacityKg;
        public bool WholeValues => false;
        public LocString CapacityUnits => GameUtil.GetCurrentMassUnit(false);
    }
}
