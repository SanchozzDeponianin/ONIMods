using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.PatchManager;

namespace TravelTubesFlydo
{
    internal sealed class Patches : KMod.UserMod2
    {
        public const string TUBE_ANIM = "flydo_loco_tube_kanim";
        private delegate UtilityConnections TestTubeConnections(UtilityConnections connection, int tube_cell);
        private delegate UtilityConnections TestBridgeConnections(UtilityConnections connection, int bridge_cell, ref NavGrid.Transition transition);
        private static TestTubeConnections testTubeConnections;
        private static TestBridgeConnections testBridgeConnections;

        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new KAnimGroupManager().RegisterAnimsTogether("swoopy_bot_kanim", TUBE_ANIM);
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
            var type = PPatchTools.GetTypeSafe("TravelTubesExpanded.Patches+NavGrid_Transition_IsValid", "TravelTubesExpanded");
            if (type != null)
            {
                testTubeConnections = type.TryDetour<TestTubeConnections>(nameof(TestTubeConnections));
                testBridgeConnections = type.TryDetour<TestBridgeConnections>(nameof(TestBridgeConnections));
            }
            harmony.PatchAll();
        }

        // добавляем транзицыи
        [HarmonyPatch]
        private static class NavGrid_Constructor
        {
            private static MethodBase TargetMethod() => typeof(NavGrid).GetConstructors()[0];

            internal static void Prefix(string id, ref NavGrid.Transition[] transitions,
                ref NavGrid.NavTypeData[] nav_type_data, ref NavTableValidator[] validators)
            {
                if (id == "RobotFlyerGrid1x1")
                {
                    // незначительно повысим стоимость летальных транзицый
                    for (int i = 0; i < transitions.Length; i++)
                    {
                        if (transitions[i].start == NavType.Hover && transitions[i].end == NavType.Hover)
                            transitions[i].cost += 1;
                    }
                    // чтобы дёрнуть методы эксземпляра без this
                    transitions = default(GameNavGrids).CombineTransitions(transitions, default(GameNavGrids).MirrorTransitions(FlydoTransitions()));
                    nav_type_data = nav_type_data.Append(new NavGrid.NavTypeData { navType = NavType.Tube, idleAnim = "idle_loop" });
                    validators = validators.Append(new GameNavGrids.TubeValidator());
                }
            }

