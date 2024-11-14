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
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
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
            PGameUtils.CopySoundsToAnim(ANIM_REACT_BUTT_SCRATCH, "anim_react_butt_scratch_kanim");
            PGameUtils.CopySoundsToAnim(ANIM_LOCO_ZOMBIE, "anim_loco_new_kanim");
            Utils.LoadEmbeddedAudioSheet("AudioSheets/SFXTags_Duplicants.csv");
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
#if false
        [HarmonyPatch(typeof(MinionConfig), nameof(MinionConfig.AddMinionAmounts))]
        private static class MinionConfig_AddMinionAmounts
        {
            private static void Postfix(Modifiers modifiers)
            {
                modifiers.initialAttributes.Add(JoyReactionExtraChance.Id);
            }
        }
#endif

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
#if false
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
#endif
        [HarmonyPatch(typeof(BaseMinionConfig), nameof(BaseMinionConfig.BaseOnSpawn))]
        private static class BaseMinionConfig_BaseOnSpawn
        {
            private static DupeEffectLightController.Def defLight = new DupeEffectLightController.Def();
            private static void Prefix(Tag duplicantModel, ref System.Func<RationalAi.Instance, StateMachine.Instance>[] rationalAiSM)
            {
                if (duplicantModel == GameTags.Minions.Models.Standard)
                {
                    rationalAiSM = rationalAiSM.Append(new System.Func<RationalAi.Instance, StateMachine.Instance>[] {
                        smi => new DupeEffectLightController.Instance(smi.master, defLight),
                        smi => new DupeEffectFlatulence.Instance(smi.master),
                        smi => new DupeEffectZombie.Instance(smi.master)
                    });
                }
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
            private static void Postfix(WorkerBase worker)
            {
                if (worker.TryGetComponent<Effects>(out var effects) && effects.HasEffect(GASSY_MOO_SPICE))
                    CreateEmoteChore(worker, ButtScratchEmote, 0.75f);
            }
        }

        // усиливаем газогенератор и добавляем анимацию
        [HarmonyPatch(typeof(Flatulence), "Emit")]
        private static class Flatulence_Emit
        {
            private static void Postfix(Flatulence __instance)
            {
                if (__instance.TryGetComponent<Effects>(out var effects) && effects.HasEffect(GASSY_MOO_SPICE))
                {
                    var smi = __instance.GetSMI<StaminaMonitor.Instance>();
                    if (!smi.IsNullOrStopped() && !smi.IsSleeping())
                        CreateEmoteChore(__instance, ButtScratchEmote, 0.35f);
                }
            }

            private static float GetEmitMass(GameObject flatulent, out SimHashes emit_element)
            {
                if (flatulent.TryGetComponent<Effects>(out var effects) && effects.HasEffect(GASSY_MOO_SPICE))
                {
                    emit_element = GassyMooSpiceEmitElement;
                    float mass = GassyMooSpiceEmitMass;
                    if (flatulent.TryGetComponent<Traits>(out var traits) && traits.HasTrait(FLATULENCE))
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

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL)
            {
                return TranspilerUtils.Wrap(instructions, original, IL, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions, ILGenerator IL)
            {
                var getEmitMass = typeof(Flatulence_Emit).GetMethodSafe(nameof(GetEmitMass), true, PPatchTools.AnyArguments);
                var mass = IL.DeclareLocal(typeof(float));
                var element = IL.DeclareLocal(typeof(SimHashes));
                int i = 0;
                instructions.Insert(i++, new CodeInstruction(OpCodes.Ldarg_1));
                instructions.Insert(i++, TranspilerUtils.GetLoadLocalInstruction(element.LocalIndex, true));
                instructions.Insert(i++, new CodeInstruction(OpCodes.Call, getEmitMass));
                instructions.Insert(i++, TranspilerUtils.GetStoreLocalInstruction(mass.LocalIndex));
                while (i < instructions.Count)
                {
                    if (instructions[i].LoadsConstant(TUNING.TRAITS.FLATULENCE_EMIT_MASS))
                    {
                        instructions[i] = TranspilerUtils.GetLoadLocalInstruction(mass.LocalIndex);
                    }
                    else if (instructions[i].LoadsConstant(SimHashes.Methane))
                    {
                        instructions[i] = TranspilerUtils.GetLoadLocalInstruction(element.LocalIndex);
                    }
                    i++;
                }
                return true;
            }
        }

        // небольшая косметика. разворачиваем многострочное описание ADDITIONAL_EFFECTS
        [HarmonyPatch(typeof(Effect), nameof(Effect.CreateTooltip))]
        private static class Effect_CreateTooltip
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL)
            {
                return TranspilerUtils.Wrap(instructions, original, IL, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions, ILGenerator IL)
            {
                var op_Implicit = typeof(StringEntry).GetMethodSafe("op_Implicit", true, typeof(StringEntry));
                var replace = typeof(string).GetMethodSafe(nameof(string.Replace), false, typeof(string), typeof(string));
                var linePrefix = typeof(Effect).GetMethodSafe(nameof(Effect.CreateTooltip), true, PPatchTools.AnyArguments)
                    ?.GetParameters()?.First(p => p.ParameterType == typeof(string) && p.Name == "linePrefix");

                if (op_Implicit != null && replace != null && linePrefix != null)
                {
                    int i = instructions.FindIndex(instr => instr.Calls(op_Implicit));
                    if (i != -1)
                    {
                        instructions.Insert(++i, new CodeInstruction(OpCodes.Ldstr, "\n"));
                        instructions.Insert(++i, TranspilerUtils.GetLoadArgInstruction(linePrefix.Position));
                        instructions.Insert(++i, new CodeInstruction(OpCodes.Call, replace));
                        return true;
                    }
                }
                return false;
            }
        }

        // добавляем семена орхидеи в посылку
        [HarmonyPatch(typeof(Immigration), "ConfigureCarePackages")]
        private static class Immigration_ConfigureCarePackages
        {
            private static bool Prepare() => ExoticSpicesOptions.Instance.carepackage_seeds_amount > 0;
            private static void Postfix(List<CarePackageInfo> ___carePackages)
            {
                var seed = new CarePackageInfo(EvilFlowerConfig.SEED_ID, ExoticSpicesOptions.Instance.carepackage_seeds_amount,
                    () => DiscoveredResources.Instance.IsDiscovered(EvilFlowerConfig.SEED_ID));
                ___carePackages.Add(seed);
            }
        }
    }
}
