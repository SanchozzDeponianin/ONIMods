using System;
using System.Collections.Generic;
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
        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(SmartLogicDoorsPatches));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
            // отложенный патчинг. чтобы не высвать раньше времени статический коструктор Door
            harmony.PatchAll();
        }

        // добавляем компонент в почти все двери после инициализации всех построек
        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            foreach (var go in Assets.GetPrefabsWithComponent<Door>())
            {
                if (go.TryGetComponent(out Door door) && door.allowAutoControl && go.TryGetComponent(out LogicPorts _))
                    go.AddOrGet<SmartLogicDoor>();
            }
        }

        [PLibMethod(RunAt.OnDetailsScreenInit)]
        private static void OnDetailsScreenInit()
        {
            PUIUtils.AddSideScreenContent<SmartLogicDoorSideScreen>();
        }

        // аррргхх !!! ну почему сортировка сидэскреенов сделана через жёппу ? такая боль добавить свой экран в нужное место
        // ишшо и хармони приглючивает
        [HarmonyPatch(typeof(SideScreenContent), nameof(SideScreenContent.GetSideScreenSortOrder))]
        private static class SideScreenContent_GetSideScreenSortOrder
        {
            private static bool Prepare() => Environment.OSVersion.Platform.Equals(PlatformID.Win32NT);
            private static void Postfix(SideScreenContent __instance, ref int __result)
            {
                if (__instance is DoorToggleSideScreen)
                    __result = 20;
            }
        }

        // при изменении сигнала, заменяем хардкоженые параметры состояния двери на наши настроеные
        [HarmonyPatch(typeof(Door), nameof(Door.OnLogicValueChanged))]
        private static class Door_OnLogicValueChanged
        {
            private static Door.ControlState GetDoorState(Door door, bool IsActive)
            {
                if (door.TryGetComponent(out SmartLogicDoor sld))
                    return IsActive ? sld.GreenState : sld.RedState;
                else
                    return IsActive ? Door.ControlState.Opened : Door.ControlState.Locked;
            }
            /*
            --- this.requestedState = (LogicCircuitNetwork.IsBitActive(0, newValue) ? Door.ControlState.Opened : Door.ControlState.Locked);
            +++ this.requestedState = GetDoorState(this, LogicCircuitNetwork.IsBitActive(0, newValue));
            */
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL)
            {
                return TranspilerUtils.Wrap(instructions, original, IL, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions, ILGenerator IL, TranspilerUtils.Log log)
            {
                var isBitActive = typeof(LogicCircuitNetwork).GetMethodSafe(nameof(LogicCircuitNetwork.IsBitActive), true, PPatchTools.AnyArguments);
                var getDoorState = typeof(Door_OnLogicValueChanged).GetMethodSafe(nameof(GetDoorState), true, PPatchTools.AnyArguments);
                var requestedState = typeof(Door).GetFieldSafe("requestedState", false);

                bool result1 = false, result2 = false;
                if (isBitActive != null && getDoorState != null && requestedState != null)
                {
                    var label = IL.DefineLabel();
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        var instruction = instructions[i];
                        if (instruction.Calls(isBitActive))
                        {
                            i++;
                            if (instructions[i].IsStloc())
                            {
                                var ldloc = TranspilerUtils.GetMatchingLoadInstruction(instructions[i]);
                                instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                                instructions.Insert(++i, new CodeInstruction(OpCodes.Dup));
                                instructions.Insert(++i, ldloc);
                                instructions.Insert(++i, new CodeInstruction(OpCodes.Call, getDoorState));
                                instructions.Insert(++i, new CodeInstruction(OpCodes.Br_S, label));
                                result1 = true;
                                log.Step(1);
                            }
                        }
                        else if (instruction.StoresField(requestedState))
                        {
                            instruction.labels.Add(label);
                            result2 = true;
                            log.Step(2);
                        }
                    }
                }
                return result1 && result2;
            }
        }

        // копирование настроек
        // сдесь, чтобы блокировать нежелательное изменение состояния двери
        // если к ней подключен логический провод
        [HarmonyPatch(typeof(Door), "OnCopySettings")]
        private static class Door_OnCopySettings
        {
            internal static bool Prefix(Door __instance, object data)
            {
                if (data is GameObject go && go.TryGetComponent(out SmartLogicDoor other) && __instance.TryGetComponent(out SmartLogicDoor @this))
                {
                    @this.GreenState = other.GreenState;
                    @this.RedState = other.RedState;
                    if (@this.ApplyControlState())
                        return false;
                }
                return true;
            }
        }
    }
}
