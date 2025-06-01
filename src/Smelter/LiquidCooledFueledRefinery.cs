using System.Collections.Generic;
using KSerialization;
using STRINGS;
using UnityEngine;
using PeterHan.PLib.Detours;

namespace Smelter
{
    // наследуемся от металлургического завода. так как хочется повторно использовать много кода.
    [SerializationConfig(MemberSerialization.OptIn)]
    public class LiquidCooledFueledRefinery : LiquidCooledRefinery, ICheckboxControl
    {
        public new class StatesInstance : GameStateMachine<States, StatesInstance, LiquidCooledFueledRefinery, object>.GameInstance
        {
            public StatesInstance(LiquidCooledFueledRefinery master) : base(master) { }

            [MyCmpAdd]
            public ManuallySetRemoteWorkTargetComponent remoteChore;

            public Chore emptyChore;

            public void CreateEmptyChore()
            {
                if (emptyChore != null)
                    emptyChore.Cancel("dupe");
                emptyChore = new WorkChore<SmelterWorkableEmpty>(
                    chore_type: Db.Get().ChoreTypes.EmptyStorage,
                    target: master.GetComponent<SmelterWorkableEmpty>(),
                    on_complete: OnEmptyComplete,
                    only_when_operational: false,
                    ignore_building_assignment: true
                    );
                emptyChore.AddPrecondition(ChorePreconditions.instance.IsNotARobot);
                remoteChore.SetChore(emptyChore);
            }

            public void CancelEmptyChore()
            {
                if (emptyChore != null)
                {
                    emptyChore.Cancel("Cancelled");
                    emptyChore = null;
                    remoteChore.SetChore(emptyChore);
                }
            }

            private void OnEmptyComplete(Chore chore)
            {
                emptyChore = null;
                remoteChore.SetChore(emptyChore);
                master.DropAllCoolant();
            }

            public void UpdateStates()
            {
                if (master.HasEnoughCoolant())
                {
                    if (master.HasEnoughFuel())
                        smi.GoTo(sm.ready);
                    else
                        smi.GoTo(sm.waiting.for_fuel);
                }
                else
                {
                    if (master.HasEnoughFuel())
                        smi.GoTo(sm.waiting.for_coolant);
                    else
                        smi.GoTo(sm.waiting.for_coolant_and_fuel);
                }
            }
        }

