using System;
using System.Collections.Generic;
using KSerialization;
using STRINGS;
using UnityEngine;

using PeterHan.PLib;
using PeterHan.PLib.Detours;

namespace Smelter
{
    // наследуемся от металлургического завода. так как хочется повторно использовать много кода.
    [SerializationConfig(MemberSerialization.OptIn)]
    public class LiquidCooledFueledRefinery : LiquidCooledRefinery, ICheckboxControl
    {
        public new class StatesInstance : GameStateMachine<States, StatesInstance, LiquidCooledFueledRefinery, object>.GameInstance
        {
            public StatesInstance(LiquidCooledFueledRefinery master) : base(master)
            {
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
            public Waiting waiting;
            public State ready;
            //public State output_blocked; // todo: подумоть. сливать автоматом или дупликами

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
                    waitingForFuelStatus = new StatusItem("waitingForFuelStatus", BUILDING.STATUSITEMS.ENOUGH_FUEL.NAME, BUILDING.STATUSITEMS.ENOUGH_FUEL.TOOLTIP, "status_item_fabricator_empty", StatusItem.IconType.Custom, NotificationType.BadMinor, false, OverlayModes.None.ID)
                    {
                        resolveStringCallback = delegate (string str, object obj)
                        {
                            var lcfr = (LiquidCooledFueledRefinery)obj;
                            return string.Format(str, lcfr.fuelTag.ProperName(), GameUtil.GetFormattedMass(SmelterConfig.START_FUEL_MASS, GameUtil.TimeSlice.None, GameUtil.MetricMassFormat.UseThreshold, true, "{0:0.#}"));
                        }
                    };
                }

                default_state = waiting;
                // у нас нет выходной трубы, однако воспользуемся готовым флагом для обработки нехватки топлива
                // внимание! если событие OnStorageChange прилетело из глубины недр ComplexFabricator 
                // попытка манипулировать флагами operational.SetFlag
                // приводит к хитровыебанному вылету в недрах ComplexFabricator
                // поэтому выключать флаг нужно только в проверке IsOutOfFuel
                waiting
                    .Enter((StatesInstance smi) => smi.master.Test("Enter waiting"))
                    .Enter((StatesInstance smi) => smi.UpdateStates())
                    .EventHandler(GameHashes.OnStorageChange, (StatesInstance smi) => smi.UpdateStates());
                waiting.for_coolant
                    .Enter((StatesInstance smi) => smi.master.Test("Enter for_coolant"))
                    .ToggleStatusItem(waitingForCoolantStatus, (StatesInstance smi) => smi.master);
                waiting.for_fuel
                    .Enter((StatesInstance smi) => smi.master.Test("Enter for_fuel"))
                    .ToggleStatusItem(waitingForFuelStatus, (StatesInstance smi) => smi.master);
                waiting.for_coolant_and_fuel
                    .Enter((StatesInstance smi) => smi.master.Test("Enter for_coolant_and_fuel"))
                    .ToggleStatusItem(waitingForCoolantStatus, (StatesInstance smi) => smi.master)
                    .ToggleStatusItem(waitingForFuelStatus, (StatesInstance smi) => smi.master);
                ready
                    .Enter((StatesInstance smi) => smi.master.Test("Enter ready"))
                    .Enter((StatesInstance smi) =>
                    {
                        smi.master.SetQueueDirty();
                        smi.master.operational.SetFlag(coolantOutputPipeEmpty, true);
                    })
                    .EventTransition(GameHashes.OnStorageChange, waiting, (StatesInstance smi) => !smi.master.HasEnoughCoolant() || smi.master.IsOutOfFuel());
            }
        }

        // доступ к приватным шнягам
        private static readonly IDetouredField<LiquidCooledRefinery, MeterController> METER_COOLANT = PDetours.DetourField<LiquidCooledRefinery, MeterController>("meter_coolant");
        private static readonly IDetouredField<LiquidCooledRefinery, StatesInstance> SMI = PDetours.DetourField<LiquidCooledRefinery, StatesInstance>("smi");

        // доступ к ComplexFabricator (base.base.)
        private static readonly IntPtr ComplexFabricator_OnSpawn_Ptr;
        private readonly System.Action ComplexFabricator_OnSpawn;

        [Serialize]
        private bool allowOverheating = false;

        public Tag fuelTag;

#pragma warning disable CS0649
        [MyCmpReq]
        private ElementConverter elementConverter;
#pragma warning restore CS0649

        public string CheckboxTitleKey => "lalala";
        public string CheckboxLabel => "allowOverheating";
        public string CheckboxTooltip => "allowOverheating";

        static LiquidCooledFueledRefinery()
        {
            // todo: сделать обработку внезапной ситуации
            ComplexFabricator_OnSpawn_Ptr = PPatchTools.GetMethodSafe(typeof(ComplexFabricator), nameof(OnSpawn), false, PPatchTools.AnyArguments).MethodHandle.GetFunctionPointer();
        }

