using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Harmony;
using UnityEngine;

using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace SquirrelGenerator
{
    internal static class SquirrelGeneratorPatches
    {
        private static HarmonyInstance harmonyInstance;
        public static void PrePatch(HarmonyInstance instance)
        {
            harmonyInstance = instance;
        }

        public static void OnLoad()
        {
            PUtil.InitLibrary();
            PUtil.RegisterPatchClass(typeof(SquirrelGeneratorPatches));
            POptions.RegisterOptions(typeof(SquirrelGeneratorOptions));
        }

        [PLibMethod(RunAt.AfterModsLoad)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            Utils.AddBuildingToPlanScreen("Power", SquirrelGeneratorConfig.ID, SolarPanelConfig.ID);
            Utils.AddBuildingToTechnology("AnimalControl", SquirrelGeneratorConfig.ID);
        }

        // добавить белкам новое поведение
        [HarmonyPatch(typeof(BaseSquirrelConfig), nameof(BaseSquirrelConfig.BaseSquirrel))]
        internal static class BaseSquirrelConfig_BaseSquirrel
        {
            internal static void Postfix(ref GameObject __result, bool is_baby)
            {
                if (!is_baby)
                {
                    var def = __result.AddOrGetDef<WheelRunningMonitor.Def>();
                    def.searchMinInterval = SquirrelGeneratorOptions.Instance.SearchMinInterval;
                    def.searchMaxInterval = SquirrelGeneratorOptions.Instance.SearchMaxInterval;
                }
            }

            /*
            ChoreTable.Builder chore_table = new ChoreTable.Builder().Add(new DeathStates.Def(), true).Add(new AnimInterruptStates.Def(), true)
                <блаблабла>
                .Add(new CallAdultStates.Def(), true)
        +++     .PushInterruptGroup()
        +++     .Add(new WheelRunningStates.Def(), true)
        +++     .PopInterruptGroup()
                .Add(new SeedPlantingStates.Def(), true)
                .PopInterruptGroup()
                .Add(new IdleStates.Def(), true);
            */
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                ConstructorInfo constructor = typeof(SeedPlantingStates.Def).GetConstructors()[0];
                string methodName = method.DeclaringType.FullName + "." + method.Name;
                bool result = false;
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction instruction = instructionsList[i];
                    if (instruction.opcode == OpCodes.Newobj && (ConstructorInfo)instruction.operand == constructor)
                    {
#if DEBUG
                        PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                        yield return new CodeInstruction(OpCodes.Callvirt, typeof(ChoreTable.Builder).GetMethod(nameof(ChoreTable.Builder.PushInterruptGroup), new Type[] { }));
                        yield return new CodeInstruction(OpCodes.Newobj, typeof(WheelRunningStates.Def).GetConstructors()[0]);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return new CodeInstruction(OpCodes.Callvirt, typeof(ChoreTable.Builder).GetMethod(nameof(ChoreTable.Builder.Add), new Type[] { typeof(StateMachine.BaseDef), typeof(bool) }));
                        yield return new CodeInstruction(OpCodes.Callvirt, typeof(ChoreTable.Builder).GetMethod(nameof(ChoreTable.Builder.PopInterruptGroup), new Type[] { }));
                        result = true;
                    }
                    yield return instruction;
                }
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
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
