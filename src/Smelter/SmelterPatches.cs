using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
            var options = new POptions();
            if (DlcManager.IsExpansion1Active())
                options.RegisterOptions(this, typeof(SmelterOptionsExpansion1));
            else
                options.RegisterOptions(this, typeof(SmelterOptions));
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

        // хоть и LiquidCooledFueledRefinery наследуется от LiquidCooledRefinery
        // но не весь функционал нам нужен. нужно избежать инициализации ненужных штук
        // поэтому подменяем вызов "base.OnSpawn" на "base.base.OnSpawn"
        [HarmonyPatch(typeof(LiquidCooledFueledRefinery), "OnSpawn")]
        internal static class LiquidCooledFueledRefinery_OnSpawn
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var @base = typeof(LiquidCooledRefinery).GetMethodSafe("OnSpawn", false, PPatchTools.AnyArguments);
                var basebase = typeof(ComplexFabricator).GetMethodSafe("OnSpawn", false, PPatchTools.AnyArguments);
                bool result = false;
                if (@base != null && basebase != null)
                {
                    for (int i = 0; i < instructionsList.Count(); i++)
                    {
                        var instruction = instructionsList[i];
                        if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info) && info == @base)
                        {
                            instructionsList[i] = new CodeInstruction(OpCodes.Call, basebase);
                            result = true;
                            break;
#if DEBUG
                            PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                        }
                    }
                }
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
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
