using System;
using UnityEngine;
using STRINGS;

namespace SuitRecharger
{
    public class SuitRecharger : Workable, ISecondaryInput
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

        private static float batteryChargeTime = 60f;

#pragma warning disable CS0649
        [MyCmpReq]
        private Building building;

        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private Storage storage;
#pragma warning restore CS0649

        // для керосина
        [SerializeField]
        public ConduitPortInfo portInfo;
        private int secondaryInputCell = -1;
        private FlowUtilityNetwork.NetworkItem flowNetworkItem;
        private ConduitConsumer fuel_consumer;
        private Tag fuel_tag;

        private MeterController oxygenMeter;
        private MeterController fuelMeter;
        private WorkChore<SuitRecharger> chore;

        public float OxygenAvailable { get; private set; }
        public float FuelAvailable { get; private set; }

        private SuitTank suitTank;
        private JetSuitTank jetSuitTank;
        private LeadSuitTank leadSuitTank;

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

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            resetProgressOnStop = true;
            showProgressBar = false;
            workerStatusItem = null;
            overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_suitrecharger_kanim") };
            synchronizeAnims = true;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // вторичный вход для керосина
            fuel_tag = SimHashes.Petroleum.CreateTag();
            fuel_consumer = gameObject.AddComponent<ConduitConsumer>();
            fuel_consumer.conduitType = portInfo.conduitType;
            fuel_consumer.consumptionRate = ConduitFlow.MAX_LIQUID_MASS;
            fuel_consumer.capacityTag = fuel_tag;
            fuel_consumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            fuel_consumer.forceAlwaysSatisfied = true;
            fuel_consumer.capacityKG = SuitRechargerConfig.FUEL_CAPACITY;
            fuel_consumer.useSecondaryInput = true;
            /*
            var requires_inputs = gameObject.AddComponent<RequireInputs>();
            requires_inputs.conduitConsumer = fuel_consumer;
            requires_inputs.SetRequirements(false, true);
            */
            int cell = Grid.PosToCell(transform.GetPosition());
            var rotated_offset = building.GetRotatedOffset(portInfo.offset);
            secondaryInputCell = Grid.OffsetCell(cell, rotated_offset);
            flowNetworkItem = new FlowUtilityNetwork.NetworkItem(portInfo.conduitType, Endpoint.Sink, secondaryInputCell, gameObject);
            Conduit.GetNetworkManager(portInfo.conduitType).AddToNetworks(secondaryInputCell, flowNetworkItem, true);
            // создаём метеры
            oxygenMeter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_oxygen_target", "meter_oxygen", Meter.Offset.Infront, Grid.SceneLayer.BuildingFront, new string[] { "meter_oxygen_target" });
            fuelMeter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_resources_target", "meter_resources", Meter.Offset.Behind, Grid.SceneLayer.BuildingBack, new string[] { "meter_resources_target" });
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
            Conduit.GetNetworkManager(portInfo.conduitType).RemoveFromNetworks(secondaryInputCell, flowNetworkItem, true);
            base.OnCleanUp();
        }

        private void OnStorageChange(object data = null)
        {
            var oxygen = GetOxygen();
            if (oxygen != null)
                OxygenAvailable = oxygen.GetComponent<PrimaryElement>().Mass;
            else
                OxygenAvailable = 0;
            var fuel = GetFuel();
            if (fuel != null)
                FuelAvailable = fuel.GetComponent<PrimaryElement>().Mass;
            else
                FuelAvailable = 0;
            RefreshMeter();
        }

        private void RefreshMeter()
        {
            oxygenMeter.SetPositionPercent(Math.Min(OxygenAvailable / SuitRechargerConfig.O2_CAPACITY, 1f));
            fuelMeter.SetPositionPercent(Math.Min(FuelAvailable / SuitRechargerConfig.FUEL_CAPACITY, 1f));
        }

        private GameObject GetOxygen()
        {
            return storage.FindFirst(GameTags.Oxygen);
        }

        private GameObject GetFuel()
        {
            return storage.FindFirst(fuel_tag);
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
                    float amount_to_refill = suitTank.capacity * dt / workTime;
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
                    float amount_to_refill = JetSuitTank.FUEL_CAPACITY * dt / workTime;
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
            return portInfo.conduitType == type;
        }

        CellOffset ISecondaryInput.GetSecondaryConduitOffset(ConduitType type)
        {
            if (portInfo.conduitType == type)
                return portInfo.offset;
            else
                return CellOffset.none;
        }
    }
}
