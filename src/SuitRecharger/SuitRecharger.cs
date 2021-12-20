using System;
using System.Collections.Generic;
using Klei.CustomSettings;
using KSerialization;
using TUNING;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using static STRINGS.DUPLICANTS.CHORES;
using static SuitRecharger.STRINGS.DUPLICANTS.CHORES.PRECONDITIONS;

namespace SuitRecharger
{
    public class SuitRecharger : StateMachineComponent<SuitRecharger.StatesInstance>, ISaveLoadable, ISecondaryInput, ISecondaryOutput, ISliderControl, ISingleSliderControl
    {
        private static readonly EventSystem.IntraObjectHandler<SuitRecharger> CheckPipesDelegate =
            new EventSystem.IntraObjectHandler<SuitRecharger>((SuitRecharger component, object data) => component.CheckPipes(data));

        private static readonly EventSystem.IntraObjectHandler<SuitRecharger> OnStorageChangeDelegate =
            new EventSystem.IntraObjectHandler<SuitRecharger>(
                (SuitRecharger component, object data) => component.OnStorageChange(data));

        private static readonly EventSystem.IntraObjectHandler<SuitRecharger> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<SuitRecharger>(
                (SuitRecharger component, object data) => component.OnCopySettings(data));

        // проверка что костюм действительно надет
        private static readonly Chore.Precondition IsSuitEquipped = new Chore.Precondition
        {
            id = nameof(IsSuitEquipped),
            description = IS_SUIT_EQUIPPED,
            sortOrder = -1,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                return context.consumerState.prefabid.HasTag(GameTags.HasSuitTank);
            }
        };

        // todo: возможно стоит проверять остаток массы для конкретного костюма, а не требовать возможность полной заправки
        // todo: возможно эти прекондиции неоптимальны по быстродействию. нужно обдумать

