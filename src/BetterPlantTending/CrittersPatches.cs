using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;

namespace BetterPlantTending
{
    using static ModAssets;
    internal static class CrittersPatches
    {
        #region Pip
        // научиваем белочек делать экстракцию декоративных безурожайных семян
        // а также доставать растений в горшках и ящиках
        [HarmonyPatch(typeof(ClimbableTreeMonitor.Instance), nameof(ClimbableTreeMonitor.Instance.FindClimbableTree))]
        [HarmonyPatch(new[] { typeof(object), typeof(ClimbableTreeMonitor.Instance.FindClimableTreeContext) },
            new[] { ArgumentType.Normal, ArgumentType.Ref })]
        private static class ClimbableTreeMonitor_Instance_FindClimbableTree
        {
            private static bool CanReachBelow(Navigator navigator, int cell, KMonoBehaviour plant)
            {
                return navigator.CanReach(cell) || (plant.HasTag(GameTags.PlantedOnFloorVessel) && navigator.CanReach(Grid.CellBelow(cell)));
            }
            private static void AddPlant(List<KMonoBehaviour> list, KMonoBehaviour plant)
            {
                if (plant != null && plant.TryGetComponent<ExtraSeedProducer>(out var producer) && producer.ExtraSeedAvailable)
                {
                    list.Add(plant);
                }
            }
            /*
            var plant = obj as KMonoBehaviour;
            ...
        ---     if (navigator.CanReach(cell))
        +++     if (CanReachBelow(navigator, cell, plant))
            ...
                var trunk = target.GetComponent<ForestTreeSeedMonitor>();
                var locker = target.GetComponent<StorageLocker>();
        +++     AddPlant(context.targets, plant);
            ...
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions, MethodBase method)
            {
                var context_type = typeof(ClimbableTreeMonitor.Instance.FindClimableTreeContext);
                var context = method.GetParameters().FirstOrDefault(p => p.ParameterType == context_type.MakeByRefType());
                var targets = context_type.GetFieldSafe(nameof(ClimbableTreeMonitor.Instance.FindClimableTreeContext.targets), false);
                var getComponent = typeof(Component).GetMethod(nameof(Component.GetComponent), Type.EmptyTypes).MakeGenericMethod(typeof(StorageLocker));
                var canReach = typeof(Navigator).GetMethodSafe(nameof(Navigator.CanReach), false, typeof(int));
                var canReachBelow = typeof(ClimbableTreeMonitor_Instance_FindClimbableTree).GetMethodSafe(nameof(CanReachBelow), true, PPatchTools.AnyArguments);
                var addPlant = typeof(ClimbableTreeMonitor_Instance_FindClimbableTree).GetMethodSafe(nameof(AddPlant), true, PPatchTools.AnyArguments);

                if (context == null || targets == null || getComponent == null || canReach == null || canReachBelow == null || addPlant == null)
                    return false;
                int i = instructions.FindIndex(inst => inst.Is(OpCodes.Isinst, typeof(KMonoBehaviour)));
                if (i == -1 || !instructions[i + 1].IsStloc())
                    return false;
                var plant = instructions[i + 1].GetMatchingLoadInstruction();

                i = instructions.FindIndex(i, inst => inst.Calls(canReach));
                if (i == -1)
                    return false;

                int j = instructions.FindIndex(i, inst => inst.Calls(getComponent));
                if (j == -1 || !instructions[j + 1].IsStloc())
                    return false;

                j += 2;
                instructions.Insert(j++, context.GetLoadArgInstruction());
                instructions.Insert(j++, new CodeInstruction(OpCodes.Ldfld, targets));
                instructions.Insert(j++, plant);
                instructions.Insert(j++, new CodeInstruction(OpCodes.Call, addPlant));

                instructions.Insert(i++, plant);
                instructions[i] = new CodeInstruction(OpCodes.Call, canReachBelow);
                return true;
            }
        }

        [HarmonyPatch(typeof(TreeClimbStates), nameof(TreeClimbStates.GetClimbableCell))]
        private static class TreeClimbStates_GetClimbableCell
        {
            private static void Postfix(TreeClimbStates.Instance smi, ref int __result)
            {
                var plant = smi.sm.target.Get(smi);
                if (plant != null && plant.HasTag(GameTags.PlantedOnFloorVessel) && smi.gameObject.TryGetComponent(out Navigator navigator))
                {
                    int below = Grid.CellBelow(__result);
                    int cost = navigator.GetNavigationCost(__result);
                    int cost_below = navigator.GetNavigationCost(below);
                    if (cost_below != PathProber.InvalidCost && (cost == PathProber.InvalidCost || cost_below < cost))
                        __result = below;
                }
            }
        }

        [HarmonyPatch(typeof(TreeClimbStates), nameof(TreeClimbStates.Rummage))]
        private static class TreeClimbStates_Rummage
        {
            private static bool Prefix(TreeClimbStates.Instance smi)
            {
                var target = smi.sm.target.Get(smi);
                if (target != null && target.TryGetComponent<ExtraSeedProducer>(out var producer))
                {
                    producer.ExtractExtraSeed();
                    return false;
                }
                return true;
            }
        }
        #endregion
        #region Divergents
        // не убобрять растение если оно засохло
        // кроме кукурузы из длц4
        private static bool IsFullyGrownOrWilting(bool is_FullyGrown, KPrefabID kPrefabID)
        {
            return is_FullyGrown || (kPrefabID.HasTag(GameTags.Wilting) && kPrefabID.PrefabTag != GardenFoodPlantConfig.ID);
        }

        // научиваем жучинкусов убобрять безурожайные растения, такие как холодых и оксихрен, и декоративочка
        // использован достаточно грязный хак. но все работает.
        [HarmonyPatch(typeof(CropTendingStates), nameof(CropTendingStates.FindCrop))]
        private static class CropTendingStates_FindCrop
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active();
            private static readonly int radius = (int)Math.Sqrt(CropTendingStates.MAX_SQR_EUCLIDEAN_DISTANCE);

            // поиск растений для убобрения.
            // Клеи зачем-то перебирают список всех урожайных растений на карте
            // а мы заменим на GameScenePartitioner
            private static List<KMonoBehaviour> FindPlants(CropTendingStates.Instance smi)
            {
                int myWorldId = smi.gameObject.GetMyWorldId();
                var entries = ListPool<ScenePartitionerEntry, GameScenePartitioner>.Allocate();
                var ext = new Extents(Grid.PosToCell(smi.master.transform.GetPosition()), radius);
                GameScenePartitioner.Instance.GatherEntries(ext.x, ext.y, ext.width, ext.height, GameScenePartitioner.Instance.plants, entries);
                var plants = new List<KMonoBehaviour>();
                foreach (var entry in entries)
                {
                    var kmb = entry.obj as KMonoBehaviour;
                    if (kmb != null && (kmb.GetMyWorldId() == myWorldId)
                        && (kmb.TryGetComponent(out Crop _) || kmb.TryGetComponent(out ExtraSeedProducer _)))
                    {
                        plants.Add(kmb);
                    }
                }
                entries.Recycle();
                return plants;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var smi = typeof(CropTendingStates)
                    .GetMethodSafe(nameof(CropTendingStates.FindCrop), false, typeof(CropTendingStates.Instance))
                    ?.GetParameters()?.First(p => p.ParameterType == typeof(CropTendingStates.Instance));
                var GetWorldItems = typeof(Components.Cmps<Crop>)
                    .GetMethodSafe(nameof(Components.Cmps<Crop>.GetWorldItems), false, typeof(int), typeof(bool));
                var findPlants = typeof(CropTendingStates_FindCrop).GetMethodSafe(nameof(FindPlants), true, PPatchTools.AnyArguments);
                var getKPrefabID = typeof(Component).GetMethodSafe(nameof(Component.GetComponent), false).MakeGenericMethod(typeof(KPrefabID));
                var hasTag = typeof(KPrefabID).GetMethodSafe(nameof(KPrefabID.HasTag), false, typeof(Tag));
                var fullyGrown = typeof(GameTags).GetFieldSafe(nameof(GameTags.FullyGrown), true);
                var isWilting = typeof(CrittersPatches).GetMethodSafe(nameof(CrittersPatches.IsFullyGrownOrWilting), true, PPatchTools.AnyArguments);

                if (smi != null && GetWorldItems != null && findPlants != null && getKPrefabID != null && hasTag != null && fullyGrown != null && isWilting != null)
                {
                    /*
                    foreach (Crop crop in 
                ---         Components.Crops.GetWorldItems(smi.gameObject.GetMyWorldId(), false))
                +++         FindPlants(smi))
                    {
                        блаблабла;
                        var kPrefabID = crop.GetComponent<KPrefabID>();
                ---     if (kPrefabID.HasTag(GameTags.FullyGrown))
                +++     if (kPrefabID.HasTag(GameTags.FullyGrown) || kPrefabID.HasTag(GameTags.Wilting))
                            блаблабла;
                    }
                    */
                    int i = instructions.FindIndex(inst => inst.Calls(GetWorldItems));
                    if (i == -1)
                        return false;

