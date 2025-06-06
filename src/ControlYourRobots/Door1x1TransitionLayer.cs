using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;

namespace ControlYourRobots
{
    // штуковина для открытия/закрытия дверей при прохождении. для существ 1х1
    // отрезана проверка наличия двери на клетку выше
    [HarmonyPatch]
    public class Door1x1TransitionLayer : DoorTransitionLayer
    {
        public Door1x1TransitionLayer(Navigator navigator) : base(navigator) { }

        public override void BeginTransition(Navigator navigator, Navigator.ActiveTransition transition)
        {
            if (patched)
                PatchedBeginTransition(this, navigator, transition);
            else
                base.BeginTransition(navigator, transition);
        }

        private static bool patched = false;

        [HarmonyReversePatch(HarmonyReversePatchType.Original)]
        [HarmonyPatch(typeof(DoorTransitionLayer), nameof(DoorTransitionLayer.BeginTransition))]
        private static void PatchedBeginTransition(DoorTransitionLayer layer, Navigator navigator, Navigator.ActiveTransition transition)
        {
#pragma warning disable CS8321
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) =>
                instructions.Transpile(original, RemoveDoorAbove);
#pragma warning restore CS8321
            layer.BeginTransition(navigator, transition);
        }
        /*
        --- this.AddDoor(Grid.CellAbove(cell2));
        */
        private static bool RemoveDoorAbove(List<CodeInstruction> instructions)
        {
            var cell_above = typeof(Grid).GetMethodSafe(nameof(Grid.CellAbove), true, typeof(int));
            var add_door = typeof(DoorTransitionLayer).GetMethodSafe("AddDoor", false, typeof(int));
            if (cell_above != null && add_door != null)
            {
                int i = instructions.FindIndex(inst => inst.Calls(cell_above));
                if (i != -1)
                {
                    int j = instructions.FindIndex(i, inst => inst.Calls(add_door));
                    if (j != -1)
                    {
                        var pop = new CodeInstruction(OpCodes.Pop);
                        instructions[j] = pop;
                        instructions.Insert(j, pop);
                        instructions.RemoveAt(i);
                        patched = true;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
