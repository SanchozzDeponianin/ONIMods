using UnityEngine;
using STRINGS;

namespace SuitRecharger
{
    public class SuitRecharger : Workable, ISecondaryInput, ISecondaryOutput
    {
        private static readonly EventSystem.IntraObjectHandler<SuitRecharger> OnOperationalChangedDelegate =
            new EventSystem.IntraObjectHandler<SuitRecharger>(
                (SuitRecharger component, object data) => component.UpdateChore());

        private static readonly EventSystem.IntraObjectHandler<SuitRecharger> OnStorageChangeDelegate =
            new EventSystem.IntraObjectHandler<SuitRecharger>(
                (SuitRecharger component, object data) => component.OnStorageChange(data));

        // проверка что костюм действительно надет
        private static readonly Chore.Precondition IsSuitEquipped = new Chore.Precondition
        {
            id = nameof(IsSuitEquipped),
            // todo: уточнить текстовку
            description = DUPLICANTS.CHORES.PRECONDITIONS.CAN_DO_RECREATION,
            sortOrder = -1,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                return context.consumerState.prefabid.HasTag(GameTags.HasSuitTank);
            }
        };

        // todo: возможно стоит разделить на несколько прекондиций для лучшего текста.
        // todo: возможно стоит проверять остаток массы для конкретного костюма, а не требовать возможность полной заправки
        // todo: возможно эти прекондиции неоптимальны по быстродействию. нужно обдумать

