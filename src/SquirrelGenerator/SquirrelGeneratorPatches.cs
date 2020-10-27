using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using STRINGS;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace SquirrelGenerator
{
    internal static class SquirrelGeneratorPatches
    {
        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        internal static class GeneratedBuildings_LoadGeneratedBuildings
        {
            private static void Prefix()
            {
                Utils.AddBuildingToPlanScreen("Power", SquirrelGeneratorConfig.ID, "SolarPanel");
            }
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        internal static class Db_Initialize
        {
            private static void Prefix()
            {
                Utils.AddBuildingToTechnology("AnimalControl", SquirrelGeneratorConfig.ID);
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        internal static class Localization_Initialize
        {
            private static void Postfix(Localization.Locale ___sLocale)
            {
                Utils.InitLocalization(typeof(STRINGS), ___sLocale);
                // чтобы подтянуть название белки из локализации
                Utils.ReplaceLocString(ref STRINGS.BUILDINGS.PREFABS.SQUIRRELGENERATOR.DESC, STRINGS.SQUIRREL, CREATURES.SPECIES.SQUIRREL.NAME);
                Utils.ReplaceLocString(ref STRINGS.BUILDINGS.PREFABS.SQUIRRELGENERATOR.EFFECT, STRINGS.SQUIRREL, CREATURES.SPECIES.SQUIRREL.NAME);
                LocString.CreateLocStringKeys(typeof(STRINGS.BUILDINGS));

                Config.Initialize();
            }
        }

        // добавить белкам новое поведение
        [HarmonyPatch(typeof(BaseSquirrelConfig), "BaseSquirrel")]
        internal static class BaseSquirrelConfig_BaseSquirrel
        {
            private static void Postfix(ref GameObject __result, bool is_baby)
            {
                if (!is_baby)
                {
                    WheelRunningMonitor.Def def = __result.AddOrGetDef<WheelRunningMonitor.Def>();
                    def.searchMinInterval = Config.Get().SearchMinInterval;
                    def.searchMaxInterval = Config.Get().SearchMaxInterval;
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
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction instruction = instructionsList[i];
                    if (instruction.opcode == OpCodes.Newobj && (ConstructorInfo)instruction.operand == typeof(SeedPlantingStates.Def).GetConstructors()[0])
                    {
                        yield return new CodeInstruction(OpCodes.Callvirt, typeof(ChoreTable.Builder).GetMethod("PushInterruptGroup", new Type[] { }));
                        yield return new CodeInstruction(OpCodes.Newobj, typeof(WheelRunningStates.Def).GetConstructors()[0]);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return new CodeInstruction(OpCodes.Callvirt, typeof(ChoreTable.Builder).GetMethod("Add", new Type[] { typeof(StateMachine.BaseDef), typeof(bool) }));
                        yield return new CodeInstruction(OpCodes.Callvirt, typeof(ChoreTable.Builder).GetMethod("PopInterruptGroup", new Type[] { }));
                    }
                    yield return instruction;
                }
            }
        }
    }
}
