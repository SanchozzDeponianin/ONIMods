using System;
using System.Reflection.Emit;
using HarmonyLib;

namespace SanchozzONIMods.Lib
{
    public static class TranspilerUtils
    {
        public static CodeInstruction GetMatchingLoadInstruction(CodeInstruction code)
        {
            var opcode = code.opcode;
            if (opcode == OpCodes.Stloc) return new CodeInstruction(OpCodes.Ldloc, code.operand);
            else if (opcode == OpCodes.Stloc_S) return new CodeInstruction(OpCodes.Ldloc_S, code.operand);
            else if (opcode == OpCodes.Stloc_0) return new CodeInstruction(OpCodes.Ldloc_0);
            else if (opcode == OpCodes.Stloc_1) return new CodeInstruction(OpCodes.Ldloc_1);
            else if (opcode == OpCodes.Stloc_2) return new CodeInstruction(OpCodes.Ldloc_2);
            else if (opcode == OpCodes.Stloc_3) return new CodeInstruction(OpCodes.Ldloc_3);
            else throw new ArgumentException("Instruction is not a store", "code");
        }

        public static CodeInstruction GetMatchingStoreInstruction(CodeInstruction code)
        {
            var opcode = code.opcode;
            if (opcode == OpCodes.Ldloc) return new CodeInstruction(OpCodes.Stloc, code.operand);
            else if (opcode == OpCodes.Ldloc_S) return new CodeInstruction(OpCodes.Stloc_S, code.operand);
            else if (opcode == OpCodes.Ldloc_0) return new CodeInstruction(OpCodes.Stloc_0);
            else if (opcode == OpCodes.Ldloc_1) return new CodeInstruction(OpCodes.Stloc_1);
            else if (opcode == OpCodes.Ldloc_2) return new CodeInstruction(OpCodes.Stloc_2);
            else if (opcode == OpCodes.Ldloc_3) return new CodeInstruction(OpCodes.Stloc_3);
            else throw new ArgumentException("Instruction is not a load", "code");
        }

        // пришлось скопипастить
        public static CodeInstruction GetLoadLocalInstruction(int index, bool useAddress = false)
        {
            if (useAddress)
            {
                if (index < 256) return new CodeInstruction(OpCodes.Ldloca_S, Convert.ToByte(index));
                else return new CodeInstruction(OpCodes.Ldloca, index);
            }
            else
            {
                if (index == 0) return new CodeInstruction(OpCodes.Ldloc_0);
                else if (index == 1) return new CodeInstruction(OpCodes.Ldloc_1);
                else if (index == 2) return new CodeInstruction(OpCodes.Ldloc_2);
                else if (index == 3) return new CodeInstruction(OpCodes.Ldloc_3);
                else if (index < 256) return new CodeInstruction(OpCodes.Ldloc_S, Convert.ToByte(index));
                else return new CodeInstruction(OpCodes.Ldloc, index);
            }
        }

        public static CodeInstruction GetStoreLocalInstruction(int index)
        {
            if (index == 0) return new CodeInstruction(OpCodes.Stloc_0);
            else if (index == 1) return new CodeInstruction(OpCodes.Stloc_1);
            else if (index == 2) return new CodeInstruction(OpCodes.Stloc_2);
            else if (index == 3) return new CodeInstruction(OpCodes.Stloc_3);
            else if (index < 256) return new CodeInstruction(OpCodes.Stloc_S, Convert.ToByte(index));
            else return new CodeInstruction(OpCodes.Stloc, index);
        }

        public static CodeInstruction GetLoadArgInstruction(int index, bool useAddress = false)
        {
            if (useAddress)
            {
                if (index < 256) return new CodeInstruction(OpCodes.Ldarga_S, Convert.ToByte(index));
                else return new CodeInstruction(OpCodes.Ldarga, index);
            }
            else
            {
                if (index == 0) return new CodeInstruction(OpCodes.Ldarg_0);
                else if (index == 1) return new CodeInstruction(OpCodes.Ldarg_1);
                else if (index == 2) return new CodeInstruction(OpCodes.Ldarg_2);
                else if (index == 3) return new CodeInstruction(OpCodes.Ldarg_3);
                else if (index < 256) return new CodeInstruction(OpCodes.Ldarg_S, Convert.ToByte(index));
                else return new CodeInstruction(OpCodes.Ldarg, index);
            }
        }

        public static CodeInstruction GetStoreArgInstruction(int index)
        {
            if (index < 256) return new CodeInstruction(OpCodes.Starg_S, Convert.ToByte(index));
            else return new CodeInstruction(OpCodes.Starg, index);
        }

        public static int GetLocalIndex(this CodeInstruction code)
        {
            if (code.opcode == OpCodes.Ldloc_0 || code.opcode == OpCodes.Stloc_0) return 0;
            else if (code.opcode == OpCodes.Ldloc_1 || code.opcode == OpCodes.Stloc_1) return 1;
            else if (code.opcode == OpCodes.Ldloc_2 || code.opcode == OpCodes.Stloc_2) return 2;
            else if (code.opcode == OpCodes.Ldloc_3 || code.opcode == OpCodes.Stloc_3) return 3;
            else if (code.opcode == OpCodes.Ldloc_S || code.opcode == OpCodes.Ldloc) return Convert.ToInt32(code.operand);
            else if (code.opcode == OpCodes.Stloc_S || code.opcode == OpCodes.Stloc) return Convert.ToInt32(code.operand);
            else if (code.opcode == OpCodes.Ldloca_S || code.opcode == OpCodes.Ldloca) return Convert.ToInt32(code.operand);
            else throw new ArgumentException("Instruction is not a load or store", "code");
        }

        public static int GetArgIndex(this CodeInstruction code)
        {
            if (code.opcode == OpCodes.Ldarg_0) return 0;
            else if (code.opcode == OpCodes.Ldarg_1) return 1;
            else if (code.opcode == OpCodes.Ldarg_2) return 2;
            else if (code.opcode == OpCodes.Ldarg_3) return 3;
            else if (code.opcode == OpCodes.Ldarg_S || code.opcode == OpCodes.Ldarg) return Convert.ToInt32(code.operand);
            else if (code.opcode == OpCodes.Starg_S || code.opcode == OpCodes.Starg) return Convert.ToInt32(code.operand);
            else if (code.opcode == OpCodes.Ldarga_S || code.opcode == OpCodes.Ldarga) return Convert.ToInt32(code.operand);
            else throw new ArgumentException("Instruction is not a load or store", "code");
        }
    }
}