            private static CellOffset[] no_cell_offset => new CellOffset[0];
            private static NavOffset[] no_nav_offset => new NavOffset[0];
            private static NavGrid.Transition[] FlydoTransitions()
            {
                var tube_transitions = new[]
                {
                    // унутре трубы
                    new NavGrid.Transition(NavType.Tube, NavType.Tube, 1, 0, NavAxis.NA, true, false, false, 1, string.Empty,
                        no_cell_offset, no_cell_offset, no_nav_offset, no_nav_offset),
                    new NavGrid.Transition(NavType.Tube, NavType.Tube, 0, 1, NavAxis.NA, true, false, false, 1, string.Empty,
                        no_cell_offset, no_cell_offset, no_nav_offset, no_nav_offset),
                    new NavGrid.Transition(NavType.Tube, NavType.Tube, 0, -1, NavAxis.NA, true, false, false, 1, string.Empty,
                        no_cell_offset, no_cell_offset, no_nav_offset, no_nav_offset),
                    // с проворотом
                    new NavGrid.Transition(NavType.Tube, NavType.Tube, 1, 1, NavAxis.Y, false, false, false, 2, string.Empty,
                        no_cell_offset, no_cell_offset, new NavOffset[]{ new NavOffset(NavType.Tube, 0, 1) }, no_nav_offset, animSpeed: 2.2f),
                    new NavGrid.Transition(NavType.Tube, NavType.Tube, 1, 1, NavAxis.X, false, false, false, 2, string.Empty,
                        no_cell_offset, no_cell_offset, new NavOffset[]{ new NavOffset(NavType.Tube, 1, 0) }, no_nav_offset, animSpeed: 2.2f),
                    new NavGrid.Transition(NavType.Tube, NavType.Tube, 1, -1, NavAxis.Y, false, false, false, 2, string.Empty,
                        no_cell_offset, no_cell_offset, new NavOffset[]{ new NavOffset(NavType.Tube, 0, -1) }, no_nav_offset, animSpeed: 2.2f),
                    new NavGrid.Transition(NavType.Tube, NavType.Tube, 1, -1, NavAxis.X, false, false, false, 2, string.Empty,
                        no_cell_offset, no_cell_offset, new NavOffset[]{ new NavOffset(NavType.Tube, 1, 0) }, no_nav_offset, animSpeed: 2.2f),
                    // из трубы
                    new NavGrid.Transition(NavType.Tube, NavType.Hover, 1, 0, NavAxis.NA, false, false, false, 1, string.Empty,
                        no_cell_offset, no_cell_offset, no_nav_offset, no_nav_offset),
                    new NavGrid.Transition(NavType.Tube, NavType.Hover, 0, 1, NavAxis.NA, false, false, false, 1, string.Empty,
                        no_cell_offset, no_cell_offset, no_nav_offset, no_nav_offset),
                    new NavGrid.Transition(NavType.Tube, NavType.Hover, 0, -1, NavAxis.NA, false, false, false, 1, string.Empty,
                        no_cell_offset, no_cell_offset, no_nav_offset, no_nav_offset),
                    // в трубу
                    new NavGrid.Transition(NavType.Hover, NavType.Tube, 1, 0, NavAxis.NA, false, false, false, 2, string.Empty,
                        no_cell_offset, no_cell_offset, no_nav_offset, no_nav_offset),
                    new NavGrid.Transition(NavType.Hover, NavType.Tube, 0, 1, NavAxis.NA, false, false, false, 2, string.Empty,
                        no_cell_offset, no_cell_offset, no_nav_offset, no_nav_offset),
                    new NavGrid.Transition(NavType.Hover, NavType.Tube, 0, -1, NavAxis.NA, false, false, false, 2, string.Empty,
                        no_cell_offset, no_cell_offset, no_nav_offset, no_nav_offset),
                    // из воды
                    new NavGrid.Transition(NavType.Swim, NavType.Tube, 1, 0, NavAxis.NA, false, false, false, 2, "hover_tube_1_0",
                        no_cell_offset, no_cell_offset, no_nav_offset, no_nav_offset),
                    new NavGrid.Transition(NavType.Swim, NavType.Tube, 0, 1, NavAxis.NA, false, false, false, 2, "hover_tube_0_1",
                        no_cell_offset, no_cell_offset, no_nav_offset, no_nav_offset),
                    new NavGrid.Transition(NavType.Swim, NavType.Tube, 0, -1, NavAxis.NA, false, false, false, 2, "hover_tube_0_-1",
                        no_cell_offset, no_cell_offset, no_nav_offset, no_nav_offset),
                };
                // длинный прыжок через скрещивание
                if (testTubeConnections != null)
                {
                    tube_transitions = tube_transitions.Append(
                    new NavGrid.Transition(NavType.Tube, NavType.Tube, 2, 0, NavAxis.NA, true, false, false, 2, "tube_tube_1_0",
                        no_cell_offset, new CellOffset[] { new CellOffset(1, 0) },
                        new NavOffset[] { new NavOffset(NavType.Tube, 1, 0) }, no_nav_offset, animSpeed: 0.5f));
                }
                return tube_transitions;
            }
        }