        // проверка что костюму требуется заправка.
        // скопировано из SuitLocker.ReturnSuitWorkable
        // отключена проверка уровня заряда свинцовых костюмов
        private static readonly Chore.Precondition DoesSuitNeedRecharging = new Chore.Precondition
        {
            id = nameof(DoesSuitNeedRecharging),
            description = DUPLICANTS.CHORES.PRECONDITIONS.DOES_SUIT_NEED_RECHARGING_URGENT,
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
            // todo: уточнить текстовку
            description = "Not Enough Oxygen",
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
            // todo: уточнить текстовку
            description = "Not Enough Fuel",
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

        public static float сhargeTime = 1f;
        private static float batteryChargeTime = 60f;
        public static float warmupTime;
        private float elapsedTime;

#pragma warning disable CS0649
        // todo: поправить после решения косяка с вращением
        /*[MyCmpReq]
        private Building building;*/

        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private Storage storage;
#pragma warning restore CS0649

        // керосин
        [SerializeField]
        public ConduitPortInfo fuelPortInfo;
        private int fuelInputCell = Grid.InvalidCell;
        private FlowUtilityNetwork.NetworkItem fuelNetworkItem;
        private ConduitConsumer fuelConsumer;
        private Tag fuelTag;

        // жидкие отходы
        [SerializeField]
        public ConduitPortInfo liquidWastePortInfo;
        private int liquidWasteOutputCell = Grid.InvalidCell;
        private FlowUtilityNetwork.NetworkItem liquidWasteNetworkItem;
        private ConduitDispenser liquidWasteDispenser;

        // газообразные отходы
        [SerializeField]
        public ConduitPortInfo gasWastePortInfo;
        private int gasWasteOutputCell = Grid.InvalidCell;
        private FlowUtilityNetwork.NetworkItem gasWasteNetworkItem;
        private ConduitDispenser gasWasteDispenser;

        private MeterController oxygenMeter;
        private MeterController fuelMeter;
        private WorkChore<SuitRecharger> chore;

        public float OxygenAvailable { get; private set; }
        public float FuelAvailable { get; private set; }

        private SuitTank suitTank;
        private JetSuitTank jetSuitTank;
        private LeadSuitTank leadSuitTank;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            resetProgressOnStop = true;
            showProgressBar = false;
            SetWorkTime(float.PositiveInfinity);
            workerStatusItem = null;
            synchronizeAnims = true;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // вторичные входы и выходы для керосина и отходов
            // todo: поправить после решения косяка с вращением
            fuelTag = SimHashes.Petroleum.CreateTag();
            fuelInputCell = Grid.OffsetCell(Grid.PosToCell(this), /*building.GetRotatedOffset*/(fuelPortInfo.offset));
            fuelConsumer = CreateConduitConsumer(ConduitType.Liquid, fuelInputCell, out fuelNetworkItem);
            fuelConsumer.capacityTag = fuelTag;
            fuelConsumer.capacityKG = SuitRechargerConfig.FUEL_CAPACITY;

            liquidWasteOutputCell = Grid.OffsetCell(Grid.PosToCell(this), /*building.GetRotatedOffset*/(liquidWastePortInfo.offset));
            liquidWasteDispenser = CreateConduitDispenser(ConduitType.Liquid, liquidWasteOutputCell, out liquidWasteNetworkItem);
            liquidWasteDispenser.elementFilter = new SimHashes[] { SimHashes.Petroleum };
            liquidWasteDispenser.invertElementFilter = true;

            gasWasteOutputCell = Grid.OffsetCell(Grid.PosToCell(this), /*building.GetRotatedOffset*/(gasWastePortInfo.offset));
            gasWasteDispenser = CreateConduitDispenser(ConduitType.Gas, gasWasteOutputCell, out gasWasteNetworkItem);
            gasWasteDispenser.elementFilter = new SimHashes[] { SimHashes.Oxygen };
            gasWasteDispenser.invertElementFilter = true;
            /*
            var requires_inputs = gameObject.AddComponent<RequireInputs>();
            requires_inputs.conduitConsumer = fuel_consumer;
            requires_inputs.SetRequirements(false, true);
            */
            // создаём метеры
            oxygenMeter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_target", "meter", Meter.Offset.Infront, Grid.SceneLayer.NoLayer, new string[] { "meter_target" });
            fuelMeter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_target_fuel", "meter_fuel", Meter.Offset.Infront, Grid.SceneLayer.NoLayer, new string[] { "meter_target_fuel" });

            Subscribe((int)GameHashes.OperationalChanged, OnOperationalChangedDelegate);
            Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            OnStorageChange();
            UpdateChore();
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.OperationalChanged, OnOperationalChangedDelegate);
            Unsubscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            CancelChore();
            Conduit.GetNetworkManager(fuelPortInfo.conduitType).RemoveFromNetworks(fuelInputCell, fuelNetworkItem, true);
            Conduit.GetNetworkManager(liquidWastePortInfo.conduitType).RemoveFromNetworks(liquidWasteOutputCell, liquidWasteNetworkItem, true);
            Conduit.GetNetworkManager(gasWastePortInfo.conduitType).RemoveFromNetworks(gasWasteOutputCell, gasWasteNetworkItem, true);
            base.OnCleanUp();
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
            var dispenser = gameObject.AddComponent<ConduitDispenser>();
            dispenser.conduitType = outputType;
            dispenser.useSecondaryOutput = true;
            dispenser.alwaysDispense = true;
            dispenser.storage = storage;
            var networkManager = Conduit.GetNetworkManager(outputType);
            flowNetworkItem = new FlowUtilityNetwork.NetworkItem(outputType, Endpoint.Source, outputCell, gameObject);
            networkManager.AddToNetworks(outputCell, flowNetworkItem, true);
            return dispenser;
        }

        private void OnStorageChange(object data = null)
        {
            var primaryElement = ((GameObject)data)?.GetComponent<PrimaryElement>();
            if (primaryElement != null)
            {
                if (primaryElement.ElementID == SimHashes.Oxygen)
                    OxygenAvailable = primaryElement.Mass;
                if (primaryElement.ElementID == SimHashes.Petroleum)
                    FuelAvailable = primaryElement.Mass;
            }
            else
            {
                OxygenAvailable = GetOxygen()?.GetComponent<PrimaryElement>()?.Mass ?? 0;
                FuelAvailable = GetFuel()?.GetComponent<PrimaryElement>()?.Mass ?? 0;
            }
            RefreshMeter();
        }

        private void RefreshMeter()
        {
            oxygenMeter.SetPositionPercent(Mathf.Clamp01(OxygenAvailable / SuitRechargerConfig.O2_CAPACITY));
            fuelMeter.SetPositionPercent(Mathf.Clamp01(FuelAvailable / SuitRechargerConfig.FUEL_CAPACITY));
        }

        private GameObject GetOxygen()
        {
            return storage.FindFirst(GameTags.Oxygen);
        }

        private GameObject GetFuel()
        {
            return storage.FindFirst(fuelTag);
        }

        private void CreateChore()
        {
            if (chore == null)
            {
                chore = new WorkChore<SuitRecharger>(
                    chore_type: Db.Get().ChoreTypes.ReturnSuitUrgent,
                    target: this,
                    only_when_operational: false,
                    priority_class: PriorityScreen.PriorityClass.personalNeeds,
                    priority_class_value: 5,
                    add_to_daily_report: false);
                chore.AddPrecondition(IsSuitEquipped, null);
                chore.AddPrecondition(DoesSuitNeedRecharging, null);
                chore.AddPrecondition(IsEnoughOxygen, this);
                chore.AddPrecondition(IsEnoughFuel, this);
            }
        }

        private void CancelChore()
        {
            if (chore != null)
            {
                chore.Cancel("RechargeWorkable.CancelChore");
                chore = null;
            }
        }

        private void UpdateChore()
        {
            if (operational.IsOperational) CreateChore();
            else CancelChore();
        }

        protected override void OnStartWork(Worker worker)
        {
            var suit = worker.GetComponent<MinionIdentity>().GetEquipment().GetAssignable(Db.Get().AssignableSlots.Suit);
            if (suit != null)
            {
                suitTank = suit.GetComponent<SuitTank>();
                jetSuitTank = suit.GetComponent<JetSuitTank>();
                leadSuitTank = suit.GetComponent<LeadSuitTank>();
            }
            operational.SetActive(true, false);
            elapsedTime = 0;
        }

        protected override void OnStopWork(Worker worker)
        {
            operational.SetActive(false, false);
            if (worker != null)
            {
                if (jetSuitTank != null && !jetSuitTank.IsEmpty())
                {
                    worker.RemoveTag(GameTags.JetSuitOutOfFuel);
                }
                if (leadSuitTank != null)
                {
                    if (!leadSuitTank.IsEmpty())
                        worker.RemoveTag(GameTags.SuitBatteryOut);
                    if (!leadSuitTank.NeedsRecharging())
                        worker.RemoveTag(GameTags.SuitBatteryLow);
                }
            }
            suitTank = null;
            jetSuitTank = null;
            leadSuitTank = null;
        }

        protected override void OnCompleteWork(Worker worker)
        {
            chore = null;
            UpdateChore();
        }

        protected override bool OnWorkTick(Worker worker, float dt)
        {
            elapsedTime += dt;
            if (elapsedTime <= warmupTime)
                return false;
            bool oxygen_charged = ChargeSuit(dt);
            bool fuel_charged = FuelSuit(dt);
            FillBattery(dt);
            return oxygen_charged && fuel_charged;
        }

        public override bool InstantlyFinish(Worker worker)
        {
            return false;
        }

        private bool ChargeSuit(float dt)
        {
            if (suitTank != null && !suitTank.IsFull())
            {
                var oxygen = GetOxygen();
                if (oxygen != null)
                {
                    float amount_to_refill = suitTank.capacity * dt / сhargeTime;
                    amount_to_refill = Mathf.Min(amount_to_refill, suitTank.capacity - suitTank.GetTankAmount());
                    amount_to_refill = Mathf.Min(amount_to_refill, oxygen.GetComponent<PrimaryElement>().Mass);
                    if (amount_to_refill > 0f)
                    {
                        storage.Transfer(suitTank.storage, suitTank.elementTag, amount_to_refill, false, true);
                        return false;
                    }
                }
            }
            return true;
        }

        private bool FuelSuit(float dt)
        {
            if (jetSuitTank != null && !jetSuitTank.IsFull())
            {
                var fuel = GetFuel();
                if (fuel != null)
                {
                    var fuel_pe = fuel.GetComponent<PrimaryElement>();
                    float amount_to_refill = JetSuitTank.FUEL_CAPACITY * dt / сhargeTime;
                    amount_to_refill = Mathf.Min(amount_to_refill, JetSuitTank.FUEL_CAPACITY - jetSuitTank.amount);
                    amount_to_refill = Mathf.Min(amount_to_refill, fuel_pe.Mass);
                    if (amount_to_refill > 0f)
                    {
                        fuel_pe.Mass -= amount_to_refill;
                        jetSuitTank.amount += amount_to_refill;
                        return false;
                    }
                }
            }
            return true;
        }

        // свинцовые костюмы заряжаем не в полную силу, а так чучуть.
        // todo: обдумать вариант более быстрой зарядки с повышеным расходом искричества
        private bool FillBattery(float dt)
        {
            if (leadSuitTank != null && !leadSuitTank.IsFull())
            {
                leadSuitTank.batteryCharge += dt / batteryChargeTime;
                return false;
            }
            return true;
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
