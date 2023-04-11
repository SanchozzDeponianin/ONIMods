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
    public class SuitRecharger : StateMachineComponent<SuitRecharger.StatesInstance>, ISaveLoadable, ISecondaryInput, ISecondaryOutput
    {
        private static readonly EventSystem.IntraObjectHandler<SuitRecharger> CheckPipesDelegate =
            new EventSystem.IntraObjectHandler<SuitRecharger>((SuitRecharger component, object data) => component.CheckPipes(data));

        private static readonly EventSystem.IntraObjectHandler<SuitRecharger> OnStorageChangeDelegate =
            new EventSystem.IntraObjectHandler<SuitRecharger>(
                (SuitRecharger component, object data) => component.OnStorageChange(data));

        private static readonly EventSystem.IntraObjectHandler<SuitRecharger> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<SuitRecharger>(
                (SuitRecharger component, object data) => component.OnCopySettings(data));

        // цена полной починки
        public struct RepairSuitCost
        {
            public Tag material;
            public float amount;
            public float energy;
        }
        internal static Dictionary<Tag, RepairSuitCost[]> repairSuitCost = new Dictionary<Tag, RepairSuitCost[]>();
        internal static List<Tag> repairMaterials = new List<Tag>();

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

        private static bool TryGetAssignableSuit(Equipment equipment, out Assignable assignable)
        {
            assignable = null;
            if (equipment != null)
                assignable = equipment.GetSlot(Db.Get().AssignableSlots.Suit)?.assignable;
            return assignable != null;
        }

        // проверка возможности ремонта и что костюм имеет достаточную прочность чтобы не сломаться в процессе зарядки
        private static readonly Chore.Precondition IsSuitHasEnoughDurability = new Chore.Precondition
        {
            id = nameof(IsSuitHasEnoughDurability),
            description = IS_SUIT_HAS_ENOUGH_DURABILITY,
            sortOrder = 5,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                if (data is SuitRecharger recharger && recharger != null
                    && TryGetAssignableSuit(context.consumerState.equipment, out var assignable)
                    && assignable is Equippable equippable
                    && assignable.TryGetComponent<Durability>(out var durability))
                {
                    float d = durability.GetTrueDurability(context.consumerState.resume);
                    if (recharger.enableRepair && repairSuitCost.TryGetValue(equippable.def.Id.ToTag(), out var costs))
                    {
                        foreach (var cost in costs)
                        {
                            float need = cost.amount * (1f - d);
                            recharger.repairMaterialsAvailable.TryGetValue(cost.material, out float available);
                            if (need <= available)
                                return true;
                        }
                    }
                    return d >= recharger.durabilityThreshold;
                }
                return true;
            }
        };

        // проверка что костюму требуется заправка.
        private static readonly Chore.Precondition DoesSuitNeedRecharging = new Chore.Precondition
        {
            id = nameof(DoesSuitNeedRecharging),
            description = PRECONDITIONS.DOES_SUIT_NEED_RECHARGING_URGENT,
            sortOrder = 1,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                bool result = false;
                if (TryGetAssignableSuit(context.consumerState.equipment, out var assignable)
                    && assignable.TryGetComponent<SuitTank>(out var suit_tank))
                {
                    if (suit_tank.NeedsRecharging())
                        result = true;
                    else
                    {
                        if (assignable.TryGetComponent<JetSuitTank>(out var jet_suit_tank) && jet_suit_tank.NeedsRecharging())
                            result = true;
                    }
                }
                return result;
            }
        };

        // проверка что костюму требуется срочная заправка 
        private static readonly Chore.Precondition DoesSuitNeedRechargingUrgent = new Chore.Precondition
        {
            id = nameof(DoesSuitNeedRechargingUrgent),
            description = PRECONDITIONS.HAS_URGE,
            sortOrder = 1,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                bool result = false;
                if (TryGetAssignableSuit(context.consumerState.equipment, out var assignable)
                    && assignable.TryGetComponent<SuitTank>(out var suit_tank)
                    && suit_tank.IsEmpty())
                {
                    result = true;
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
                if (data is SuitRecharger recharger && recharger != null
                    && TryGetAssignableSuit(context.consumerState.equipment, out var assignable)
                    && assignable.TryGetComponent<SuitTank>(out var suit_tank)
                    && recharger.OxygenAvailable < (suit_tank.capacity - suit_tank.GetTankAmount()))
                {
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
                if (data is SuitRecharger recharger && recharger != null
                    && TryGetAssignableSuit(context.consumerState.equipment, out var assignable)
                    && assignable.TryGetComponent<JetSuitTank>(out var jet_suit_tank)
                    && recharger.FuelAvailable < (JetSuitTank.FUEL_CAPACITY - jet_suit_tank.amount)
                    && assignable.TryGetComponent<SuitTank>(out var suit_tank)
                    && !suit_tank.NeedsRecharging())
                {
                    result = false;
                }
                return result;
            }
        };

        // проверка что заправка не "уже выполняется"
        private static readonly Chore.Precondition NotCurrentlyRecharging = new Chore.Precondition
        {
            id = nameof(NotCurrentlyRecharging),
            description = CURRENTLY_RECHARGING,
            sortOrder = 0,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                bool result = true;
                var currentChore = context.consumerState.choreDriver.GetCurrentChore();
                if (currentChore != null)
                {
                    string id = currentChore.choreType.Id;
                    result = (id != RecoverBreathRecharge.Id && id != Db.Get().ChoreTypes.Recharge.Id);
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
                urge: null, //RecoverBreath.urge.Id,
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
                var chore = CreateUseChore(smi, Db.Get().ChoreTypes.Recharge, PriorityScreen.PriorityClass.personalNeeds);
                chore.AddPrecondition(DoesSuitNeedRecharging, null);
                return chore;
            }

            public Chore CreateRecoverBreathChore(StatesInstance smi)
            {
                var chore = CreateUseChore(smi, RecoverBreathRecharge, PriorityScreen.PriorityClass.compulsory);
                chore.AddPrecondition(DoesSuitNeedRechargingUrgent, null);
                return chore;
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
                chore.AddPrecondition(NotCurrentlyRecharging);
                if (durabilityEnabled)  // не проверять если износ отключен в настройках сложности
                    chore.AddPrecondition(IsSuitHasEnoughDurability, smi.master);
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

        // порог изношенности костюма, при превышении не заряжать, если ремонт не разрешен или невозможен
        [Serialize]
        private float durabilityThreshold;
        public float DurabilityThreshold { get => durabilityThreshold; set => durabilityThreshold = Mathf.Clamp01(value); }

        static private float defaultDurabilityThreshold;
        static private float durabilityPerCycleGap = 0.2f;
        static internal bool durabilityEnabled => defaultDurabilityThreshold > 0f;

        [Serialize]
        private bool enableRepair;
        public bool EnableRepair { get => enableRepair; set { enableRepair = value; UpdateDeliveryComponents(); } }

        private MeterController oxygenMeter;
        private MeterController fuelMeter;

        public float OxygenAvailable { get; private set; }
        public float FuelAvailable { get; private set; }
        public Dictionary<Tag, float> repairMaterialsAvailable { get; private set; } = new Dictionary<Tag, float>();

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
            DurabilityThreshold = defaultDurabilityThreshold;

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
            if (TryGetComponent<KBatchedAnimController>(out var kbac))
            {
                oxygenMeter = new MeterController(kbac, "meter_target", "meter", Meter.Offset.Infront, Grid.SceneLayer.NoLayer, new string[] { "meter_target" });
                fuelMeter = new MeterController(kbac, "meter_target_fuel", "meter_fuel", Meter.Offset.Infront, Grid.SceneLayer.NoLayer, new string[] { "meter_target_fuel" });
            }

            Subscribe((int)GameHashes.ConduitConnectionChanged, CheckPipesDelegate);
            Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            Game.Instance.liquidConduitFlow.AddConduitUpdater(OnLiquidConduitUpdate, ConduitFlowPriority.Default);
            Game.Instance.gasConduitFlow.AddConduitUpdater(OnGasConduitUpdate, ConduitFlowPriority.Default);

            repairMaterials.Do(tag => { repairMaterialsAvailable[tag] = 0f; });
            OnStorageChange();
            UpdateDeliveryComponents();
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
            consumer.OperatingRequirement = Operational.State.Functional;
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
            if (data is GameObject go && go != null && go.TryGetComponent<SuitRecharger>(out var recharger))
            {
                DurabilityThreshold = recharger.durabilityThreshold;
                EnableRepair = recharger.enableRepair;
            }
        }

        private void OnStorageChange(object data = null)
        {
            OxygenAvailable = storage.GetMassAvailable(GameTags.Oxygen);
            FuelAvailable = storage.GetMassAvailable(fuelTag);
            foreach (var tag in repairMaterials)
                repairMaterialsAvailable[tag] = storage.GetMassAvailable(tag);
            RefreshMeter();
        }

        private void RefreshMeter()
        {
            oxygenMeter.SetPositionPercent(Mathf.Clamp01(OxygenAvailable / SuitRechargerConfig.O2_CAPACITY));
            fuelMeter.SetPositionPercent(Mathf.Clamp01(FuelAvailable / SuitRechargerConfig.FUEL_CAPACITY));
        }

        private void UpdateDeliveryComponents()
        {
            var mdkgs = GetComponents<ManualDeliveryKG>();
            foreach (var mg in mdkgs)
            {
                if (!mg.allowPause)
                    mg.Pause(!enableRepair, "repair disable");
                if (!enableRepair)
                {
                    foreach (var tag in repairMaterials)
                        storage.Drop(tag);
                }
            }
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
    }
}
