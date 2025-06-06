using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;

namespace WhereMyLoot
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
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

            private static readonly Vector2I dropOffset = new(0, 1);

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

                    // штука для разблокировки исследований
                    var smi = __instance.GetSMI<POITechItemUnlocks.Instance>();
                    if (!smi.IsNullOrStopped() && smi.IsInsideState(smi.sm.locked))
                    {
                        smi.sm.seenNotification.Set(true, smi);
                        smi.UnlockTechItems();
                        smi.sm.pendingChore.Set(false, smi);
                        if (!string.IsNullOrEmpty(smi.def.loreUnlockId))
                            Game.Instance.unlocks.Unlock(smi.def.loreUnlockId, true);
                        //smi.gameObject.Trigger((int)GameHashes.UIRefresh, null);
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
            private static void DeactivateScreenIsMarkedForDemolition(LoreBearer loreBearer, KScreen screen)
            {
                if (loreBearer != null && loreBearer.TryGetComponent<Demolishable>(out var demolishable) && isMarkedForDemolition.Get(demolishable))
                {
                    screen?.Deactivate();
                }
            }
            /*
            --- var infoDialogScreen = LoreBearer.ShowPopupDialog().blabla.AddDefaultOK();
                blablablabla;
                // и на версиях 531669 и ранее вот это много раз:
                if (чтототам)
                {
                    блаблабла;
            ---     return;
            +++     goto end:
                }
                blablablabla;
                // и в конце
            --- return;
            +++ goto end:
            +++ end:
            +++ DeactivateScreenIsMarkedForDemolition(this, infoDialogScreen);
                return;
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL)
            {
                return instructions.Transpile(original, IL, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions, ILGenerator IL)
            {
                var addDefaultOK = typeof(InfoDialogScreen).GetMethodSafe(nameof(InfoDialogScreen.AddDefaultOK), false, typeof(bool));
                var deactivateScreen = typeof(LoreBearer_OnClickRead).GetMethodSafe(nameof(DeactivateScreenIsMarkedForDemolition), true, PPatchTools.AnyArguments);
                bool result = false;
                if (addDefaultOK != null && deactivateScreen != null)
                {
                    var label = IL.DefineLabel();
                    CodeInstruction ldloc_Screen = null;
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(addDefaultOK) && instructions[i + 1].IsStloc())
                        {
                            ldloc_Screen = TranspilerUtils.GetMatchingLoadInstruction(instructions[i + 1]);
                            continue;
                        }
                        if (instructions[i].opcode == OpCodes.Ret)
                        {
                            var instruction = new CodeInstruction(instructions[i]);
                            instruction.opcode = OpCodes.Br_S;
                            instruction.operand = label;
                            instructions[i] = instruction;
                        }
                    }
                    var end = new CodeInstruction(OpCodes.Nop);
                    end.labels.Add(label);
                    instructions.Add(end);
                    if (ldloc_Screen != null)
                    {
                        instructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                        instructions.Add(ldloc_Screen);
                        instructions.Add(new CodeInstruction(OpCodes.Call, deactivateScreen));
                        result = true;
                    }
                    instructions.Add(new CodeInstruction(OpCodes.Ret));
                }
                return result;
            }
        }
    }
}