        // самопатч, callvirt => call
        [HarmonyPatch(typeof(NavGrid_Constructor), nameof(NavGrid_Constructor.Prefix))]
        private static class NavGrid_Constructor_Prefix
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Callvirt && instr.operand is MethodBase method && method.DeclaringType == typeof(GameNavGrids))
                        instr.opcode = OpCodes.Call;
                }
                return instructions;
            }
        }

        // валидируем транзицыи входа в трубу
        [HarmonyPatch(typeof(NavGrid.Transition), nameof(NavGrid.Transition.IsValid))]
        private static class NavGrid_Transition_IsValid
        {
            private static void Postfix(ref NavGrid.Transition __instance, int cell, ref int __result)
            {
                if (Grid.IsValidCell(__result) && __instance.end == NavType.Tube
                    && (__instance.start == NavType.Hover || __instance.start == NavType.Swim))
                {
                    var connections = Game.Instance.travelTubeSystem.GetConnections(__result, false);
                    if (testTubeConnections != null)
                        connections = testTubeConnections(connections, __result);
                    if (testBridgeConnections != null)
                        connections = testBridgeConnections(connections, __result, ref __instance);
                    if (connections != UtilityConnectionsExtensions.DirectionFromToCell(cell, __result))
                        __result = Grid.InvalidCell;
                }
            }
        }

        [HarmonyPatch(typeof(FetchDroneConfig), nameof(FetchDroneConfig.CreatePrefab))]
        private static class FetchDroneConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                var kbac = __result.GetComponent<KBatchedAnimController>();
                kbac.animFiles = kbac.animFiles.Append(Assets.GetAnim(TUBE_ANIM));
                __result.GetComponent<KPrefabID>().prefabSpawnFn += OnSpawn;
            }

            private static void OnSpawn(GameObject inst)
            {
                if (inst.TryGetComponent(out Navigator navigator))
                    navigator.transitionDriver.overrideLayers.Add(new FlydoTubeTransitionLayer(navigator));
            }
        }

        // не считать утопленым если унутре трубы
        [HarmonyPatch(typeof(DrowningMonitor), nameof(DrowningMonitor.IsCellSafe))]
        private static class DrowningMonitor_IsCellSafe
        {
            private static bool Prefix(DrowningMonitor __instance, ref bool __result)
            {
                if (__instance.TryGetComponent(out Navigator navigator) && navigator.CurrentNavType == NavType.Tube)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SubmergedMonitor.Instance), nameof(SubmergedMonitor.Instance.IsSubmerged))]
        private static class SubmergedMonitor_Instance_IsSubmerged
        {
            private static void Postfix(SubmergedMonitor.Instance __instance, ref bool __result)
            {
                __result = __result && __instance.GetComponent<Navigator>().CurrentNavType != NavType.Tube;
            }
        }

        // предотвратим несанкционированное изменение CurrentNavType со стороны SubmergedMonitor если внутри трубы
        // а также, если внутри трубы и не движемся - проверим не застряли ли (изза деконструкции труб например)
        [HarmonyPatch]
        private static class SubmergedMonitor_InitializeStates
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                var list = new List<MethodBase>();
                foreach (var type in typeof(SubmergedMonitor).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (type.IsDefined(typeof(CompilerGeneratedAttribute)))
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                        {
                            if (method.ReturnType == typeof(void) && method.GetParameters().Length > 0
                                && method.GetParameters()[0].ParameterType == typeof(SubmergedMonitor.Instance))
                            {
                                list.Add(method);
                            }
                        }
                    }
                }
                return list;
            }

            private static void SetCurrentNavType(Navigator navigator, NavType nav_type)
            {
                if (navigator.CurrentNavType != NavType.Tube || (!navigator.IsMoving() && TestStuck(navigator)))
                    navigator.SetCurrentNavType(nav_type);
            }

            private static bool TestStuck(Navigator navigator)
            {
                int idx = navigator.cachedCell * navigator.NavGrid.maxLinksPerCell;
                var link = navigator.NavGrid.Links[idx];
                while (link.link != PathFinder.InvalidHandle)
                {
                    if (link.startNavType == navigator.CurrentNavType)
                        return false;
                    idx++;
                    link = navigator.NavGrid.Links[idx];
                }
                return true;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, ReplaceMethod);
            }

            private static bool ReplaceMethod(ref List<CodeInstruction> instructions)
            {
                var set_nav_type = typeof(Navigator).GetMethodSafe(nameof(Navigator.SetCurrentNavType), false, typeof(NavType));
                var patch = typeof(SubmergedMonitor_InitializeStates).GetMethodSafe(nameof(SetCurrentNavType), true, PPatchTools.AnyArguments);
                if (set_nav_type != null && patch != null)
                {
                    instructions = PPatchTools.ReplaceMethodCallSafe(instructions, set_nav_type, patch).ToList();
                    return true;
                }
                else
                    return false;
            }
        }
    }
}