        public new class States : GameStateMachine<States, StatesInstance, LiquidCooledFueledRefinery>
        {
            public class Waiting : State
            {
                public State for_coolant;
                public State for_fuel;
                public State for_coolant_and_fuel;
            }

            public static StatusItem waitingForCoolantStatus;
            public static StatusItem waitingForFuelStatus;
            public static StatusItem waitingForEmptyingStatus;

            public Signal coolant_too_hot;
            public Waiting waiting;
            public State ready;
            public State needsEmptying;

            public override void InitializeStates(out BaseState default_state)
            {
                if (waitingForCoolantStatus == null)
                {
                    waitingForCoolantStatus = new StatusItem("waitingForCoolantStatus", BUILDING.STATUSITEMS.ENOUGH_COOLANT.NAME, BUILDING.STATUSITEMS.ENOUGH_COOLANT.TOOLTIP, "status_item_no_liquid_to_pump", StatusItem.IconType.Custom, NotificationType.BadMinor, false, OverlayModes.None.ID)
                    {
                        resolveStringCallback = delegate (string str, object obj)
                        {
                            var lcfr = (LiquidCooledFueledRefinery)obj;
                            return string.Format(str, lcfr.coolantTag.ProperName(), GameUtil.GetFormattedMass(lcfr.minCoolantMass, GameUtil.TimeSlice.None, GameUtil.MetricMassFormat.UseThreshold, true, "{0:0.#}"));
                        }
                    };
                }
                if (waitingForFuelStatus == null)
                {
                    waitingForFuelStatus = new StatusItem("waitingForFuelStatus", BUILDING.STATUSITEMS.ENOUGH_FUEL.NAME, BUILDING.STATUSITEMS.ENOUGH_FUEL.TOOLTIP, "status_item_resource_unavailable", StatusItem.IconType.Custom, NotificationType.BadMinor, false, OverlayModes.None.ID)
                    {
                        resolveStringCallback = delegate (string str, object obj)
                        {
                            var lcfr = (LiquidCooledFueledRefinery)obj;
                            return string.Format(str, lcfr.fuelTag.ProperName(), GameUtil.GetFormattedMass(SmelterConfig.START_FUEL_MASS, GameUtil.TimeSlice.None, GameUtil.MetricMassFormat.UseThreshold, true, "{0:0.#}"));
                        }
                    };
                }
                if (waitingForEmptyingStatus == null)
                {
                    waitingForEmptyingStatus = new StatusItem("waitingForEmptying", STRINGS.BUILDINGS.STATUSITEMS.SMELTERNEEDSEMPTYING.NAME, STRINGS.BUILDINGS.STATUSITEMS.SMELTERNEEDSEMPTYING.TOOLTIP, "status_item_empty_pipe", StatusItem.IconType.Custom, NotificationType.BadMinor, false, OverlayModes.None.ID);
                }

                default_state = waiting;
                // у нас нет выходной трубы, однако воспользуемся готовым флагом для обработки нехватки топлива
                // внимание! если событие OnStorageChange (или иное событие) 
                // прилетело из глубины недр ComplexFabricator в процессе выполнения рецепта
                // то попытка манипулировать флагами operational.SetFlag
                // приводит к хитровыебанному вылету в недрах ComplexFabricator
                // поэтому выключать флаг нужно только в проверке IsOutOfFuel (прилетает от ElementConverter)
                // или в проверках выполняемых не во время выполнения рецепта
                root
                    .OnSignal(coolant_too_hot, needsEmptying);
                waiting
                    .Enter((StatesInstance smi) => smi.UpdateStates())
                    .EventHandler(GameHashes.OnStorageChange, (StatesInstance smi) => smi.UpdateStates());
                waiting.for_coolant
                    .ToggleStatusItem(waitingForCoolantStatus, (StatesInstance smi) => smi.master);
                waiting.for_fuel
                    .ToggleStatusItem(waitingForFuelStatus, (StatesInstance smi) => smi.master);
                waiting.for_coolant_and_fuel
                    .ToggleStatusItem(waitingForCoolantStatus, (StatesInstance smi) => smi.master)
                    .ToggleStatusItem(waitingForFuelStatus, (StatesInstance smi) => smi.master);
                ready
                    .Enter((StatesInstance smi) =>
                    {
                        smi.master.SetQueueDirty();
                        smi.master.operational.SetFlag(coolantOutputPipeEmpty, true);
                    })
                    .EventTransition(GameHashes.OnStorageChange, waiting, (StatesInstance smi) => !smi.master.HasEnoughCoolant() || smi.master.IsOutOfFuel());
                needsEmptying
                    .Enter((StatesInstance smi) =>
                    {
                        smi.master.operational.SetFlag(coolantOutputPipeEmpty, false);
                        smi.master.SetQueueDirty();
                        smi.Trigger((int)GameHashes.DroppedAll); // обработчик в ComplexFabricator очищает и обновляет очередь задач. должно помочь предотвратить сбои
                        smi.CreateEmptyChore();
                    })
                    .Exit((StatesInstance smi) => smi.CancelEmptyChore())
                    .EventTransition(GameHashes.OnStorageChange, waiting, (StatesInstance smi) => smi.master.IsHotCoolantIsRemoved())
                    .ToggleStatusItem(waitingForEmptyingStatus);
            }
        }

        // доступ к приватным шнягам
        private static readonly IDetouredField<LiquidCooledRefinery, MeterController> METER_COOLANT = PDetours.DetourField<LiquidCooledRefinery, MeterController>("meter_coolant");

        [Serialize]
        private bool allowOverheating = false;

        public float maxCoolantMass;
        public Tag fuelTag;
        private StatesInstance smi;

#pragma warning disable CS0649
        [MyCmpReq]
        private ElementConverter elementConverter;
#pragma warning restore CS0649

        public bool AllowOverheating { get => allowOverheating; }
        string ICheckboxControl.CheckboxTitleKey => STRINGS.BUILDINGS.PREFABS.SMELTER.NAME.key.String;
        string ICheckboxControl.CheckboxLabel => STRINGS.BUILDINGS.PREFABS.SMELTER.SIDE_SCREEN_CHECKBOX;
        string ICheckboxControl.CheckboxTooltip => STRINGS.BUILDINGS.PREFABS.SMELTER.SIDE_SCREEN_CHECKBOX_TOOLTIP;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // настраиваем метер воды
            METER_COOLANT.Set(this, new MeterController(GetComponent<KBatchedAnimController>(), "meter_target", "meter", Meter.Offset.Behind, Grid.SceneLayer.NoLayer, Vector3.zero, null));
            // у нас нет выходной трубы
            operational.SetFlag(coolantOutputPipeEmpty, false);
            // брать топливо из выходного хранилища. так как входное для рецептов. чтобы не было путаницы углерода для топлива и стали.
            elementConverter.SetStorage(outStorage);
            fabricatorSM.idleAnimationName = "on";
            workable.OnWorkableEventCB += OnWorkableEvent;
            smi = new StatesInstance(this);
            smi.StartSM();
        }

