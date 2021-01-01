using Harmony;

using SanchozzONIMods.Lib;
using PeterHan.PLib;

namespace Smelter
{
    internal static class SmelterPatches
    {
        public static void OnLoad()
        {
            PUtil.InitLibrary();
            PUtil.RegisterPatchClass(typeof(SmelterPatches));
            // todo: обдумать нужность опций. а) отключение доп рецептов. б) переиспользование воды в электроплавильне
            //POptions.RegisterOptions(typeof(SquirrelGeneratorOptions));
        }

        [PLibMethod(RunAt.AfterModsLoad)]
        private static void InitLocalization()
        {
            Utils.InitLocalization(typeof(STRINGS)/*, "", true*/);
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void AddBuilding()
        {
            Utils.AddBuildingToPlanScreen("Refining", SmelterConfig.ID, KilnConfig.ID);
            Utils.AddBuildingToTechnology("BasicRefinement", SmelterConfig.ID);
        }

        // добавляем рецепты
        [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
        internal static class GeneratedBuildings_LoadGeneratedBuildings
        {
            private static void Postfix()
            {
                SmelterConfig.ConfigureRecipes();
            }
        }

        // проверяем отработанного хладагента перед началом следующего заказа.
        // пытаемся предотвратить сбой при отключении в процессе работы
        [HarmonyPatch(typeof(ComplexFabricator), "StartWorkingOrder")]
        internal static class ComplexFabricator_StartWorkingOrder
        {
            private static bool Prefix(ComplexFabricator __instance, Operational ___operational)
            {
                
                (__instance as LiquidCooledFueledRefinery)?.CheckCoolantIsTooHot();
                return ___operational.IsOperational;
            }
        }

        // todo: газообразные продукты нужно выпускать в атмосферу
    }
}
