using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace Smelter
{
    internal sealed class SmelterPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(SmelterPatches));
            new POptions().RegisterOptions(this, typeof(SmelterOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
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

        // газообразные продукты нужно выпускать в атмосферу
        // ограничимся только теми постройкаи куда мы добавили такие рецепты
        private static List<string> fabricators = new List<string>() { SmelterConfig.ID, MetalRefineryConfig.ID, KilnConfig.ID };

        [HarmonyPatch(typeof(ComplexFabricator), "SpawnOrderProduct")]
        internal static class ComplexFabricator_SpawnOrderProduct
        {
            private static void Postfix(ComplexFabricator __instance, List<GameObject> __result)
            {
                if (fabricators.Contains(__instance.PrefabID().Name))
                {
                    foreach (GameObject gameObject in __result)
                    {
                        if (gameObject?.GetComponent<PrimaryElement>().Element.IsGas ?? false)
                        {
                            gameObject.GetComponent<Dumpable>()?.Dump();
                        }
                    }
                }
            }
        }

        // сброс перегретого хладагента
        [HarmonyPatch(typeof(LiquidCooledRefinery), "SpawnOrderProduct")]
        internal static class LiquidCooledRefinery_SpawnOrderProduct
        {
            private static void Postfix(LiquidCooledRefinery __instance)
            {
                if (__instance is LiquidCooledFueledRefinery || SmelterOptions.Instance.MetalRefineryDropOverheatedCoolant)
                {
                    __instance.DropOverheatedCoolant();
                }
            }
        }

        // переиспользование отработанного хладагента
        [HarmonyPatch(typeof(LiquidCooledRefinery), "TransferCurrentRecipeIngredientsForBuild")]
        internal static class LiquidCooledRefinery_TransferCurrentRecipeIngredientsForBuild
        {
            private static void Prefix(LiquidCooledRefinery __instance)
            {
                var lcfr = (__instance as LiquidCooledFueledRefinery);
                bool allowOverheating = lcfr?.AllowOverheating ?? false;
                if (lcfr != null || SmelterOptions.Instance.MetalRefineryReuseCoolant)
                {
                    __instance.ReuseCoolant(allowOverheating);
                }
            }
        }
    }
}
