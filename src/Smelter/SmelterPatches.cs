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
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(SmelterPatches));
            new POptions().RegisterOptions(this, typeof(SmelterOptions));
            new KAnimGroupManager().RegisterInteractAnims(SmelterConfig.ANIM_WORK);
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
            PGameUtils.CopySoundsToAnim(SmelterConfig.ANIM, "smelter_kanim");
            Utils.LoadEmbeddedAudioSheet("AudioSheets/SFXTags_Buildings.csv");
            Utils.LoadEmbeddedAudioSheet("AudioSheets/SFXTags_Duplicants.csv");
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            Utils.AddBuildingToPlanScreen(BUILD_CATEGORY.Refining, SmelterConfig.ID, BUILD_SUBCATEGORY.materials, KilnConfig.ID);
            Utils.AddBuildingToTechnology("BasicRefinement", SmelterConfig.ID);
        }

        // хоть и LiquidCooledFueledRefinery наследуется от LiquidCooledRefinery
        // но не весь функционал нам нужен. нужно избежать инициализации ненужных штук
        // поэтому подменяем вызов "base.OnSpawn" на "base.base.OnSpawn"
        [HarmonyPatch(typeof(LiquidCooledFueledRefinery), "OnSpawn")]
        private static class LiquidCooledFueledRefinery_OnSpawn
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Transpile(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var @base = typeof(LiquidCooledRefinery).GetMethodSafe("OnSpawn", false);
                var basebase = typeof(ComplexFabricator).GetMethodSafe("OnSpawn", false);
                if (@base != null && basebase != null)
                {
                    for (int i = 0; i < instructions.Count(); i++)
                    {
                        if (instructions[i].Calls(@base))
                        {
                            instructions[i] = new CodeInstruction(OpCodes.Call, basebase);
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // проверяем отработанного хладагента перед началом следующего заказа.
        // пытаемся предотвратить сбой при отключении в процессе работы
        [HarmonyPatch(typeof(ComplexFabricator), "StartWorkingOrder")]
        private static class ComplexFabricator_StartWorkingOrder
        {
            private static bool Prefix(ComplexFabricator __instance, Operational ___operational)
            {

                (__instance as LiquidCooledFueledRefinery)?.CheckCoolantIsTooHot();
                return ___operational.IsOperational;
            }
        }

        // газообразные продукты нужно выпускать в атмосферу
        // ограничимся только теми постройкаи куда мы добавили такие рецепты
        private static readonly List<string> fabricators = new List<string>() { SmelterConfig.ID, MetalRefineryConfig.ID };

        [HarmonyPatch(typeof(ComplexFabricator), "SpawnOrderProduct")]
        private static class ComplexFabricator_SpawnOrderProduct
        {
            private static void Postfix(ComplexFabricator __instance, List<GameObject> __result)
            {
                if (fabricators.Contains(__instance.PrefabID().Name))
                {
                    foreach (GameObject gameObject in __result)
                    {
                        if (gameObject.TryGetComponent<PrimaryElement>(out var primaryElement) && primaryElement.Element.IsGas
                            && gameObject.TryGetComponent<Dumpable>(out var dumpable))
                            dumpable.Dump();
                    }
                }
            }
        }

        // сброс перегретого хладагента
        [HarmonyPatch(typeof(LiquidCooledRefinery), "SpawnOrderProduct")]
        private static class LiquidCooledRefinery_SpawnOrderProduct
        {
            private static void Postfix(LiquidCooledRefinery __instance)
            {
                if (__instance is LiquidCooledFueledRefinery || SmelterOptions.Instance.features.MetalRefinery_Drop_Overheated_Coolant)
                {
                    __instance.DropOverheatedCoolant();
                }
            }
        }

        // переиспользование отработанного хладагента
        [HarmonyPatch(typeof(LiquidCooledRefinery), "TransferCurrentRecipeIngredientsForBuild")]
        private static class LiquidCooledRefinery_TransferCurrentRecipeIngredientsForBuild
        {
            private static void Prefix(LiquidCooledRefinery __instance)
            {
                var lcfr = (__instance as LiquidCooledFueledRefinery);
                bool allowOverheating = lcfr?.AllowOverheating ?? false;
                if (lcfr != null || SmelterOptions.Instance.features.MetalRefinery_Reuse_Coolant)
                {
                    __instance.ReuseCoolant(allowOverheating);
                }
            }
        }
    }
}
