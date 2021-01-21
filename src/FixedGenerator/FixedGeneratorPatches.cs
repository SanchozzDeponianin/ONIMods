using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;

namespace FixedGenerator
{
    [Obsolete("Not needed any more", true)]
    internal static class FixedGeneratorPatches
    {
        // исправляем проверку наличия горючки
        [HarmonyPatch(typeof(EnergyGenerator), "IsConvertible")]
        internal static class EnergyGenerator_IsConvertible
        {
            private static bool Prefix(ref bool __result, float dt, EnergyGenerator.Formula ___formula, Storage ___storage)
            {
                bool flag = true;
                foreach (EnergyGenerator.InputItem inputItem in ___formula.inputs)
                {
                    float mass = inputItem.consumptionRate * dt;
                    PrimaryElement primaryElement = ___storage.FindFirstWithMass(inputItem.tag, mass);
                    if (primaryElement != null)
                    {
                        flag = (flag && primaryElement.Mass >= mass);
                    }
                    else
                    {
                        flag = false;
                    }
                    if (!flag)
                    {
                        break;
                    }
                }
                __result = flag;
                return false;
            }
        }

        // исправляем отображение метера
        [HarmonyPatch(typeof(EnergyGenerator), nameof(EnergyGenerator.EnergySim200ms))]
        internal static class EnergyGenerator_EnergySim200ms
        {
            private static void Prefix(EnergyGenerator __instance, Storage ___storage, MeterController ___meter)
            {
                if (__instance.hasMeter)
                {
                    float mass = 0;
                    foreach (EnergyGenerator.InputItem inputItem in __instance.formula.inputs)
                    {
                        mass += ___storage.GetAmountAvailable(inputItem.tag);
                    }
                    ___meter.SetPositionPercent(mass / __instance.formula.inputs[0].maxStoredMass);
                }
            }
            /*
            ---	if (hasMeter)
            +++ goto goto:
	            {
		            InputItem inputItem = formula.inputs[0];
		            float positionPercent = 0f;
		            GameObject gameObject = storage.FindFirst(inputItem.tag);
		            if ((Object)gameObject != (Object)null)
		            {
			            positionPercent = gameObject.GetComponent<PrimaryElement>().Mass / inputItem.maxStoredMass;
		            }
		            meter.SetPositionPercent(positionPercent);
                }
                goto:
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                FieldInfo hasMeter = typeof(EnergyGenerator).GetField(nameof(EnergyGenerator.hasMeter));
                bool result = false;
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction instruction = instructionsList[i];
                    if (instruction.opcode == OpCodes.Ldfld && (FieldInfo)instruction.operand == hasMeter)
                    {
#if DEBUG
                        Debug.Log("EnergyGenerator EnergySim200ms Transpiler injected");
#endif
                        yield return new CodeInstruction(OpCodes.Pop);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                        result = true;
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
                if (!result)
                {
                    Debug.LogWarning("Could not apply Transpiler to the 'EnergyGenerator.EnergySim200ms'");
                }
            }
        }
    }
}