        protected override void OnCleanUp()
        {
            smi.StopSM("cleanup");
            workable.OnWorkableEventCB -= OnWorkableEvent;
            base.OnCleanUp();
        }

        // предотвращаем двойное проигрывание анимации вызванное из недр ComplexFabricatorSM
        private void OnWorkableEvent(Workable workable, Workable.WorkableEvent workable_event)
        {
            if (workable_event == global::Workable.WorkableEvent.WorkStopped)
            {
                var smi = fabricatorSM.smi;
                if (!smi.IsNullOrStopped())
                    smi.GoTo(smi.sm.operating.working_pst_complete);
                operational.SetActive(false, false);
            }
        }

        public bool HasEnoughFuel()
        {
            return outStorage.GetAmountAvailable(fuelTag) >= SmelterConfig.START_FUEL_MASS;
        }

        internal bool IsOutOfFuel()
        {
            bool outoffuel = outStorage.GetAmountAvailable(fuelTag) <= 0f;
            operational.SetFlag(coolantOutputPipeEmpty, !outoffuel);
            return outoffuel;
        }

        internal bool IsHotCoolantIsRemoved()
        {
            return !outStorage.Has(coolantTag);
        }

        internal void CheckCoolantIsTooHot()
        {
            // если общая масса на выходе слишком горячая и больше лимита - нужно заказать опустошение
            // если разрешен перегрев, или отсутствует следующий в очереди заказ - проверка не нужна
            if (allowOverheating || !operational.IsOperational)
                return;

            var nextorder = CurrentWorkingOrder ?? NextOrder;
            if (nextorder == null)
                return;

            float total_mass = outStorage.GetAmountAvailable(coolantTag);
            if (total_mass < maxCoolantMass)
                return;

            // если масса отработанного хладагента превышает лимит, 
            // проверяем сколько его можно бесопасно использовать для следующего в очереди рецепта
            float energyDelta = this.CalculateEnergyDelta(nextorder);

            var pooledList = ListPool<GameObject, LiquidCooledFueledRefinery>.Allocate();
            outStorage.Find(coolantTag, pooledList);
            foreach (GameObject gameObject in pooledList)
            {
                gameObject.TryGetComponent<PrimaryElement>(out var primaryElement);
                float mass = primaryElement.Mass;
                float temperatureDelta = this.CalculateTemperatureDelta(primaryElement, energyDelta);
                if (mass > 0 && (primaryElement.Temperature + temperatureDelta < primaryElement.Element.highTemp))
                {
                    total_mass -= mass;
                }
            }
            pooledList.Recycle();

            if (total_mass < maxCoolantMass)
                return;
            smi.sm.coolant_too_hot.Trigger(smi);
        }

        internal void DropAllCoolant()
        {
            var position = Grid.CellToPosCCC(Grid.PosToCell(this), Grid.SceneLayer.Ore) + outputOffset;
            var pooledList = ListPool<GameObject, LiquidCooledFueledRefinery>.Allocate();
            outStorage.Find(coolantTag, pooledList);
            foreach (GameObject gameObject in pooledList)
            {
                outStorage.Drop(gameObject)?.transform.SetPosition(position);
            }
            pooledList.Recycle();
        }

        // некоторые продукты имеют более низкую т плавления чем выходная т
        // сделаем их холоднее
        protected override List<GameObject> SpawnOrderProduct(ComplexRecipe recipe)
        {
            var results = base.SpawnOrderProduct(recipe);
            foreach (var result in results)
            {
                if (result.TryGetComponent<PrimaryElement>(out var primaryElement)
                    && primaryElement.Temperature >= primaryElement.Element.highTemp)
                    primaryElement.Temperature = primaryElement.Element.highTemp - SimMessages.STATE_TRANSITION_TEMPERATURE_BUFER;
            }
            return results;
        }

        bool ICheckboxControl.GetCheckboxValue()
        {
            return allowOverheating;
        }

        void ICheckboxControl.SetCheckboxValue(bool value)
        {
            allowOverheating = value;
        }
    }
}
