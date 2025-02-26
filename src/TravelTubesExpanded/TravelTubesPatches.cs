using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace TravelTubesExpanded
{
    internal sealed class TravelTubesPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(TravelTubesPatches));
            new POptions().RegisterOptions(this, typeof(TravelTubesOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
            LocString.CreateLocStringKeys(typeof(STRINGS.BUILDINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            var ids = new string[] { TravelTubeInsulatedWallBridgeConfig.ID, TravelTubeBunkerWallBridgeConfig.ID,
                TravelTubeLadderBridgeConfig.ID, TravelTubeFirePoleBridgeConfig.ID };
            for (int i = ids.Length - 1; i >= 0; i--)
                ModUtil.AddBuildingToPlanScreen(BUILD_CATEGORY.Base, ids[i], BUILD_SUBCATEGORY.transport, TravelTubeWallBridgeConfig.ID);
            Utils.AddBuildingToTechnology("TravelTubes", ids);
        }

#if false
        // нехрен шляццо возле пускача
        [HarmonyPatch(typeof(TravelTubeEntranceConfig), nameof(TravelTubeEntranceConfig.CreateBuildingDef))]
        private static class TravelTubeEntranceConfig_CreateBuildingDef
        {
            private static void Postfix(BuildingDef __result)
            {
                __result.PreventIdleTraversalPastBuilding = true;
            }
        }
#endif

        [HarmonyPatch(typeof(TravelTubeEntranceConfig), nameof(TravelTubeEntranceConfig.DoPostConfigureComplete))]
        private static class TravelTubeEntranceConfig_ConfigureBuildingTemplate
        {
            private static void Postfix(GameObject go)
            {
                go.AddOrGet<TravelTubeEntrance>().joulesPerLaunch = TravelTubesOptions.Instance.kjoules_per_launch * Constants.KW2W;
                go.AddOrGet<EntranceFakeTubes>();
            }
        }

        // возможность использовать мост над пускачом как вход в трубы
        [HarmonyPatch(typeof(TravelTubeEntrance), nameof(TravelTubeEntrance.TubeConnectionsChanged))]
        private static class TravelTubeEntrance_TubeConnectionsChanged
        {
            private static void Prefix(TravelTubeEntrance __instance, ref object data)
            {
                if ((UtilityConnections)data == 0)
                {
                    int bridge_cell = Grid.OffsetCell(Grid.PosToCell(__instance), 0, 2);
                    var go = Grid.Objects[bridge_cell, (int)ObjectLayer.FoundationTile];
                    if (go != null && go.TryGetComponent(out TravelTubeUtilityNetworkLink link))
                    {
                        link.GetCells(out int a, out int b);
                        if (UtilityConnectionsExtensions.DirectionFromToCell(bridge_cell, a) == UtilityConnections.Up
                            || UtilityConnectionsExtensions.DirectionFromToCell(bridge_cell, b) == UtilityConnections.Up)
                            data = UtilityConnections.Up;
                    }
                }
            }
        }

#if false
        // нафигация дуплей через трубы
        // todo: а нужно ли ?
        [HarmonyPatch]
        private static class NavGrid_Constructor
        {
            private static MethodBase TargetMethod()
            {
                return typeof(NavGrid).GetConstructors()[0];
            }

            private static void Prefix(string id, ref NavGrid.Transition[] transitions)
            {
                if (id == "MinionNavGrid")
                {
                    // поправим кост диагональных транзицый, а то он странный
                    for (int i = 0; i < transitions.Length; i++)
                    {
                        var t = transitions[i];
                        if (t.start == NavType.Tube && t.end == NavType.Tube && t.x != 0 && t.y != 0 && t.cost == 10)
                        {
                            t.cost = 7;
                            transitions[i] = t;
                        }
                    }
                }
            }
        }
#endif

        // правим проверки нафигационных транзицый
        [HarmonyPatch(typeof(NavGrid.Transition), nameof(NavGrid.Transition.IsValid))]
        private static class NavGrid_Transition_IsValid
        {
            private static ParameterInfo cell;
            private static MethodInfo get_connections;

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL)
            {
                cell = original.GetParameters().FirstOrDefault(p => p.ParameterType == typeof(int) && p.Name == "cell");
                get_connections = typeof(UtilityNetworkManager<TravelTubeNetwork, TravelTube>)
                    .GetMethod(nameof(UtilityNetworkTubesManager.GetConnections));

                // эти слои ObjectLayer.TravelTube ObjectLayer.TravelTubeConnection почему то не используются игрой
                // ну что добавим оптимизацию не дергать фундаменты ?
                // бляяя в плиб тоже есть бага
                /*
                instructions = PPatchTools.ReplaceConstant(instructions,
                    (int)ObjectLayer.FoundationTile, (int)ObjectLayer.TravelTubeConnection, true)
                */
                instructions = instructions
                    .Wrap(original, IL, AddJumpEntranceAndDualBridgeTest)
                    .Wrap(original, IL, AddTubeAndBridgeExitTest)
                    .Wrap(original, IL, AddFloorToTubeTest);
                return instructions;
            }

            // возможность проскочить через пускачь
            // возможность пройти через два моста прислонённых другкдругу
            /*
                if (this.start == NavType.Tube)
                {
                    if (this.end == NavType.Tube)
                    {
                        GameObject go1 = Grid.Objects[cell, 9];
                        GameObject go2 = Grid.Objects[to_cell, 9];
                        var link1 = (go1) ? go1.GetComponent<TravelTubeUtilityNetworkLink>() : null;
                        var link2 = (go2) ? go2.GetComponent<TravelTubeUtilityNetworkLink>() : null;
            +++         if (TestDualBridge(this, cell, ref to_cell, go1, go2, link1, link2)
            +++             return to_cell;
                        if (link1)
                        {
            */
            private static bool AddJumpEntranceAndDualBridgeTest(List<CodeInstruction> instructions, ILGenerator IL)
            {
                var get_link = typeof(GameObject).GetMethodSafe(nameof(GameObject.GetComponent), false)
                    ?.MakeGenericMethod(typeof(TravelTubeUtilityNetworkLink));
                var test = typeof(NavGrid_Transition_IsValid).GetMethodSafe(nameof(TestJumpEntranceAndDualBridge), true, PPatchTools.AnyArguments);
                if (cell != null && get_link != null && test != null)
                {
                    Predicate<CodeInstruction> match = inst => inst.Calls(get_link);
                    int i = instructions.FindIndex(match);
                    if (i != -1 && instructions[i - 1].IsLdloc() && instructions[i + 1].IsStloc())
                    {
                        var go1 = instructions[i - 1];
                        var link1 = instructions[i + 1];
                        i++;
                        i = instructions.FindIndex(i, match);
                        if (i != -1 && instructions[i - 1].IsLdloc() && instructions[i + 1].IsStloc())
                        {
                            var go2 = instructions[i - 1];
                            var link2 = instructions[i + 1];
                            i += 2;
                            var to_cell = IL.DeclareLocal(typeof(int));
                            var @else = IL.DefineLabel();
                            instructions.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
                            instructions.Insert(i++, TranspilerUtils.GetLoadArgInstruction(cell));
                            instructions.Insert(i++, TranspilerUtils.GetLoadLocalInstruction(to_cell.LocalIndex, true));
                            instructions.Insert(i++, go1.Clone());
                            instructions.Insert(i++, go2.Clone());
                            instructions.Insert(i++, TranspilerUtils.GetMatchingLoadInstruction(link1));
                            instructions.Insert(i++, TranspilerUtils.GetMatchingLoadInstruction(link2));
                            instructions.Insert(i++, new CodeInstruction(OpCodes.Call, test));
                            instructions.Insert(i++, new CodeInstruction(OpCodes.Brfalse_S, @else));
                            instructions.Insert(i++, TranspilerUtils.GetLoadLocalInstruction(to_cell.LocalIndex));
                            instructions.Insert(i++, new CodeInstruction(OpCodes.Ret));
                            instructions.Insert(i, new CodeInstruction(OpCodes.Nop));
                            instructions[i].labels.Add(@else);
                            return true;
                        }
                    }
                }
                return false;
            }

            private static bool HasEntrance(GameObject go)
            {
                return go != null && Grid.HasTubeEntrance[Grid.PosToCell(go)];
            }

            private static bool TestJumpEntranceAndDualBridge(ref NavGrid.Transition transition, int from, ref int to,
                GameObject go_from, GameObject go_to,
                TravelTubeUtilityNetworkLink link_from, TravelTubeUtilityNetworkLink link_to)
            {
                to = Grid.OffsetCell(from, transition.x, transition.y);
                int a, b;
                // сначала пускачь, проверяем только вертикаль
                if (transition.x == 0)
                {
                    if (HasEntrance(go_from))
                    {
                        if (HasEntrance(go_to)) // проход внутре пускача
                            return true;
                        else if (link_to != null) // выход из пускача
                        {
                            link_to.GetCells(out a, out b);
                            if (from == a || from == b)
                                return true;
                        }
                        else
                        {
                            if (UtilityConnectionsExtensions.DirectionFromToCell(from, to)
                                == Game.Instance.travelTubeSystem.GetConnections(to, false))
                            {
                                return true;
                            }
                        }
                    }
                    else if (HasEntrance(go_to)) // вход внутьрь пускача
                    {
                        if (link_from != null)
                        {
                            link_from.GetCells(out a, out b);
                            if (to == a || to == b)
                                return true;
                        }
                        else
                        {
                            if (UtilityConnectionsExtensions.DirectionFromToCell(to, from)
                                == Game.Instance.travelTubeSystem.GetConnections(from, false))
                            {
                                return true;
                            }
                        }
                    }
                }
                // теперь пара мостов
                if (link_from == null || link_to == null)
                    return false;
                link_from.GetCells(out a, out b);
                if (to != a && to != b)
                {
                    to = Grid.InvalidCell;
                    return true;
                }
                link_to.GetCells(out a, out b);
                if (from != a && from != b)
                {
                    to = Grid.InvalidCell;
                    return true;
                }
                if (UtilityConnectionsExtensions.DirectionFromToCell(from, to) == 0)
                    to = Grid.InvalidCell;
                return true;
            }

            // для трубы с одним направлением проверим отсутствие моста с другой стороны
            // для избежания неправомерного выхода через лестничный мост
            // возможность использовать полуподключенный мост как выход из труб
            /*
                if (this.start == NavType.Tube)
                {
                    if (this.end == NavType.Tube)
                    {
                        GameObject go1 = Grid.Objects[cell, 9];
                        GameObject go2 = Grid.Objects[to_cell, 9];
                        блаблабла;
                    }
                    else
                    {
                        UtilityConnections connections = Game.Instance.travelTubeSystem.GetConnections(cell, false);
            +++         TestTubeConnections(connections, cell);
            +++         TestBridgeConnections(connections, cell);
                        здесь куча проверок Left Right Up Down;
            */
            private static bool AddTubeAndBridgeExitTest(List<CodeInstruction> instructions, ILGenerator IL)
            {
                var grid_objects = typeof(Grid).GetField(nameof(Grid.Objects));
                var test1 = typeof(NavGrid_Transition_IsValid).GetMethodSafe(nameof(TestTubeConnections), true, PPatchTools.AnyArguments);
                var test2 = typeof(NavGrid_Transition_IsValid).GetMethodSafe(nameof(TestBridgeConnections), true, PPatchTools.AnyArguments);
                if (cell != null && grid_objects != null && test1 != null && test2 != null)
                {
                    int i = instructions.FindIndex(inst => inst.LoadsField(grid_objects, true));
                    if (i == -1) return false;
                    Label? @else = null;
                    i = instructions.FindLastIndex(i, inst => inst.Branches(out @else));
                    if (i == -1 || @else == null) return false;
                    i = instructions.FindIndex(i, inst => inst.labels.Contains((Label)@else));
                    if (i == -1) return false;
                    i = instructions.FindIndex(i, inst => inst.Calls(get_connections));
                    if (i == -1) return false;
                    i++;
                    instructions.Insert(i++, TranspilerUtils.GetLoadArgInstruction(cell));
                    instructions.Insert(i++, new CodeInstruction(OpCodes.Call, test1));
                    instructions.Insert(i++, TranspilerUtils.GetLoadArgInstruction(cell));
                    instructions.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
                    instructions.Insert(i++, new CodeInstruction(OpCodes.Call, test2));
                    return true;
                }
                return false;
            }

            // если кусок трубы имеет соединение только в одну сторону
            // проверим есть ли с другой стороны мост
            private static UtilityConnections TestTubeConnections(UtilityConnections connection, int tube_cell)
            {
                if (connection != UtilityConnections.Left && connection != UtilityConnections.Right
                    && connection != UtilityConnections.Up && connection != UtilityConnections.Down)
                    return connection;
                connection |= GetDirectionTubeToNeighbourBridge(tube_cell, connection.InverseDirection().CellInDirection(tube_cell));
                return connection;
            }

            private static UtilityConnections GetDirectionTubeToNeighbourBridge(int tube_cell, int neighbour_cell)
            {
                var go = Grid.Objects[neighbour_cell, (int)ObjectLayer.FoundationTile];
                if (go != null && go.TryGetComponent(out TravelTubeUtilityNetworkLink neighbour_link))
                {
                    neighbour_link.GetCells(out int a, out int b);
                    if (a == tube_cell || b == tube_cell)
                        return UtilityConnectionsExtensions.DirectionFromToCell(tube_cell, neighbour_cell);
                }
                return 0;
            }

            // в норме для мостов GetConnections == 0
            // проверим есть ли тут мост и соединён ли он с трубами и соседними мостами
            private static UtilityConnections TestBridgeConnections(UtilityConnections connection, int bridge_cell, ref NavGrid.Transition transition)
            {
                if (connection != 0)
                    return connection;
                var go = Grid.Objects[bridge_cell, (int)ObjectLayer.FoundationTile];
                if (go != null && go.TryGetComponent(out TravelTubeUtilityNetworkLink link))
                {
                    link.GetCells(out int a, out int b);
                    connection |= GetDirectionBridgeToNeighbour(bridge_cell, a);
                    connection |= GetDirectionBridgeToNeighbour(bridge_cell, b);
                    // граничный случай, одиночный мост над пускачом
                    if (connection == 0)
                    {
                        int below_cell = Grid.CellBelow(bridge_cell);
                        if (Grid.IsValidCell(below_cell) && (below_cell == a || below_cell == b)
                            && HasEntrance(Grid.Objects[below_cell, (int)ObjectLayer.FoundationTile]))
                        {
                            if (transition.end == NavType.Tube)
                                connection = UtilityConnections.Up;
                            else if (transition.y > 0)
                                connection = UtilityConnections.Down;
                            else if (transition.y < 0)
                                connection = UtilityConnections.Up;
                        }
                    }
                }
                return connection;
            }

            private static UtilityConnections GetDirectionBridgeToNeighbour(int bridge_cell, int neighbour_cell)
            {
                var mustbe = UtilityConnectionsExtensions.DirectionFromToCell(bridge_cell, neighbour_cell);
                var neighbour = Game.Instance.travelTubeSystem.GetConnections(neighbour_cell, false);
                if (mustbe == neighbour)
                    return neighbour;
                else if (neighbour == 0)
                    return GetDirectionTubeToNeighbourBridge(bridge_cell, neighbour_cell);
                else
                    return 0;
            }

            // возможность использовать полуподключенный мост над пускачом как вход в трубы
            /*
            else if (this.start == NavType.Floor && this.end == NavType.Tube)
            {
                int cell5 = Grid.OffsetCell(cell, this.x, this.y);              |
                                                                                V
                if (Game.Instance.travelTubeSystem.GetConnections(cell5, false) != UtilityConnections.Up)
                    return Grid.InvalidCell;
            }
            */
            private static bool AddFloorToTubeTest(List<CodeInstruction> instructions, ILGenerator IL)
            {
                var end = typeof(NavGrid.Transition).GetField(nameof(NavGrid.Transition.end));
                var test = typeof(NavGrid_Transition_IsValid).GetMethodSafe(nameof(TestBridgeConnections), true, PPatchTools.AnyArguments);
                if (end != null && get_connections != null && test != null)
                {
                    int i = instructions.FindLastIndex(inst => inst.LoadsField(end));
                    if (i != -1 && instructions[i + 1].LoadsConstant(NavType.Tube))
                    {
                        i = instructions.FindIndex(i, inst => inst.Calls(get_connections));
                        if (i != -1 && instructions[i - 2].IsLdloc())
                        {
                            var to_cell = instructions[i - 2];
                            i++;
                            instructions.Insert(i++, to_cell.Clone());
                            instructions.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
                            instructions.Insert(i++, new CodeInstruction(OpCodes.Call, test));
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
