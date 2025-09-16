using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
#if USESPLIB
using PeterHan.PLib.Core;
#endif

namespace SanchozzONIMods.Lib
{
    public static class TranspilerUtils
    {
        // обёртка для транспилеров, для упрощения и унификации логирования в случаях успеха и неудачи
        public delegate bool Callback_Full(List<CodeInstruction> instructions, MethodBase original, ILGenerator IL);
        public delegate bool Callback_Method(List<CodeInstruction> instructions, MethodBase original);
        public delegate bool Callback_IL(List<CodeInstruction> instructions, ILGenerator IL);
        public delegate bool Callback(List<CodeInstruction> instructions);

        private static IEnumerable<CodeInstruction> TranspileInternal(this IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL, Callback_Full transpiler, MethodBase transpiler_info)
        {
            var modified_instructions = instructions.ToList();
            for (int i = 0; i < modified_instructions.Count; i++)
                modified_instructions[i] = new CodeInstruction(modified_instructions[i]);

            var method_name = original.DeclaringType.FullName + "." + original.Name;
            transpiler_info ??= transpiler.Method;
            var transpiler_name = transpiler_info.DeclaringType.FullName + "." + transpiler_info.Name;

            if (transpiler(modified_instructions, original, IL))
            {
#if DEBUG
                var message = $"Transpiler '{transpiler_name}' injected to the method '{method_name}'";
#if USESPLIB
                PUtil.LogDebug(message);
#else
                Debug.Log(message);
#endif
#endif
                return modified_instructions;
            }
            else
            {
                var message = $"Could not apply Transpiler '{transpiler_name}' to the method '{method_name}'";
#if USESPLIB
                PUtil.LogWarning(message);
#else
                Debug.LogWarning(message);
#endif
                return instructions;
            }
        }

        public static IEnumerable<CodeInstruction> Transpile(this IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL, Callback_Full transpiler)
        {
            return TranspileInternal(instructions, original, IL, transpiler, transpiler.Method);
        }

        public static IEnumerable<CodeInstruction> Transpile(this IEnumerable<CodeInstruction> instructions, MethodBase original, Callback_Method transpiler)
        {
            return TranspileInternal(instructions, original, null, (list, method, il) => transpiler(list, method), transpiler.Method);
        }

        public static IEnumerable<CodeInstruction> Transpile(this IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL, Callback_IL transpiler)
        {
            return TranspileInternal(instructions, original, IL, (list, method, il) => transpiler(list, il), transpiler.Method);
        }

        public static IEnumerable<CodeInstruction> Transpile(this IEnumerable<CodeInstruction> instructions, MethodBase original, Callback transpiler)
        {
            return TranspileInternal(instructions, original, null, (list, method, il) => transpiler(list), transpiler.Method);
        }

        public static CodeInstruction GetMatchingLoadInstruction(this CodeInstruction code)
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

        public static CodeInstruction GetMatchingStoreInstruction(this CodeInstruction code)
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

        public static CodeInstruction GetLoadArgInstruction(this ParameterInfo arg, bool useAddress = false)
        {
            if (arg == null)
                throw new ArgumentNullException(nameof(arg));
            int index = arg.Position;
            if (arg.Member is MethodBase method && !method.IsStatic)
                index++;
            return GetLoadArgInstruction(index, useAddress);
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