        // проверка костюм имеет достаточную прочность чтобы не сломаться в процессе зарядки
        private static readonly Chore.Precondition IsSuitHasEnoughDurability = new Chore.Precondition
        {
            id = nameof(IsSuitHasEnoughDurability),
            description = IS_SUIT_HAS_ENOUGH_DURABILITY,
            sortOrder = 5,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                bool result = true;
                var recharger = (SuitRecharger)data;
                if (recharger != null)
                {
                    var slot = context.consumerState.equipment?.GetSlot(Db.Get().AssignableSlots.Suit);
                    var durability = slot?.assignable?.GetComponent<Durability>();
                    if (durability != null && durability.GetTrueDurability(context.consumerState.resume) < recharger.durabilityThreshold)
                        result = false;
                }
                return result;
            }
        };

        // проверка что костюму требуется заправка.
        // скопировано из SuitLocker.ReturnSuitWorkable
        // отключена проверка уровня заряда свинцовых костюмов
        private static readonly Chore.Precondition DoesSuitNeedRecharging = new Chore.Precondition
        {
            id = nameof(DoesSuitNeedRecharging),
            description = PRECONDITIONS.DOES_SUIT_NEED_RECHARGING_URGENT,
            sortOrder = 1,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                bool result = false;
                var slot = context.consumerState.equipment?.GetSlot(Db.Get().AssignableSlots.Suit);
                var suit_tank = slot?.assignable?.GetComponent<SuitTank>();
                if (suit_tank != null)
                {
                    if (suit_tank.NeedsRecharging())
                        result = true;
                    else
                    {
                        var jet_suit_tank = slot?.assignable?.GetComponent<JetSuitTank>();
                        if (jet_suit_tank != null && jet_suit_tank.NeedsRecharging())
                            result = true;
                    }
                }
                return result;
            }
        };

        // проверка что кислорода достаточно для полной заправки 
        private static readonly Chore.Precondition IsEnoughOxygen = new Chore.Precondition
        {
            id = nameof(IsEnoughOxygen),
            description = IS_ENOUGH_OXYGEN,
            sortOrder = 2,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                bool result = true;
                var recharger = (SuitRecharger)data;
                if (recharger != null)
                {
                    var suit_tank = context.consumerState.equipment?.GetSlot(Db.Get().AssignableSlots.Suit)?.assignable?.GetComponent<SuitTank>();
                    if (suit_tank != null && suit_tank.NeedsRecharging() && recharger.OxygenAvailable < suit_tank.capacity)
                        result = false;
                }
                return result;
            }
        };

        // проверка что топлива достаточно для полной заправки
        // если одновременно требуется заправкa кислорода - то пофиг на топливо
        private static readonly Chore.Precondition IsEnoughFuel = new Chore.Precondition
        {
            id = nameof(IsEnoughFuel),
            description = IS_ENOUGH_FUEL,
            sortOrder = 3,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                bool result = true;
                var recharger = (SuitRecharger)data;
                if (recharger != null)
                {
                    var slot = context.consumerState.equipment?.GetSlot(Db.Get().AssignableSlots.Suit);
                    var jet_suit_tank = slot?.assignable?.GetComponent<JetSuitTank>();
                    if (jet_suit_tank != null && jet_suit_tank.NeedsRecharging() && recharger.FuelAvailable < JetSuitTank.FUEL_CAPACITY)
                    {
                        var suit_tank = slot?.assignable?.GetComponent<SuitTank>();
                        if (suit_tank != null && !suit_tank.NeedsRecharging())
                            result = false;
                    }
                }
                return result;
            }
        };

        internal static ChoreType RecoverBreathRecharge;
        internal static void Init()
        {
            var db = Db.Get();
            // воспользуемся неиспользуемой ChoreType, но подкрутим приоритеты
            var ReturnSuitUrgent = db.ChoreTypes.ReturnSuitUrgent;
            var Recharge = db.ChoreTypes.Recharge;
            var RechargeTraverse = Traverse.Create(Recharge);
            RechargeTraverse.Property<int>(nameof(ChoreType.priority)).Value = ReturnSuitUrgent.priority;
            RechargeTraverse.Property<int>(nameof(ChoreType.explicitPriority)).Value = ReturnSuitUrgent.explicitPriority;
            Recharge.interruptPriority = ReturnSuitUrgent.interruptPriority;
            // собственная ChoreType для суперсрочной подзарядки
            var RecoverBreath = db.ChoreTypes.RecoverBreath;
            RecoverBreathRecharge = new ChoreType(
                id: nameof(RecoverBreathRecharge),
                parent: db.ChoreTypes,
                chore_groups: new string[0],
                urge: RecoverBreath.urge.Id,
                name: RECOVERBREATH.NAME,
                status_message: RECOVERBREATH.STATUS,
                tooltip: RECHARGE.TOOLTIP,
                interrupt_exclusion: RecoverBreath.interruptExclusion,
                implicit_priority: RecoverBreath.priority,
                explicit_priority: RecoverBreath.explicitPriority)
            { interruptPriority = RecoverBreath.interruptPriority };
        }

        public class StatesInstance : GameStateMachine<States, StatesInstance, SuitRecharger, object>.GameInstance
        {
            public List<Chore> activeUseChores;
            public StatesInstance(SuitRecharger master) : base(master)
            {
                activeUseChores = new List<Chore>();
            }
        }

        public class States : GameStateMachine<States, StatesInstance, SuitRecharger>
        {
#pragma warning disable CS0649
            State operational;
            State notoperational;
#pragma warning restore CS0649

            public override void InitializeStates(out BaseState default_state)
            {
                default_state = notoperational;
                notoperational
                    .EventTransition(GameHashes.OperationalChanged, operational, smi => smi.master.operational.IsOperational);
                operational
                    .EventTransition(GameHashes.OperationalChanged, notoperational, smi => !smi.master.operational.IsOperational)
                    .ToggleRecurringChore(CreateNormalChore)
                    .ToggleRecurringChore(CreateRecoverBreathChore);
            }

            public Chore CreateNormalChore(StatesInstance smi)
            {
                return CreateUseChore(smi, Db.Get().ChoreTypes.Recharge, PriorityScreen.PriorityClass.personalNeeds);
            }

            public Chore CreateRecoverBreathChore(StatesInstance smi)
            {
                return CreateUseChore(smi, RecoverBreathRecharge, PriorityScreen.PriorityClass.compulsory);
            }

            public Chore CreateUseChore(StatesInstance smi, ChoreType choreType, PriorityScreen.PriorityClass priorityClass)
            {
                var chore = new WorkChore<SuitRechargerWorkable>(
                        chore_type: choreType,
                        target: smi.master.workable,
                        ignore_schedule_block: true,
                        only_when_operational: false,
                        allow_prioritization: false,
                        priority_class: priorityClass,
                        priority_class_value: Chore.DEFAULT_BASIC_PRIORITY,
                        add_to_daily_report: false);
                smi.activeUseChores.Add(chore);
                chore.onExit += (exiting_chore) => smi.activeUseChores.Remove(exiting_chore);
                chore.AddPrecondition(IsSuitEquipped, null);
                // todo: временно отключено для тестирования
                //if (durabilityEnabled)  // не проверять если износ отключен в настройках сложности
                chore.AddPrecondition(IsSuitHasEnoughDurability, smi.master);
                chore.AddPrecondition(DoesSuitNeedRecharging, null);
                chore.AddPrecondition(IsEnoughOxygen, smi.master);
                chore.AddPrecondition(IsEnoughFuel, smi.master);
                chore.AddPrecondition(ChorePreconditions.instance.IsExclusivelyAvailableWithOtherChores, smi.activeUseChores);
                return chore;
            }
        }

