using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Klei.AI;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace ExoticSpices
{
    using static ExoticSpicesAssets;

    internal sealed class ExoticSpicesPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(ExoticSpicesPatches));
            new POptions().RegisterOptions(this, typeof(ExoticSpicesOptions));
            new KAnimGroupManager().RegisterInteractAnims(ANIM_IDLE_ZOMBIE, ANIM_LOCO_ZOMBIE, ANIM_LOCO_WALK_ZOMBIE, ANIM_REACT_BUTT_SCRATCH);
            new ModdedSpicesSerializationManager().RegisterModdedSpices(PHOSPHO_RUFUS_SPICE, GASSY_MOO_SPICE, ZOMBIE_SPICE);
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
            LoadSprites();
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            InitStage1();
        }

        [HarmonyPatch(typeof(SpiceGrinder), nameof(SpiceGrinder.InitializeSpices))]
        private static class SpiceGrinder_InitializeSpices
        {
            private static void Postfix()
            {
                InitStage2();
            }
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            // костыль чтобы зомби работали ночью
            // к сожалению, придется вот так
            // сначала хотел просто поправить блоки расписания. но их сериализация сделана через жеппу.
            // лучше их не трогать вообще а то будут проблемы с загрузкой
            var preconditions = ChorePreconditions.instance;
            if (preconditions.IsScheduledTime.fn != IsScheduledTimeTireless)
            {
                orig_IsScheduledTime_fn = preconditions.IsScheduledTime.fn;
                preconditions.IsScheduledTime.fn = IsScheduledTimeTireless;
            }
            // похоже, игра формирует описания эффектов специй позже чем требуется. поможем ей
            foreach (var option in SpiceGrinder.SettingOptions.Values)
                _ = option.StatBonus;
        }

        private static Chore.PreconditionFn orig_IsScheduledTime_fn;
        private static bool IsScheduledTimeTireless(ref Chore.Precondition.Context context, object data)
        {
            bool result = orig_IsScheduledTime_fn(ref context, data);
            if (!result)
            {
                var blockType = (ScheduleBlockType)data;
                if (blockType != null)
                {
                    var schedules = Db.Get().ScheduleBlockTypes;
                    if ((blockType.IdHash == schedules.Work.IdHash || blockType.IdHash == schedules.Hygiene.IdHash)
                        && context.consumerState.prefabid.HasTag(Tireless))
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        // даём дуплику дополнительный шанс обрадоваться
        [HarmonyPatch(typeof(MinionConfig), nameof(MinionConfig.AddMinionAmounts))]
        private static class MinionConfig_AddMinionAmounts
        {
            private static void Postfix(Modifiers modifiers)
            {
                modifiers.initialAttributes.Add(JoyReactionExtraChance.Id);
            }
        }

        [HarmonyPatch(typeof(JoyBehaviourMonitor.Instance), nameof(JoyBehaviourMonitor.Instance.ShouldBeOverjoyed))]
        private static class JoyBehaviourMonitor_Instance_ShouldBeOverjoyed
        {
            private static void Postfix(JoyBehaviourMonitor.Instance __instance, ref bool __result)
            {
                if (!__result)
                {
                    var chance = JoyReactionExtraChance.Lookup(__instance.gameObject)?.GetTotalValue() ?? 0;
                    if (chance >= Random.value)
                        __result = true;
                }
            }
        }

        // прикручиваем красявости
        [HarmonyPatch(typeof(RationalAi), nameof(RationalAi.InitializeStates))]
        private static class RationalAi_InitializeStates
        {
            private static void Postfix(RationalAi __instance)
            {
                var defLight = new DupeEffectLightController.Def();
                __instance.alive
                    .ToggleStateMachine(smi => new DupeEffectLightController.Instance(smi.master, defLight))
                    .ToggleStateMachine(smi => new DupeEffectFlatulence.Instance(smi.master))
                    .ToggleStateMachine(smi => new DupeEffectZombie.Instance(smi.master));
            }
        }

        // дуплик под фоcфорной специей спит крепко
        [HarmonyPatch(typeof(SleepChore.States), nameof(SleepChore.States.InitializeStates))]
        private static class SleepChore_States_InitializeStates
        {
            private static void Postfix(SleepChore.States __instance)
            {
                __instance.sleep.Enter(smi =>
                {
                    if (smi.HasTag(GameTags.EmitsLight))
                        smi.sm.isInterruptable.Set(false, smi);
                });
            }
        }

        // жёппная анимация после сортира
        [HarmonyPatch(typeof(ToiletWorkableUse), "OnCompleteWork")]
        private static class ToiletWorkableUse_OnCompleteWork
        {
            private static void Postfix(Worker worker)
            {
                if (worker.GetComponent<Effects>().HasEffect(GASSY_MOO_SPICE))
                    CreateEmoteChore(worker, ButtScratchEmote, 0.75f);
            }
        }

        // усиливаем газогенератор и добавляем анимацию
        [HarmonyPatch(typeof(Flatulence), "Emit")]
        private static class Flatulence_Emit
        {
            private static void Postfix(Flatulence __instance)
            {
                if (__instance.GetComponent<Effects>().HasEffect(GASSY_MOO_SPICE))
                {
                    var smi = __instance.GetSMI<StaminaMonitor.Instance>();
                    if (!smi.IsNullOrStopped() && !smi.IsSleeping())
                        CreateEmoteChore(__instance, ButtScratchEmote, 0.35f);
                }
            }

            private static float GetEmitMass(GameObject flatulent, out SimHashes emit_element)
            {
                var effects = flatulent.GetComponent<Effects>();
                if (effects != null && effects.HasEffect(GASSY_MOO_SPICE))
                {
                    emit_element = GassyMooSpiceEmitElement;
                    float mass = GassyMooSpiceEmitMass;
                    var traits = flatulent.GetComponent<Traits>();
                    if (traits != null && traits.HasTrait(FLATULENCE))
                    {
                        emit_element = SimHashes.Methane;
                        mass = (2f * mass) + TUNING.TRAITS.FLATULENCE_EMIT_MASS;
                    }
                    return mass;
                }
                else
                {
                    emit_element = SimHashes.Methane;
                    return TUNING.TRAITS.FLATULENCE_EMIT_MASS;
                }
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator IL)
            {
                var iList = instructions.ToList();
                var getEmitMass = typeof(Flatulence_Emit).GetMethodSafe(nameof(GetEmitMass), true, PPatchTools.AnyArguments);
                var mass = IL.DeclareLocal(typeof(float));
                var element = IL.DeclareLocal(typeof(SimHashes));
                int i = 0;
                iList.Insert(i++, new CodeInstruction(OpCodes.Ldarg_1));
                iList.Insert(i++, TranspilerUtils.GetLoadLocalInstruction(element.LocalIndex, true));
                iList.Insert(i++, new CodeInstruction(OpCodes.Call, getEmitMass));
                iList.Insert(i++, TranspilerUtils.GetStoreLocalInstruction(mass.LocalIndex));
                while (i < iList.Count)
                {
                    if (iList[i].LoadsConstant(TUNING.TRAITS.FLATULENCE_EMIT_MASS))
                    {
                        iList[i] = TranspilerUtils.GetLoadLocalInstruction(mass.LocalIndex);
                    }
                    else if (iList[i].LoadsConstant(SimHashes.Methane))
                    {
                        iList[i] = TranspilerUtils.GetLoadLocalInstruction(element.LocalIndex);
                    }
                    i++;
                }
                return iList;
            }
        }

        // небольшая косметика. разворачиваем многострочное описание ADDITIONAL_EFFECTS
        [HarmonyPatch(typeof(Effect), nameof(Effect.CreateTooltip))]
        private static class Effect_CreateTooltip
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator IL)
            {
                var iList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var op_Implicit = typeof(StringEntry).GetMethodSafe("op_Implicit", true, typeof(StringEntry));
                var replace = typeof(string).GetMethodSafe(nameof(string.Replace), false, typeof(string), typeof(string));
                var linePrefix = typeof(Effect).GetMethodSafe(nameof(Effect.CreateTooltip), true, PPatchTools.AnyArguments)
                    ?.GetParameters()?.First(p => p.ParameterType == typeof(string) && p.Name == "linePrefix");

                bool result = false;
                if (op_Implicit != null && replace != null && linePrefix != null)
                {
                    int i = iList.FindIndex(instr =>
                        instr.opcode == OpCodes.Call && instr.operand is MethodInfo info && info == op_Implicit);
                    if (i != -1)
                    {
                        iList.Insert(++i, new CodeInstruction(OpCodes.Ldstr, "\n"));
                        iList.Insert(++i, TranspilerUtils.GetLoadArgInstruction(linePrefix.Position));
                        iList.Insert(++i, new CodeInstruction(OpCodes.Call, replace));
                        result = true;
#if DEBUG
                        Debug.Log($"'{methodName}' Transpiler injected");
#endif
                    }
                }
                if (!result)
                {
                    Debug.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return iList;
            }
        }
    }
}
