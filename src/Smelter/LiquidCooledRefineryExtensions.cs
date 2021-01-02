using UnityEngine;

namespace Smelter
{
    public static class LiquidCooledRefineryExtensions
    {
        // формулы расчета тепла и прироста температуры для порции хладагента массой minCoolantMass
        // для правильности расчета должны быть аналогичны формулам внутри LiquidCooledRefinery.SpawnOrderProduct
        public static float CalculateEnergyDelta(this LiquidCooledRefinery @this, ComplexRecipe recipe)
        {
            var firstresult = recipe.results[0];
            var element = ElementLoader.GetElement(firstresult.material);
            return GameUtil.CalculateEnergyDeltaForElementChange(firstresult.amount, element.specificHeatCapacity, element.highTemp, @this.outputTemperature) * @this.thermalFudge;
        }

        public static float CalculateTemperatureDelta(this LiquidCooledRefinery @this, PrimaryElement primaryElement, float energyDelta)
        {
            return GameUtil.CalculateTemperatureChange(primaryElement.Element.specificHeatCapacity, @this.minCoolantMass, -energyDelta);
        }

        // сброс перегретого хладагента
        internal static void DropOverheatedCoolant(this LiquidCooledRefinery @this)
        {
            var pooledList = ListPool<GameObject, LiquidCooledRefinery>.Allocate();
            var position = @this.transform.GetPosition() + @this.outputOffset;
            @this.outStorage.Find(@this.coolantTag, pooledList);
            foreach (GameObject gameObject in pooledList)
            {
                var primaryElement = gameObject.GetComponent<PrimaryElement>();
                if (primaryElement.Temperature > primaryElement.Element.highTemp)
                {
                    @this.outStorage.Drop(gameObject)?.GetComponent<Dumpable>()?.Dump(position);
                }
            }
            pooledList.Recycle();
        }

        // переиспользование отработанного хладагента
        internal static void ReuseCoolant(this LiquidCooledRefinery @this, bool allowOverheating = false)
        {
            // высчитать бесопастное повышение температуры для текущего рецепта
            // брать из выходного хранилища, если температура бесопастна или разрешен перегрев. помещать в рабочее
            float energyDelta = @this.CalculateEnergyDelta(@this.CurrentWorkingOrder);

            var pooledList = ListPool<GameObject, LiquidCooledFueledRefinery>.Allocate();
            @this.outStorage.Find(@this.coolantTag, pooledList);

            float remaining_mass = @this.minCoolantMass;
            foreach (GameObject gameObject in pooledList)
            {
                var pickupable = gameObject.GetComponent<Pickupable>();
                var primaryElement = pickupable.PrimaryElement;
                float mass = primaryElement.Mass;
                float temperatureDelta = @this.CalculateTemperatureDelta(primaryElement, energyDelta);

                if (mass > 0 && (allowOverheating || (primaryElement.Temperature + temperatureDelta < primaryElement.Element.highTemp)))
                {
                    if (mass <= remaining_mass)
                    {
                        @this.outStorage.Transfer(gameObject, @this.buildStorage, false, true);
                        remaining_mass -= mass;
                    }
                    else
                    {
                        var take = pickupable.Take(remaining_mass);
                        @this.buildStorage.Store(take.gameObject, true);
                        remaining_mass -= take.PrimaryElement.Mass;
                    }
                    if (remaining_mass <= 0)
                        break;
                }
            }
            pooledList.Recycle();

            // недостающую массу добрать из входного хранилища
            while (remaining_mass > 0f && @this.buildStorage.GetAmountAvailable(@this.coolantTag) < @this.minCoolantMass && @this.inStorage.GetAmountAvailable(@this.coolantTag) > 0f)
            {
                float mass = @this.inStorage.Transfer(@this.buildStorage, @this.coolantTag, remaining_mass, false, true);
                remaining_mass -= mass;
            }
        }
    }
}
