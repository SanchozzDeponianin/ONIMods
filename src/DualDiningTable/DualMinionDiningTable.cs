using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;

namespace DualDiningTable
{
    [HarmonyPatch]
    public class DualMinionDiningTable : MultiMinionDiningTable
    {
        public new const string SEAT_ID = "DualMinionDiningSeat";

        [HarmonyReversePatch(HarmonyReversePatchType.Original)]
        [HarmonyPatch(typeof(MultiMinionDiningTable), nameof(SpawnSeat))]
        private new static GameObject SpawnSeat(MultiMinionDiningTable diningTable, int diningTableCell, int seatIndex)
        {
#pragma warning disable CS8321
            // заменяем гвоздями прибитый статичный seats и имя гамэобъекта
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) =>
                instructions.Transpile(original, TrueSeat);
#pragma warning restore CS8321
            return null;
        }

        private static bool TrueSeat(ref List<CodeInstruction> instructions)
        {
            var nailed = typeof(MultiMinionDiningTableConfig).GetField(nameof(MultiMinionDiningTableConfig.seats));
            var my = typeof(DualMinionDiningTableConfig).GetField(nameof(DualMinionDiningTableConfig.seats));
            if (nailed == null || my == null)
                return false;
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].LoadsField(nailed))
                    instructions[i].operand = my;
                else if (instructions[i].LoadsConstant(MultiMinionDiningTable.SEAT_ID))
                    instructions[i].operand = DualMinionDiningTable.SEAT_ID;
            }
            return true;
        }

        [HarmonyReversePatch(HarmonyReversePatchType.Original)]
        [HarmonyPatch(typeof(MultiMinionDiningTable), nameof(OnSpawn))]
        public override void OnSpawn()
        {
#pragma warning disable CS8321
            // заменяем вызов на патченный
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) =>
                instructions.Transpile(original, PatchedSpawnSeat);
#pragma warning restore CS8321
            base.OnSpawn();
        }

        private static bool PatchedSpawnSeat(ref List<CodeInstruction> instructions)
        {
            var nailed = typeof(MultiMinionDiningTable).GetMethodSafe(nameof(SpawnSeat), true, PPatchTools.AnyArguments);
            var my = typeof(DualMinionDiningTable).GetMethodSafe(nameof(SpawnSeat), true, PPatchTools.AnyArguments);
            var count = typeof(MultiMinionDiningTable).GetProperty(nameof(MultiMinionDiningTable.SeatCount)).GetGetMethod();
            var true_count = typeof(DualMinionDiningTableConfig).GetProperty(nameof(DualMinionDiningTableConfig.SeatCount)).GetGetMethod();
            if (nailed == null || my == null || count == null || true_count == null)
                return false;
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].Calls(nailed))
                    instructions[i].operand = my;
                else if (instructions[i].Calls(count))
                    instructions[i].operand = true_count;
            }
            return true;
        }
    }
}
