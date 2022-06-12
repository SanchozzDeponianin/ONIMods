using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;

namespace SmartLogicDoors
{
    internal sealed class SmartLogicDoorsPatches : KMod.UserMod2
    {
        private static Harmony harmony;
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            SmartLogicDoorsPatches.harmony = harmony;
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(SmartLogicDoorsPatches));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
            // отложенный патчинг. чтобы не высвать раньше времени статический коструктор Door
            harmony.PatchTranspile(typeof(Door), nameof(Door.OnLogicValueChanged),
                new HarmonyMethod(typeof(Door_OnLogicValueChanged), nameof(Door_OnLogicValueChanged.Transpiler)));
            harmony.Patch(typeof(Door), "OnCopySettings", prefix:
                new HarmonyMethod(typeof(Door_OnCopySettings), nameof(Door_OnCopySettings.Prefix)));
        }

        // добавление сидескреена
        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        private static class DetailsScreen_OnPrefabInit
        {
            private static void Postfix(List<DetailsScreen.SideScreenRef> ___sideScreens)
            {
                PUIUtils.AddSideScreenContent<SmartLogicDoorSideScreen>();
            }
        }

        // аррргхх !!! ну почему сортировка сидэскреенов сделана через жёппу ? такая боль добавить свой экран в нужное место
        [HarmonyPatch(typeof(SideScreenContent), nameof(SideScreenContent.GetSideScreenSortOrder))]
        private static class SideScreenContent_GetSideScreenSortOrder
        {
            private static void Postfix(SideScreenContent __instance, ref int __result)
            {
                if (__instance is DoorToggleSideScreen)
                    __result = 20;
            }
        }

        // добавляем компонент в почти все двери после инициализации всех построек
        [HarmonyPatch(typeof(BuildingConfigManager), nameof(BuildingConfigManager.ConfigurePost))]
        private static class BuildingConfigManager_ConfigurePost
        {
            private static void Postfix()
            {
                foreach (var go in Assets.GetPrefabsWithComponent<Door>())
                {
                    if (go.GetComponent<Door>().allowAutoControl && go.GetComponent<LogicPorts>() != null)
                        go.AddOrGet<SmartLogicDoor>();
                }
            }
        }

        // при изменении сигнала, заменяем хардкоженые параметры состояния двери на наши настроеные
        //[HarmonyPatch(typeof(Door), nameof(Door.OnLogicValueChanged))]
        private static class Door_OnLogicValueChanged
        {
            private static Door.ControlState GetDoorState(Door door, bool IsActive)
            {
                var sld = door.GetComponent<SmartLogicDoor>();
                if (sld == null)
                    return IsActive ? Door.ControlState.Opened : Door.ControlState.Locked;
                else
                    return IsActive ? sld.GreenState : sld.RedState;
            }
            /*
            --- this.requestedState = (LogicCircuitNetwork.IsBitActive(0, newValue) ? Door.ControlState.Opened : Door.ControlState.Locked);
            +++ this.requestedState = GetDoorState(this, LogicCircuitNetwork.IsBitActive(0, newValue));
            */
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator IL)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var isBitActive = typeof(LogicCircuitNetwork).GetMethodSafe(nameof(LogicCircuitNetwork.IsBitActive), true, PPatchTools.AnyArguments);
                var getDoorState = typeof(Door_OnLogicValueChanged).GetMethodSafe(nameof(GetDoorState), true, PPatchTools.AnyArguments);
                var requestedState = typeof(Door).GetFieldSafe("requestedState", false);

                bool result1 = false, result2 = false;
                if (isBitActive != null && getDoorState != null && requestedState != null)
                {
                    var label = IL.DefineLabel();
                    for (int i = 0; i < instructionsList.Count; i++)
                    {
                        var instruction = instructionsList[i];
                        if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info) && isBitActive == info)
                        {
                            i++;
                            var ldloc = Utils.GetMatchingLoadInstruction(instructionsList[i]);
                            if (ldloc != null)
                            {
                                instructionsList.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                                instructionsList.Insert(++i, new CodeInstruction(OpCodes.Dup));
                                instructionsList.Insert(++i, ldloc);
                                instructionsList.Insert(++i, new CodeInstruction(OpCodes.Call, getDoorState));
                                instructionsList.Insert(++i, new CodeInstruction(OpCodes.Br_S, label));
#if DEBUG
                                Debug.Log($"'{methodName}' Transpiler#1 injected");
#endif
                                result1 = true;
                            }
                        }
                        else if (instruction.opcode == OpCodes.Stfld && (instruction.operand is FieldInfo finfo) && requestedState == finfo)
                        {
                            instruction.labels.Add(label);
#if DEBUG
                                Debug.Log($"'{methodName}' Transpiler#2 injected");
#endif
                            result2 = true;
                        }
                    }
                }
                if (!(result1 && result2))
                {
                    Debug.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }
        }

        // копирование настроек
        // сдесь, чтобы блокировать нежелательное изменение состояния двери
        // если к ней подключен логический провод
        //[HarmonyPatch(typeof(Door), "OnCopySettings")]
        private static class Door_OnCopySettings
        {
            internal static bool Prefix(Door __instance, object data)
            {
                var @this = __instance.GetComponent<SmartLogicDoor>();
                if (@this != null)
                {
                    var other = ((GameObject)data)?.GetComponent<SmartLogicDoor>();
                    if (other != null)
                    {
                        @this.GreenState = other.GreenState;
                        @this.RedState = other.RedState;
                        if (@this.ApplyControlState())
                            return false;
                    }
                }
                return true;
            }
        }
    }
}
