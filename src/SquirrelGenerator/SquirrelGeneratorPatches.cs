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

namespace SquirrelGenerator
{
    internal sealed class SquirrelGeneratorPatches : KMod.UserMod2
    {
        private static Harmony harmonyInstance;

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmonyInstance = harmony;
            PUtil.InitLibrary();
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
            Utils.AddBuildingToPlanScreen("Power", SquirrelGeneratorConfig.ID, SolarPanelConfig.ID);
            Utils.AddBuildingToTechnology("Ranching", SquirrelGeneratorConfig.ID);
        }

        // добавить белкам новое поведение
        [HarmonyPatch(typeof(BaseSquirrelConfig), nameof(BaseSquirrelConfig.BaseSquirrel))]
        internal static class BaseSquirrelConfig_BaseSquirrel
        {
            internal static void Postfix(GameObject __result, bool is_baby)
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
            private static ChoreTable.Builder Inject(ChoreTable.Builder builder)
            {
                return builder.PushInterruptGroup().Add(new WheelRunningStates.Def()).PopInterruptGroup();
            }

            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;
                var constructor = typeof(SeedPlantingStates.Def).GetConstructors()[0];
                bool result = false;
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction instruction = instructionsList[i];
                    if (instruction.opcode == OpCodes.Newobj && (instruction.operand is ConstructorInfo info) && info == constructor)
                    {
                        yield return new CodeInstruction(OpCodes.Call, typeof(BaseSquirrelConfig_BaseSquirrel).GetMethodSafe(nameof(Inject), true, PPatchTools.AnyArguments));
                        result = true;
#if DEBUG
                        PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
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
