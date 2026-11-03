using System;
using System.Collections.Generic;
using STRINGS;
using KSerialization;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace RoverRefueling
{
    public class RoverFuelCargoBay : KMonoBehaviour, IUserControlledCapacity, ISidescreenButtonControl
    {
        [SerializeField]
        public Tag fuelTag;

        [SerializeField]
        public Storage storage;

        [SerializeField]
        public CargoBay.CargoType storageType;

        [SerializeField]
        public List<Tag> discoverResourcesOnSpawn;

        [MyCmpReq]
        public TreeFilterable TreeFilterable;

        private MeterController meter;
        private FilteredStorage filteredStorage;

        [Serialize]
        private bool fillEnable;
        public bool FillEnable => fillEnable;

        [Serialize]
        private float userMaxCapacity;
        public float UserMaxCapacity
        {
            get => userMaxCapacity;
            set
            {
                userMaxCapacity = value;
                Trigger((int)GameHashes.StorageCapacityChanged, this);
                filteredStorage.FilterChanged();
            }
        }
        public float MinCapacity => 0f;
        public float MaxCapacity => storage.capacityKg;
        public float AmountStored => storage.GetMassAvailable(fuelTag);
        public bool WholeValues => false;
        public LocString CapacityUnits => GameUtil.GetCurrentMassUnit(false);
        public float RemainingCapacity => userMaxCapacity - AmountStored;

        protected override void OnPrefabInit()
        {
            fillEnable = ModOptions.Instance.fuel_cargo_bay_fill_enable;
            userMaxCapacity = MaxCapacity;
            filteredStorage = new FilteredStorage(this, null, this, false, Db.Get().ChoreTypes.Fetch);
            filteredStorage.SetHasMeter(false);
            filteredStorage.FilterChanged();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            userMaxCapacity = Mathf.Min(userMaxCapacity, MaxCapacity);
            if (discoverResourcesOnSpawn != null)
            {
                foreach (var tag in discoverResourcesOnSpawn)
                {
                    var element = ElementLoader.GetElement(tag);
                    if (element != null)
                        DiscoveredResources.Instance.Discover(element.tag, element.GetMaterialCategoryTag());
                }
            }
            meter = new MeterController(GetComponent<KBatchedAnimController>(),
                "meter_target", "meter", Meter.Offset.Infront, Grid.SceneLayer.NoLayer,
                "meter_target", "meter_fill", "meter_frame", "meter_back", "meter_highlight", "meter_shadow");
            var tracker = meter.gameObject.GetComponent<KBatchedAnimTracker>();
            tracker.matchParentOffset = true;
            tracker.forceAlwaysAlive = true;
            OnStorageChange(null);
            UpdateFilteredStorageEnabled();
            this.SubscribeX(GameHashes.OnStorageChange, OnStorageChangeDelegate);
            this.SubscribeX(GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
            this.SubscribeX(GameHashes.CopySettings, OnCopySettingsDelegate);
            this.SubscribeX(GameHashes.RocketLanded, OnRocketLandedDelegate);
        }

        protected override void OnCleanUp()
        {
            this.UnsubscribeAll();
            base.OnCleanUp();
        }

        public void DropExcess()
        {
            float excess = Mathf.Max(0f, AmountStored - UserMaxCapacity);
            if (excess > 0f)
                storage.DropSome(fuelTag, excess);
        }

        private void DropAll()
        {
            storage.Drop(fuelTag);
        }

        private static readonly Action<object, object> OnRocketLandedDelegate = (cmp, data) => ((RoverFuelCargoBay)cmp).filteredStorage.FilterChanged();
        private static readonly Action<object, object> OnRefreshUserMenuDelegate = (cmp, data) => ((RoverFuelCargoBay)cmp).OnRefreshUserMenu(data);
        private void OnRefreshUserMenu(object data)
        {
            var button = new KIconButtonMenu.ButtonInfo(
                iconName: "action_empty_contents",
                text: UI.USERMENUACTIONS.EMPTYSTORAGE.NAME,
                tooltipText: UI.USERMENUACTIONS.EMPTYSTORAGE.TOOLTIP,
                shortcutKey: Utils.MaxAction,
                on_click: () => DropAll());
            Game.Instance.userMenu.AddButton(gameObject, button, 1f);
        }

        private static readonly Action<object, object> OnCopySettingsDelegate = (cmp, data) => ((RoverFuelCargoBay)cmp).OnCopySettings(data);
        private void OnCopySettings(object data)
        {
            if (((GameObject)data).TryGetComponent<RoverFuelCargoBay>(out var other))
                UserMaxCapacity = other.UserMaxCapacity;
        }

        private static readonly Action<object, object> OnStorageChangeDelegate = (cmp, data) => ((RoverFuelCargoBay)cmp).OnStorageChange(data);
        private void OnStorageChange(object data)
        {
            meter.SetPositionPercent(AmountStored / MaxCapacity);
            if (RemainingCapacity <= 0f)
                filteredStorage.FilterChanged();
        }

        private void UpdateFilteredStorageEnabled()
        {
            // эта херь не имеет возможности отключения, придется запрещать доставку
            if (fillEnable)
                filteredStorage.RemoveForbiddenTag(fuelTag);
            else
                filteredStorage.AddForbiddenTag(fuelTag);
        }

        public string SidescreenButtonText
        {
            get
            {
                if (fillEnable)
                    return string.Format(UI.UISIDESCREENS.BUTTONMENUSIDESCREEN.DISALLOW_INTERNAL_CONSTRUCTOR.text,
                        fuelTag.ProperName());
                else
                    return string.Format(UI.UISIDESCREENS.BUTTONMENUSIDESCREEN.ALLOW_INTERNAL_CONSTRUCTOR.text,
                        fuelTag.ProperName());
            }
        }

        public string SidescreenButtonTooltip
        {
            get
            {
                if (fillEnable)
                    return string.Format(UI.UISIDESCREENS.BUTTONMENUSIDESCREEN.DISALLOW_INTERNAL_CONSTRUCTOR_TOOLTIP.text,
                        fuelTag.ProperName());
                else
                    return string.Format(UI.UISIDESCREENS.BUTTONMENUSIDESCREEN.ALLOW_INTERNAL_CONSTRUCTOR_TOOLTIP.text,
                        fuelTag.ProperName());
            }
        }

        public void OnSidescreenButtonPressed()
        {
            fillEnable = !fillEnable;
            UpdateFilteredStorageEnabled();
        }

        public void SetButtonTextOverride(ButtonMenuTextOverride textOverride) { }
        public bool SidescreenEnabled() => true;
        public bool SidescreenButtonInteractable() => true;
        public int ButtonSideScreenSortOrder() => 20;
        public int HorizontalGroupID() => -1;
    }
}
