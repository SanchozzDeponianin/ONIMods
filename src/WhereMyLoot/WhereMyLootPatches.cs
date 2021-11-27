using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;

namespace WhereMyLoot
{
    internal sealed class WhereMyLootPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary(true);
        }

        [HarmonyPatch(typeof(Demolishable), "TriggerDestroy")]
        internal static class Demolishable_TriggerDestroy
        {
            private static readonly IDetouredField<Demolishable, bool> destroyed =
                PDetours.DetourFieldLazy<Demolishable, bool>("destroyed");

            private static readonly IDetouredField<SetLocker, bool> used =
                PDetours.DetourFieldLazy<SetLocker, bool>("used");
            private static readonly DetouredMethod<System.Action<SetLocker>> CompleteChore =
                PDetours.DetourLazy<System.Action<SetLocker>>(typeof(SetLocker), "CompleteChore");

            private static readonly IDetouredField<LoreBearer, bool> BeenClicked =
                PDetours.DetourFieldLazy<LoreBearer, bool>("BeenClicked");
            private static readonly DetouredMethod<Action<LoreBearer>> OnClickRead =
                PDetours.DetourLazy<System.Action<LoreBearer>>(typeof(LoreBearer), "OnClickRead");

            private static readonly Vector2I dropOffset = new Vector2I(0, 1);

            private static void Prefix(Demolishable __instance)
            {
                if (__instance != null && !destroyed.Get(__instance))
                {
                    // лутаемые объекты типа шкафчиков
                    var setLocker = __instance.GetComponent<SetLocker>();
                    // if (!setLocker.used) setLocker.CompleteChore();
                    if (setLocker != null && !used.Get(setLocker))
                        CompleteChore.Invoke(setLocker);

                    // объекты которые можно "осмотреть", запись в кодекс
                    var loreBearer = __instance.GetComponent<LoreBearer>();
                    // if (!loreBearer.BeenClicked) loreBearer.OnClickRead();
                    if (loreBearer != null && !BeenClicked.Get(loreBearer))
                        OnClickRead.Invoke(loreBearer);

                    // калибратор. если не использован - нужно дропнуть зарядник
                    var geneShuffler = __instance.GetComponent<GeneShuffler>();
                    if (geneShuffler != null && !geneShuffler.IsConsumed)
                    {
                        geneShuffler.IsConsumed = true;
                        Scenario.SpawnPrefab(Grid.PosToCell(__instance), dropOffset.x, dropOffset.y, GeneShufflerRechargeConfig.ID, Grid.SceneLayer.Front).SetActive(true);
                        PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Plus, Assets.GetPrefab(GeneShufflerRechargeConfig.ID.ToTag()).GetProperName(), __instance.transform, 1.5f, false);
                    }
                }
            }
        }

        // подавляем надоедливый диалог об обнаружении новых записей лора
        [HarmonyPatch(typeof(LoreBearer), "OnClickRead")]
        internal static class LoreBearer_OnClickRead
        {
            private static readonly IDetouredField<Demolishable, bool> isMarkedForDemolition =
                PDetours.DetourFieldLazy<Demolishable, bool>("isMarkedForDemolition");
            private static void ActivateScreen(LoreBearer loreBearer, KScreen screen)
            {
                var demolishable = loreBearer?.GetComponent<Demolishable>();
                if (demolishable != null && isMarkedForDemolition.Get(demolishable))
                {
                    screen.Deactivate();
                    return;
                }
                screen.Activate();
            }
            /*
            --- var infoDialogScreen = GameScreenManager.Instance.StartScreen(blabla);
            +++ var infoDialogScreen = GameScreenManager.Instance.InstantiateScreen(blabla);
                blablablabla;
                // и вот это много раз:
                if (чтототам)
                {
                    блаблабла;
            ---     return;
            +++     goto end:
                }
                blablablabla;
            +++ end:
            +++ ActivateScreen(this, infoDialogScreen);
                return;
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator IL)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var StartScreen = typeof(GameScreenManager).GetMethodSafe(nameof(GameScreenManager.StartScreen), false, PPatchTools.AnyArguments);
                var InstantiateScreen = typeof(GameScreenManager).GetMethodSafe(nameof(GameScreenManager.InstantiateScreen), false, PPatchTools.AnyArguments);
                var activateScreen = typeof(LoreBearer_OnClickRead).GetMethodSafe(nameof(ActivateScreen), true, PPatchTools.AnyArguments);

                bool result = false;
                if (StartScreen != null && InstantiateScreen != null && activateScreen != null)
                {
                    instructionsList = PPatchTools.ReplaceMethodCall(instructionsList, StartScreen, InstantiateScreen).ToList();
                    var label = IL.DefineLabel();
                    for (int i = 0; i < instructionsList.Count; i++)
                    {
                        var instruction = instructionsList[i];
                        if (instruction.opcode == OpCodes.Ret)
                        {
                            instruction.opcode = OpCodes.Br_S;
                            instruction.operand = label;
                        }
                    }
                    var end = new CodeInstruction(OpCodes.Nop);
                    end.labels.Add(label);
                    instructionsList.Add(end);
                    instructionsList.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    instructionsList.Add(new CodeInstruction(OpCodes.Ldloc_0));
                    instructionsList.Add(new CodeInstruction(OpCodes.Call, activateScreen));
                    instructionsList.Add(new CodeInstruction(OpCodes.Ret));
                    result = true;
                }
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
#if DEBUG
                else
                    PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                return instructionsList;
            }
        }
    }
}
