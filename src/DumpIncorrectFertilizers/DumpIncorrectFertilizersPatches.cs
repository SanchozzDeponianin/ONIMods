using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace DumpIncorrectFertilizers
{
    internal sealed class DumpIncorrectFertilizersPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
        }

        private static Chore CreateDumpChore(GameObject storage)
        {
            var workable = storage.AddOrGet<DumpIncorrectFertilizersWorkable>();
            return new WorkChore<DumpIncorrectFertilizersWorkable>(Db.Get().ChoreTypes.EmptyStorage, workable, only_when_operational: false);
        }

        [HarmonyPatch(typeof(FertilizationMonitor), nameof(FertilizationMonitor.InitializeStates))]
        private static class FertilizationMonitor_InitializeStates
        {
            private static void Postfix(FertilizationMonitor __instance)
            {
                __instance.replanted.starved.wrongFert
                    .ToggleStatusItem(Db.Get().BuildingStatusItems.AwaitingEmptyBuilding)
                    .ToggleRecurringChore(smi => CreateDumpChore(smi.sm.fertilizerStorage.Get(smi)), smi => smi.sm.fertilizerStorage.Get(smi) != null);
            }
        }

        [HarmonyPatch(typeof(IrrigationMonitor), nameof(IrrigationMonitor.InitializeStates))]
        private static class IrrigationMonitor_InitializeStates
        {
            private static void Postfix(IrrigationMonitor __instance)
            {
                __instance.replanted.starved.wrongLiquid
                    .ToggleStatusItem(Db.Get().BuildingStatusItems.AwaitingEmptyBuilding)
                    .ToggleRecurringChore(smi => CreateDumpChore(smi.sm.resourceStorage.Get(smi)), smi => smi.sm.resourceStorage.Get(smi) != null);
            }
        }

        // чтобы вытащенное выпадало со стороны работничка
        [HarmonyPatch(typeof(IrrigationMonitor.Instance), nameof(IrrigationMonitor.Instance.DumpIncorrectFertilizers))]
        [HarmonyPatch(new Type[] { typeof(Storage), typeof(PlantElementAbsorber.ConsumeInfo[]), typeof(bool) })]
        private static class IrrigationMonitor_Instance_DumpIncorrectFertilizers
        {
            /*
                storage.Drop(gameObject, true)
            +++ .Trigger((int)GameHashes.WorkableEntombOffset, workable.worker);
            */
            private static GameObject TriggerOffset(GameObject go, Storage storage)
            {
                if (go != null && storage != null && storage.TryGetComponent(out DumpIncorrectFertilizersWorkable workable) && workable.worker != null)
                    go.Trigger((int)GameHashes.WorkableEntombOffset, workable.worker);
                return go;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Transpile(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions, MethodBase original)
            {
                var drop = typeof(Storage).GetMethod(nameof(Storage.Drop), new Type[] { typeof(GameObject), typeof(bool) });
                var storage = original.GetParameters().FirstOrDefault(p => p.ParameterType == typeof(Storage));
                var trigger = typeof(IrrigationMonitor_Instance_DumpIncorrectFertilizers)
                    .GetMethod(nameof(TriggerOffset), BindingFlags.NonPublic | BindingFlags.Static);
                if (drop != null && storage != null && trigger != null)
                {
                    int i = instructions.FindIndex(inst => inst.Calls(drop));
                    if (i != -1)
                    {
                        instructions.Insert(++i, TranspilerUtils.GetLoadArgInstruction(storage));
                        instructions.Insert(++i, new CodeInstruction(OpCodes.Call, trigger));
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
