using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace SquirrelGenerator
{
    internal sealed class SquirrelGeneratorPatches : KMod.UserMod2
    {
        private static Harmony harmonyInstance;

        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary();
            base.OnLoad(harmony);
            harmonyInstance = harmony;
            new PPatchManager(harmony).RegisterPatchClass(typeof(SquirrelGeneratorPatches));
            new POptions().RegisterOptions(this, typeof(SquirrelGeneratorOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            ModUtil.AddBuildingToPlanScreen(BUILD_CATEGORY.Power, SquirrelGeneratorConfig.ID, BUILD_SUBCATEGORY.generators, ManualGeneratorConfig.ID);
            Utils.AddBuildingToTechnology("Ranching", SquirrelGeneratorConfig.ID);
        }

        // добавить белкам новое поведение
        [HarmonyPatch(typeof(BaseSquirrelConfig), nameof(BaseSquirrelConfig.BaseSquirrel))]
        private static class BaseSquirrelConfig_BaseSquirrel
        {
            internal static void Postfix(GameObject __result, bool is_baby)
            {
                if (!is_baby)
                {
                    __result.AddOrGet<WheelRunningMonitor>();
                }
#if DEBUG
                Debug.Log($"{__result.PrefabID()} ChoreTable !!!");
                Debug.Log("id\tpriority\tinterrupt_priority");
                var entries = Traverse.Create(__result.GetComponent<ChoreConsumer>().choreTable).Field<ChoreTable.Entry[]>("entries").Value;
                foreach (var entry in entries)
                    Debug.Log($"{entry.choreType.Id}\t{entry.choreType.priority}\t{entry.choreType.interruptPriority}");
#endif
            }
            // внедряем новое поведение так чтобы оно могло быть прервано другими типичными беличими делами
            /*
            ChoreTable.Builder chore_table = new ChoreTable.Builder().Add(new DeathStates.Def()).Add(new AnimInterruptStates.Def())
                <блаблабла>
                .Add(new CallAdultStates.Def())
                .Add(new SeedPlantingStates.Def(блабла))
        +++     .PushInterruptGroup()
        +++     .Add(new WheelRunningStates.Def())
        +++     .PopInterruptGroup()
                .PopInterruptGroup()
                .Add(new IdleStates.Def());
            */
            private static ChoreTable.Builder Inject(ChoreTable.Builder builder)
            {
                return builder.PushInterruptGroup().Add(new WheelRunningStates.Def()).PopInterruptGroup();
            }

            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var pop = typeof(ChoreTable.Builder).GetMethodSafe(nameof(ChoreTable.Builder.PopInterruptGroup), false, PPatchTools.AnyArguments);
                var inject = typeof(BaseSquirrelConfig_BaseSquirrel).GetMethodSafe(nameof(Inject), true, PPatchTools.AnyArguments);
                if (pop != null && inject != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(pop))
                        {
                            instructions.Insert(i++, new CodeInstruction(OpCodes.Call, inject));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // задача поиска и посадки семян в любом случае, даже если нет подходящих семян
        // начинает выполняться и хочет прервать бег в колесе
        // добавляем проверку - нет семян -> белка хочет бегать сразу, без паузы
        // математически это не совсем правильно, но задача поиска семян достаточно редкая, так что сойдёт
        [HarmonyPatch(typeof(SeedPlantingStates), nameof(SeedPlantingStates.InitializeStates))]
        private static class SeedPlantingStates_InitializeStates
        {
            private static void Postfix(SeedPlantingStates __instance)
            {
                __instance.findSeed.Exit(CheckSeed);
            }
            private static void CheckSeed(SeedPlantingStates.Instance smi)
            {
                if (smi.targetSeed == null)
                {
                    smi.GetSMI<WheelRunningMonitor.StatesInstance>()?.SetSearchTimeImmediately();
                    WheelRunningStates.PrioritizeUpdateBrain(smi);
                }
            }
        }

        // патч совместимости для мода Lagoo (https://steamcommunity.com/sharedfiles/filedetails/?id=2025986309)
        [PLibMethod(RunAt.AfterModsLoad, RequireAssembly = "LagooMerged", RequireType = "Lagoo.BaseLagooConfig")]
        private static void LagooPatch()
        {
            var BaseLagooConfig = PPatchTools.GetTypeSafe("Lagoo.BaseLagooConfig", "LagooMerged");
            if (BaseLagooConfig != null)
            {
                PUtil.LogDebug("'Lagoo' found, trying to apply a compatibility patch.");

                var postfix = new HarmonyMethod(typeof(BaseSquirrelConfig_BaseSquirrel), nameof(BaseSquirrelConfig_BaseSquirrel.Postfix));
                harmonyInstance.Patch(BaseLagooConfig, "BaseLagoo", null, postfix);

                var transpiler = new HarmonyMethod(typeof(BaseSquirrelConfig_BaseSquirrel), nameof(BaseSquirrelConfig_BaseSquirrel.Transpiler));
                harmonyInstance.PatchTranspile(BaseLagooConfig, "BaseLagoo", transpiler);
            }
        }
    }
}