        LiquidCooledFueledRefinery()
        {
            ComplexFabricator_OnSpawn = (System.Action)Activator.CreateInstance(typeof(System.Action), this, ComplexFabricator_OnSpawn_Ptr);
        }

        protected override void OnSpawn()
        {
            ComplexFabricator_OnSpawn();

            // настраиваем метер воды
            METER_COOLANT.Set(this, new MeterController(GetComponent<KBatchedAnimController>(), "meter_target", "meter", Meter.Offset.Behind, Grid.SceneLayer.NoLayer, Vector3.zero, null));

            // у нас нет выходной трубы
            operational.SetFlag(coolantOutputPipeEmpty, false);

            // брать топливо из выходного хранилища. так как входное для рецептов. чтобы не было путаницы углерода для топлива и стали.
            elementConverter.SetStorage(outStorage);

            // переопределяем сми
            //SMI.Get(this).StopSM("override");
            var smi = new StatesInstance(this);
            SMI.Set(this, smi);
            smi.StartSM();
        }

        protected override void OnCleanUp()
        {
            SMI.Get(this).StopSM("cleanup");
            base.OnCleanUp();
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
        /*
        protected override bool HasIngredients(ComplexRecipe recipe, Storage storage)
        {
            return base.HasIngredients(recipe, storage) && HasEnoughFuel();
        }*/

        protected override List<GameObject> SpawnOrderProduct(ComplexRecipe recipe)
        {
            // todo: сделать обработку выходной воды
            // если температура выше точки кипения - вылить на пол
            // если общая масса на выходе больше лимита - нужно заказать опустошение
            var result = base.SpawnOrderProduct(recipe);
            operational.SetActive(false, false);

            var pooledList = ListPool<GameObject, LiquidCooledFueledRefinery>.Allocate();
            outStorage.Find(coolantTag, pooledList);

            foreach (GameObject gameObject in pooledList)
            {
                var primaryElement = gameObject.GetComponent<PrimaryElement>();
                if (primaryElement.Temperature > primaryElement.Element.highTemp)
                {
                    outStorage.Drop(gameObject);
                    gameObject.GetComponent<Dumpable>()?.Dump(transform.GetPosition() + Vector3.right);
                }
            }
            pooledList.Recycle();
            return result;
        }

        protected override void TransferCurrentRecipeIngredientsForBuild()
        {
            // высчитать бесопастное повышение температуры для текущего рецепта
            // брать из выходного хранилища, если температура бесопастна или разрешен перегрев. помещать в рабочее
            // недостающую массу добрать из входного хранилища

            Debug.Log("TransferCurrentRecipeIngredientsForBuild begin");

            var firstresult = CurrentWorkingOrder.results[0];
            var element = ElementLoader.GetElement(firstresult.material);
            float energyDelta = GameUtil.CalculateEnergyDeltaForElementChange(firstresult.amount, element.specificHeatCapacity, element.highTemp, outputTemperature) * thermalFudge;

            Debug.Log($"element = {element.name}, energyDelta = {energyDelta}");

            var pooledList = ListPool<GameObject, LiquidCooledFueledRefinery>.Allocate();
            outStorage.Find(coolantTag, pooledList);

            float remaining_mass = minCoolantMass;
            foreach (GameObject gameObject in pooledList)
            {
                var pickupable = gameObject.GetComponent<Pickupable>();
                var primaryElement = pickupable.PrimaryElement;
                float mass = primaryElement.Mass;
                float temperatureDelta = GameUtil.CalculateTemperatureChange(primaryElement.Element.specificHeatCapacity, minCoolantMass, -energyDelta);

                Debug.Log($"remaining_mass = {remaining_mass}, mass = {mass}, temperatureDelta = {temperatureDelta}");

                if (mass > 0 && (allowOverheating || (primaryElement.Temperature + temperatureDelta < primaryElement.Element.highTemp)))
                {
                    if (mass <= remaining_mass)
                    {
                        outStorage.Transfer(gameObject, buildStorage, false, true);
                        remaining_mass -= mass;
                    }
                    else
                    {
                        var take = pickupable.Take(remaining_mass);
                        buildStorage.Store(take.gameObject, true);
                        remaining_mass -= take.PrimaryElement.Mass;
                    }
                    if (remaining_mass <= 0)
                        break;
                }
            }
            pooledList.Recycle();

            Debug.Log("TransferCurrentRecipeIngredientsForBuild end");

            base.TransferCurrentRecipeIngredientsForBuild();
        }

        // todo: потом убрать
        public void Test(string s)
        {
            PUtil.LogDebug(s);
            PUtil.LogDebug("IsOperational: " + operational.IsOperational);
            PUtil.LogDebug("IsFunctional: " + operational.IsFunctional);
        }

        public bool GetCheckboxValue()
        {
            return allowOverheating;
        }

        public void SetCheckboxValue(bool value)
        {
            allowOverheating = value;
        }
    }
}
