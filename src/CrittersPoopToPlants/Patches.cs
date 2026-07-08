using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;

namespace CrittersPoopToPlants
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            base.OnLoad(harmony);
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            var PoopedToCritters = new Dictionary<Tag, HashSet<Tag>>();
            var diets = DietManager.CollectDiets(null);
            foreach (var diet in diets)
            {
                foreach (var produced in diet.Value.producedTags)
                {
                    if (!PoopedToCritters.ContainsKey(produced.Key))
                        PoopedToCritters[produced.Key] = new HashSet<Tag>();
                    if (!PoopedToCritters[produced.Key].Contains(diet.Key))
                        PoopedToCritters[produced.Key].Add(diet.Key);
                }
            }

            var critters = new HashSet<Tag>();
            foreach (var plant in Assets.GetPrefabsWithTag(GameTags.Plant))
            {
                if (plant.TryGetComponent<IPoopStation>(out _) || plant.IsPrefabID(OxyCoralConfig.ID))
                    continue;

                var fertilization = plant.GetDef<FertilizationMonitor.Def>();
                if (fertilization != null && fertilization.consumedElements != null)
                {
                    foreach (var consumed in fertilization.consumedElements)
                    {
                        if (PoopedToCritters.TryGetValue(consumed.tag, out var poopers))
                            critters.UnionWith(poopers);
                    }
                    if (critters.Count > 0)
                    {
                        float capacity = 0f;
                        foreach (var md in plant.GetComponents<ManualDeliveryKG>())
                        {
                            foreach (var consumed in fertilization.consumedElements)
                            {
                                if (md.requestedItemTag == consumed.tag)
                                    capacity += md.Capacity;
                            }
                        }
                        var station = plant.AddOrGet<PlantPoopStation>();
                        station.capacity = capacity;
                        station.allowedUsersIds = critters.ToArray();
                    }
                    critters.Clear();
                }
            }

            var def = Assets.GetBuildingDef(GeneratorConfig.ID);
            if (def != null && def.BuildingComplete != null && def.BuildingComplete.TryGetComponent(out ManualDeliveryKG mdkg)
                && PoopedToCritters.ContainsKey(GameTags.Carbon)) // клей лядь ну зачем пихать тэг Coal ?
            {
                var station = def.BuildingComplete.AddOrGet<BuildingPoopStation>();
                station.capacity = mdkg.Capacity;
                station.allowedUsersIds = PoopedToCritters[GameTags.Carbon].ToArray();
            }

            def = Assets.GetBuildingDef(PeatGeneratorConfig.ID);
            if (def != null && def.BuildingComplete != null && def.BuildingComplete.TryGetComponent(out mdkg)
                && PoopedToCritters.ContainsKey(mdkg.RequestedItemTag))
            {
                var station = def.BuildingComplete.AddOrGet<BuildingPoopStation>();
                station.capacity = mdkg.Capacity;
                station.allowedUsersIds = PoopedToCritters[mdkg.RequestedItemTag].ToArray();
            }
        }

        // не терять материалы при убобрении диких растениев
        [HarmonyPatch(typeof(CreatureCalorieMonitor.Stomach), nameof(CreatureCalorieMonitor.Stomach.PoopInStorage))]
        private static class CreatureCalorieMonitor_Stomach_PoopInStorage
        {
            private static void Prefix(ref bool skipPoopSpawn)
            {
                skipPoopSpawn = false;
            }
        }

        // достижимость в горшках и на потолке
        // уточняем GetNavigationCost
        [HarmonyPatch(typeof(PoopStates.Instance), nameof(PoopStates.Instance.FindPoopStation))]
        private static class PoopStates_Instance_FindPoopStation
        {
            private static int GetNavigationCost(Navigator navigator, IPoopStation station)
            {
                var approachable = station as IApproachable;
                if (!approachable.IsNullOrDestroyed())
                    return navigator.GetNavigationCost(approachable);
                else
                    return navigator.GetNavigationCost(Grid.PosToCell(station.GetPoopStationObject()));
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var test = typeof(IPoopStation).GetMethodSafe(nameof(IPoopStation.IsPoopStationOperational), false);
                var victim = typeof(Navigator).GetMethodSafe(nameof(Navigator.GetNavigationCost), false, typeof(int));
                var replace = typeof(PoopStates_Instance_FindPoopStation)
                    .GetMethodSafe(nameof(GetNavigationCost), true, typeof(Navigator), typeof(IPoopStation));

                if (test == null || victim == null || replace == null)
                    return false;
                int i = instructions.FindIndex(instr => instr.Calls(test));
                if (i == -1 || !instructions[i - 1].IsLdloc())
                    return false;
                var ipoop = instructions[i - 1];
                i = instructions.FindIndex(instr => instr.Calls(victim));
                if (i == -1 || !instructions[i - 1].IsLdloc())
                    return false;
                instructions[i - 1].opcode = ipoop.opcode;
                instructions[i - 1].operand = ipoop.operand;
                instructions[i].operand = replace;
                return true;
            }
        }

        // уточняем MoveTo
        [HarmonyPatch(typeof(PoopStates), nameof(PoopStates.InitializeStates))]
        private static class PoopStates_InitializeStates
        {
            private static PoopStates.State MoveTo(PoopStates.State @this, Func<PoopStates.Instance, int> cell_callback,
                PoopStates.State success_state, PoopStates.State fail_state, bool update_cell)
            {
                return @this.MoveTo(cell_callback, CellOffsetsCallback, success_state, fail_state, update_cell);
            }

            private static CellOffset[] CellOffsetsCallback(PoopStates.Instance smi)
            {
                var approachable = smi.PoopStation as IApproachable;
                if (!approachable.IsNullOrDestroyed())
                    return approachable.GetOffsets();
                return null;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var replace = typeof(PoopStates_InitializeStates).GetMethodSafe(nameof(MoveTo), true, PPatchTools.AnyArguments);
                var victim = instructions.Find(instr => instr.opcode == OpCodes.Callvirt && instr.operand is MethodInfo method
                    && method.Name == nameof(MoveTo) && method.ReturnType == replace.ReturnType)?.operand as MethodInfo;
                if (victim == null)
                    return false;
                instructions = PPatchTools.ReplaceMethodCallSafe(instructions, victim, replace).ToList();
                return true;
            }
        }
    }
}