#pragma warning disable CS0649
        [MyCmpReq]
        private Building building;

        [MyCmpReq]
        private KSelectable selectable;

        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private Storage storage;

        [MyCmpAdd]
        private SuitRechargerWorkable workable;
#pragma warning restore CS0649

        // керосин
        [SerializeField]
        public ConduitPortInfo fuelPortInfo;
        private int fuelInputCell = Grid.InvalidCell;
        private FlowUtilityNetwork.NetworkItem fuelNetworkItem;
        private ConduitConsumer fuelConsumer;
        public Tag fuelTag { get; private set; }
        private static StatusItem fuelNoPipeConnectedStatusItem;

        // жидкие отходы
        [SerializeField]
        public ConduitPortInfo liquidWastePortInfo;
        private int liquidWasteOutputCell = Grid.InvalidCell;
        private FlowUtilityNetwork.NetworkItem liquidWasteNetworkItem;
        private ConduitDispenser liquidWasteDispenser;
        private bool liquidWastePipeBlocked;
        private Guid liquidWastePipeBlockedStatusItemGuid;
        public bool liquidWastePipeOK => !liquidWastePipeBlocked && liquidWasteDispenser.IsConnected;
        private static StatusItem liquidWastePipeBlockedStatusItem;
        private static StatusItem liquidWasteNoPipeConnectedStatusItem;

        // газообразные отходы
        [SerializeField]
        public ConduitPortInfo gasWastePortInfo;
        private int gasWasteOutputCell = Grid.InvalidCell;
        private FlowUtilityNetwork.NetworkItem gasWasteNetworkItem;
        private ConduitDispenser gasWasteDispenser;
        private bool gasWastePipeBlocked;
        private Guid gasWastePipeBlockedStatusItemGuid;
        public bool gasWastePipeOK => !gasWastePipeBlocked && gasWasteDispenser.IsConnected;
        private static StatusItem gasWastePipeBlockedStatusItem;
        private static StatusItem gasWasteNoPipeConnectedStatusItem;

        // порог изношенности костюма, при превышении не заряжать
        [Serialize]
        private float durabilityThreshold;
        static private float defaultDurabilityThreshold;
        static private float durabilityPerCycleGap = 0.2f;
        static internal bool durabilityEnabled => defaultDurabilityThreshold > 0f;

        private MeterController oxygenMeter;
        private MeterController fuelMeter;

        public float OxygenAvailable { get; private set; }
        public float FuelAvailable { get; private set; }

        static internal void CheckDifficultySetting()
        {
            var currentQualitySetting = CustomGameSettings.Instance.GetCurrentQualitySetting(CustomGameSettingConfigs.Durability);
            float durabilityLossDifficultyMod;
            switch (currentQualitySetting?.id)
            {
                case "Indestructible":
                    durabilityLossDifficultyMod = EQUIPMENT.SUITS.INDESTRUCTIBLE_DURABILITY_MOD;
                    break;
                case "Reinforced":
                    durabilityLossDifficultyMod = EQUIPMENT.SUITS.REINFORCED_DURABILITY_MOD;
                    break;
                case "Flimsy":
                    durabilityLossDifficultyMod = EQUIPMENT.SUITS.FLIMSY_DURABILITY_MOD;
                    break;
                case "Threadbare":
                    durabilityLossDifficultyMod = EQUIPMENT.SUITS.THREADBARE_DURABILITY_MOD;
                    break;
                default:
                    durabilityLossDifficultyMod = 1f;
                    break;
            }
            defaultDurabilityThreshold = Mathf.Abs(EQUIPMENT.SUITS.OXYGEN_MASK_DECAY * durabilityLossDifficultyMod * durabilityPerCycleGap);
        }

        private StatusItem CreateStatusItem(string id, string icon = "")
        {
            return new StatusItem(id: id,
                prefix: "BUILDING",
                icon: icon,
                icon_type: StatusItem.IconType.Custom,
                notification_type: NotificationType.BadMinor,
                allow_multiples: false,
                render_overlay: OverlayModes.None.ID,
                showWorldIcon: false);
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            durabilityThreshold = defaultDurabilityThreshold;

            if (fuelNoPipeConnectedStatusItem == null)
            {
                fuelNoPipeConnectedStatusItem = CreateStatusItem("fuelNoPipeConnected", "status_item_need_supply_in");
                fuelNoPipeConnectedStatusItem.resolveStringCallback = (string str, object data) => string.Format(str, (string)data);
            }
            if (liquidWasteNoPipeConnectedStatusItem == null)
                liquidWasteNoPipeConnectedStatusItem = CreateStatusItem("liquidWasteNoPipeConnected", "status_item_need_supply_out");
            if (liquidWastePipeBlockedStatusItem == null)
                liquidWastePipeBlockedStatusItem = CreateStatusItem("liquidWastePipeFull", "status_item_no_liquid_to_pump");
            if (gasWasteNoPipeConnectedStatusItem == null)
                gasWasteNoPipeConnectedStatusItem = CreateStatusItem("gasWasteNoPipeConnected", "status_item_need_supply_out");
            if (gasWastePipeBlockedStatusItem == null)
                gasWastePipeBlockedStatusItem = CreateStatusItem("gasWastePipeFull", "status_item_no_gas_to_pump");
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // вторичные входы и выходы для керосина и отходов
            fuelTag = SimHashes.Petroleum.CreateTag();
            fuelInputCell = GetSecondaryUtilityCell(fuelPortInfo.offset);
            fuelConsumer = CreateConduitConsumer(ConduitType.Liquid, fuelInputCell, out fuelNetworkItem);
            fuelConsumer.capacityTag = fuelTag;
            fuelConsumer.capacityKG = SuitRechargerConfig.FUEL_CAPACITY;

            liquidWasteOutputCell = GetSecondaryUtilityCell(liquidWastePortInfo.offset);
            liquidWasteDispenser = CreateConduitDispenser(ConduitType.Liquid, liquidWasteOutputCell, out liquidWasteNetworkItem);
            liquidWasteDispenser.elementFilter = new SimHashes[] { SimHashes.Petroleum };
            liquidWasteDispenser.invertElementFilter = true;

            gasWasteOutputCell = GetSecondaryUtilityCell(gasWastePortInfo.offset);
            gasWasteDispenser = CreateConduitDispenser(ConduitType.Gas, gasWasteOutputCell, out gasWasteNetworkItem);
            gasWasteDispenser.elementFilter = new SimHashes[] { SimHashes.Oxygen };
            gasWasteDispenser.invertElementFilter = true;

            // создаём метеры
            oxygenMeter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_target", "meter", Meter.Offset.Infront, Grid.SceneLayer.NoLayer, new string[] { "meter_target" });
            fuelMeter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_target_fuel", "meter_fuel", Meter.Offset.Infront, Grid.SceneLayer.NoLayer, new string[] { "meter_target_fuel" });

            Subscribe((int)GameHashes.ConduitConnectionChanged, CheckPipesDelegate);
            Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            Game.Instance.liquidConduitFlow.AddConduitUpdater(OnLiquidConduitUpdate, ConduitFlowPriority.Default);
            Game.Instance.gasConduitFlow.AddConduitUpdater(OnGasConduitUpdate, ConduitFlowPriority.Default);
            OnStorageChange();
            smi.StartSM();
        }

        protected override void OnCleanUp()
        {
            smi.StopSM("OnCleanUp");
            Game.Instance.liquidConduitFlow.RemoveConduitUpdater(OnLiquidConduitUpdate);
            Game.Instance.gasConduitFlow.RemoveConduitUpdater(OnGasConduitUpdate);
            Unsubscribe((int)GameHashes.ConduitConnectionChanged, CheckPipesDelegate);
            Unsubscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            Unsubscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            Conduit.GetNetworkManager(fuelPortInfo.conduitType).RemoveFromNetworks(fuelInputCell, fuelNetworkItem, true);
            Conduit.GetNetworkManager(liquidWastePortInfo.conduitType).RemoveFromNetworks(liquidWasteOutputCell, liquidWasteNetworkItem, true);
            Conduit.GetNetworkManager(gasWastePortInfo.conduitType).RemoveFromNetworks(gasWasteOutputCell, gasWasteNetworkItem, true);
            base.OnCleanUp();
        }

        private int GetSecondaryUtilityCell(CellOffset offset)
        {
            return Grid.OffsetCell(Grid.PosToCell(this), building.GetRotatedOffset(offset));
        }

        private ConduitConsumer CreateConduitConsumer(ConduitType inputType, int inputCell, out FlowUtilityNetwork.NetworkItem flowNetworkItem)
        {
            var consumer = gameObject.AddComponent<ConduitConsumer>();
            consumer.conduitType = inputType;
            consumer.useSecondaryInput = true;
            consumer.consumptionRate = inputType == ConduitType.Gas ? ConduitFlow.MAX_GAS_MASS : ConduitFlow.MAX_LIQUID_MASS;
            consumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            consumer.forceAlwaysSatisfied = true;
            consumer.storage = storage;
            var networkManager = Conduit.GetNetworkManager(inputType);
            flowNetworkItem = new FlowUtilityNetwork.NetworkItem(inputType, Endpoint.Sink, inputCell, gameObject);
            networkManager.AddToNetworks(inputCell, flowNetworkItem, true);
            return consumer;
        }

        private ConduitDispenser CreateConduitDispenser(ConduitType outputType, int outputCell, out FlowUtilityNetwork.NetworkItem flowNetworkItem)
        {
            var dispenser = gameObject.AddComponent<AlwaysFunctionalConduitDispenser>();
            dispenser.conduitType = outputType;
            dispenser.useSecondaryOutput = true;
            dispenser.alwaysDispense = true;
            dispenser.storage = storage;
            var networkManager = Conduit.GetNetworkManager(outputType);
            flowNetworkItem = new FlowUtilityNetwork.NetworkItem(outputType, Endpoint.Source, outputCell, gameObject);
            networkManager.AddToNetworks(outputCell, flowNetworkItem, true);
            return dispenser;
        }

        private void OnLiquidConduitUpdate(float dt)
        {
            var flow = Game.Instance.liquidConduitFlow;
            liquidWastePipeBlocked = flow.HasConduit(liquidWasteOutputCell) && flow.GetContents(liquidWasteOutputCell).mass > 0f;
            liquidWastePipeBlockedStatusItemGuid = selectable.ToggleStatusItem(liquidWastePipeBlockedStatusItem, liquidWastePipeBlockedStatusItemGuid, liquidWastePipeBlocked);
        }

        private void OnGasConduitUpdate(float dt)
        {
            var flow = Game.Instance.gasConduitFlow;
            gasWastePipeBlocked = flow.HasConduit(gasWasteOutputCell) && flow.GetContents(gasWasteOutputCell).mass > 0f;
            gasWastePipeBlockedStatusItemGuid = selectable.ToggleStatusItem(gasWastePipeBlockedStatusItem, gasWastePipeBlockedStatusItemGuid, gasWastePipeBlocked);
        }

        private void CheckPipes(object data)
        {
            selectable.ToggleStatusItem(fuelNoPipeConnectedStatusItem, !fuelConsumer.IsConnected, fuelTag.ProperName());
            selectable.ToggleStatusItem(liquidWasteNoPipeConnectedStatusItem, !liquidWasteDispenser.IsConnected);
            selectable.ToggleStatusItem(gasWasteNoPipeConnectedStatusItem, !gasWasteDispenser.IsConnected);
        }

        private void OnCopySettings(object data)
        {
            var recharger = ((GameObject)data)?.GetComponent<SuitRecharger>();
            if (recharger != null)
                durabilityThreshold = recharger.durabilityThreshold;
        }

        private void OnStorageChange(object data = null)
        {
            OxygenAvailable = storage.GetMassAvailable(GameTags.Oxygen);
            FuelAvailable = storage.GetMassAvailable(fuelTag);
            RefreshMeter();
        }

        private void RefreshMeter()
        {
            oxygenMeter.SetPositionPercent(Mathf.Clamp01(OxygenAvailable / SuitRechargerConfig.O2_CAPACITY));
            fuelMeter.SetPositionPercent(Mathf.Clamp01(FuelAvailable / SuitRechargerConfig.FUEL_CAPACITY));
        }

        bool ISecondaryInput.HasSecondaryConduitType(ConduitType type)
        {
            return type == fuelPortInfo.conduitType;
        }

        CellOffset ISecondaryInput.GetSecondaryConduitOffset(ConduitType type)
        {
            if (type == fuelPortInfo.conduitType)
                return fuelPortInfo.offset;
            return CellOffset.none;
        }

        bool ISecondaryOutput.HasSecondaryConduitType(ConduitType type)
        {
            return type == liquidWastePortInfo.conduitType || type == gasWastePortInfo.conduitType;
        }

        CellOffset ISecondaryOutput.GetSecondaryConduitOffset(ConduitType type)
        {
            if (type == liquidWastePortInfo.conduitType)
                return liquidWastePortInfo.offset;
            if (type == gasWastePortInfo.conduitType)
                return gasWastePortInfo.offset;
            return CellOffset.none;
        }
        string ISliderControl.SliderTitleKey => "STRINGS.UI.UISIDESCREENS.SUITRECHARGERSIDESCREEN.TITLE";
        string ISliderControl.SliderUnits => global::STRINGS.UI.UNITSUFFIXES.PERCENT;
        int ISliderControl.SliderDecimalPlaces(int index) => 0;
        float ISliderControl.GetSliderMin(int index) => 0f;
        float ISliderControl.GetSliderMax(int index) => 100f;
        float ISliderControl.GetSliderValue(int index) => durabilityThreshold * 100f;
        void ISliderControl.SetSliderValue(float percent, int index) => durabilityThreshold = Mathf.RoundToInt(percent) / 100f;
        string ISliderControl.GetSliderTooltipKey(int index) => string.Empty;
        string ISliderControl.GetSliderTooltip() => string.Format(STRINGS.UI.UISIDESCREENS.SUITRECHARGERSIDESCREEN.TOOLTIP, durabilityThreshold * 100f);
    }
}