                    if (ModOptions.Instance.prevent_tending_grown_or_wilting)
                    {
                        int k = instructions.FindIndex(i, inst => inst.Calls(getKPrefabID));
                        if (k == -1 || !instructions[k + 1].IsStloc())
                            return false;
                        var local_KPrefabID = (LocalBuilder)instructions[k + 1].operand;

                        int m = instructions.FindIndex(k, inst => inst.LoadsField(fullyGrown));
                        if (m == -1 || !instructions[m + 1].Calls(hasTag))
                            return false;

                        m++;
                        instructions.Insert(++m, TranspilerUtils.GetLoadLocalInstruction(local_KPrefabID.LocalIndex));
                        instructions.Insert(++m, new CodeInstruction(OpCodes.Call, isWilting));
                    }

                    for (int j = 0; j < GetWorldItems.GetParameters().Length; j++)
                        instructions.Insert(i++, new CodeInstruction(OpCodes.Pop));
                    instructions.Insert(i++, TranspilerUtils.GetLoadArgInstruction(smi));
                    instructions[i] = new CodeInstruction(OpCodes.Call, findPlants);
                    return true;
                }
                return false;
            }
        }

        // чтобы жучинкусы могли достать до растений с любой соседней клетки
        // а не только слева справа от самой нижней или верхней (для растущих вниз)
        // чтобы могли убобрить свисающие с потолка пинчи
        [HarmonyPatch(typeof(CropTendingStates), nameof(CropTendingStates.FindCrop))]
        private static class CropTendingStates_FindCrop_Mk2
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active();
            /*
                var prefabID = crop.GetComponent<KPrefabID>();
                ...
                int cell = Grid.PosToCell(crop);
                int[] cells;
            +++ if (GetOccupiedCells(prefabID, ref cells))
            +++     goto label;
                cells = new int[] { Grid.CellLeft(cell), Grid.CellRight(cell) };
                if (prefabID.HasTag(GameTags.PlantedOnFloorVessel))
                {
                    cells = new int[] { Grid.CellLeft(cell), Grid.CellRight(cell), Grid.CellDownLeft(cell), Grid.CellDownRight(cell) };
                }
            +++ label:
                int num4 = 100;
                int num5 = Grid.InvalidCell;
            */
            // массив клетки слева и справа от реально занятых растением клеток, плюс ещё две снизу если растение в горшке
            private static bool GetOccupiedCells(KPrefabID plant, ref int[] cells)
            {
                if (plant != null && plant.TryGetComponent(out OccupyArea area))
                {
                    int pos = Grid.PosToCell(plant);
                    bool vessel = plant.HasTag(GameTags.PlantedOnFloorVessel);
                    var offsets = area.OccupiedCellsOffsets;
                    int count = offsets.Length;
                    int[] array;
                    if (vessel)
                        array = new int[(count + area.GetWidthInCells()) * 2];
                    else
                        array = new int[count * 2];
                    int i = 0;
                    for (int j = 0; j < count; j++)
                    {
                        int cell = Grid.OffsetCell(pos, offsets[j]);
                        array[i++] = Grid.CellLeft(cell);
                        array[i++] = Grid.CellRight(cell);
                    }
                    if (vessel)
                    {
                        for (int k = 0; k < count; k++)
                        {
                            var offset_below = new CellOffset(offsets[k].x, offsets[k].y - 1);
                            if (Array.IndexOf(offsets, offset_below) == -1)
                            {
                                int cell = Grid.OffsetCell(pos, offset_below);
                                array[i++] = Grid.CellLeft(cell);
                                array[i++] = Grid.CellRight(cell);
                            }
                        }
                    }
                    cells = array;
                    return true;
                }
                return false;
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL)
            {
                return instructions.Transpile(original, IL, transpiler);
            }
            private static bool transpiler(ref List<CodeInstruction> instructions, ILGenerator IL)
            {
                var getKPrefabID = typeof(Component).GetMethodSafe(nameof(Component.GetComponent), false).MakeGenericMethod(typeof(KPrefabID));
                var posToCell = typeof(Grid).GetMethodSafe(nameof(Grid.PosToCell), true, typeof(KMonoBehaviour));
                var cellRight = typeof(Grid).GetMethodSafe(nameof(Grid.CellRight), true, typeof(int));
                var invalidCell = typeof(Grid).GetFieldSafe(nameof(Grid.InvalidCell), true);
                var getCells = typeof(CropTendingStates_FindCrop_Mk2).GetMethodSafe(nameof(GetOccupiedCells), true, PPatchTools.AnyArguments);

                if (getKPrefabID == null || posToCell == null || cellRight == null || invalidCell == null || getCells == null)
                    return false;

                int i = instructions.FindIndex(inst => inst.Calls(getKPrefabID));
                if (i == -1 || !instructions[i + 1].IsStloc())
                    return false;
                var local_KPrefabID = (LocalBuilder)instructions[i + 1].operand;

                int j = instructions.FindIndex(i, inst => inst.Calls(posToCell));
                if (j == -1 || !instructions[j + 1].IsStloc())
                    return false;

                int k = instructions.FindIndex(j, inst => inst.Calls(cellRight));
                if (k == -1 || !(instructions[k + 1].opcode == OpCodes.Stelem_I4) || !instructions[k + 2].IsStloc())
                    return false;
                var local_cells = (LocalBuilder)instructions[k + 2].operand;

                int m = instructions.FindIndex(k, inst => inst.LoadsField(invalidCell));
                if (m == -1 || !instructions[m + 1].IsStloc())
                    return false;

                int n = instructions.FindLastIndex(m, inst => inst.IsStloc(local_cells));
                if (n == -1)
                    return false;

                var label = IL.DefineLabel();
                instructions[n + 1].labels.Add(label);
                j++;
                instructions.Insert(++j, TranspilerUtils.GetLoadLocalInstruction(local_KPrefabID.LocalIndex));
                instructions.Insert(++j, TranspilerUtils.GetLoadLocalInstruction(local_cells.LocalIndex, true));
                instructions.Insert(++j, new CodeInstruction(OpCodes.Call, getCells));
                instructions.Insert(++j, new CodeInstruction(OpCodes.Brtrue, label));
                return true;
            }
        }
        #endregion
        #region Butterfly
        [HarmonyPatch(typeof(PollinateMonitor.Def), nameof(PollinateMonitor.Def.IsHarvestablePlant))]
        private static class PollinateMonitor_Def_IsHarvestablePlant
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC4_ID)
                && (ModOptions.Instance.prevent_tending_grown_or_wilting || ModOptions.Instance.allow_tending_together);

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                if (ModOptions.Instance.prevent_tending_grown_or_wilting)
                    instructions = instructions.Transpile(original, AddTestIsWilting);
                if (ModOptions.Instance.allow_tending_together)
                    instructions = instructions.Transpile(original, ReplaseIgnoredEffects);
                return instructions;
            }
            // мимика не опыляет засохшее
            private static bool AddTestIsWilting(ref List<CodeInstruction> instructions)
            {
                var hasTag = typeof(KPrefabID).GetMethodSafe(nameof(KPrefabID.HasTag), false, typeof(Tag));
                var fullyGrown = typeof(GameTags).GetFieldSafe(nameof(GameTags.FullyGrown), true);
                var isWilting = typeof(CrittersPatches).GetMethodSafe(nameof(CrittersPatches.IsFullyGrownOrWilting), true, PPatchTools.AnyArguments);

                if (hasTag == null || fullyGrown == null || isWilting == null)
                    return false;

                int m = instructions.FindIndex(inst => inst.LoadsField(fullyGrown));
                if (m == -1 || !instructions[m + 1].Calls(hasTag))
                    return false;

                m++;
                instructions.Insert(++m, new CodeInstruction(OpCodes.Ldarg_0));
                instructions.Insert(++m, new CodeInstruction(OpCodes.Call, isWilting));
                return true;
            }

            // мимика может убобрять растения совместно c жучинкусами
            private static readonly HashedString[] ignoreMimikaEffects = new HashedString[] { BUTTERFLY_CROP_TENDED_EFFECT_ID };
            private static bool ReplaseIgnoredEffects(ref List<CodeInstruction> instructions)
            {
                var all = typeof(PollinationMonitor).GetFieldSafe(nameof(PollinationMonitor.PollinationEffects), true);
                var mimika = typeof(PollinateMonitor_Def_IsHarvestablePlant).GetFieldSafe(nameof(ignoreMimikaEffects), true);

                if (all != null && mimika != null && all.FieldType == mimika.FieldType)
                {
                    int n = 0;
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].LoadsField(all))
                        {
                            var instr = new CodeInstruction(OpCodes.Ldsfld, mimika);
                            instr.labels.AddRange(instructions[i].labels);
                            instructions[i] = instr;
                            n++;
                        }
                    }
                    return n > 0;
                }
                return false;
            }
        }
        #endregion
        #region Topotun
        // научиваем топотуна делать экстракцию безурожайных семян
        [HarmonyPatch(typeof(StompStates.Instance), nameof(StompStates.Instance.HarvestAnyOneIntersectingPlant))]
        private static class StompStates_Instance_HarvestAnyOneIntersectingPlant
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC4_ID);

            private static GameObject Extract(GameObject go)
            {
                if (go != null && go.TryGetComponent(out ExtraSeedProducer producer))
                    producer.ExtractExtraSeed();
                return go;
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var getComponent = typeof(GameObject).GetMethod(nameof(Component.GetComponent), Type.EmptyTypes).MakeGenericMethod(typeof(Harvestable));
                var extract = typeof(StompStates_Instance_HarvestAnyOneIntersectingPlant).GetMethodSafe(nameof(Extract), true, typeof(GameObject));

                if (getComponent != null && extract != null)
                {
                    int i = instructions.FindIndex(inst => inst.Calls(getComponent));
                    if (i == -1)
                        return false;

                    instructions.Insert(i++, new CodeInstruction(OpCodes.Call, extract));
                    return true;
                }
                return false;
            }
        }
        #endregion
    }
}
