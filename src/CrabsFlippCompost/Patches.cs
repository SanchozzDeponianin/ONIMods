using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;

namespace CrabsFlippCompost
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
            Utils.LoadEmbeddedAudioSheet("AudioSheets/SFXTags.csv");
            harmony.PatchAll();
        }

        // добавить крабам новое поведение
        [HarmonyPatch(typeof(BaseCrabConfig), nameof(BaseCrabConfig.BaseCrab))]
        private static class BaseCrabConfig_BaseCrab
        {
            internal static void Postfix(GameObject __result, bool is_baby)
            {
                if (!is_baby)
                    __result.AddOrGetDef<FlippCompostMonitor.Def>();
            }
            // внедряем новое поведение
            /*
            ChoreTable.Builder chore_table = new ChoreTable.Builder().Add(new DeathStates.Def())
                <блаблабла>
                .Add(new CritterCondoStates.Def());
        +++     .Add(new ApproachBehaviourStates.Def())
                .Add(new CritterEmoteStates.Def());
                .PopInterruptGroup()
                .Add(new IdleStates.Def());
            */
            private static ChoreTable.Builder Inject(ChoreTable.Builder builder, bool is_baby)
            {
                return builder.Add(new ApproachBehaviourStates.Def(FlippCompostMonitor.ID, FlippCompostMonitor.BEHAVIOUR_TAG)
                { preAnim = "slap_pre", loopAnim = "slap", pstAnim = "slap_pst" }, !is_baby);
            }

            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions, MethodBase original)
            {
                var is_baby = original.GetParameters().FindFirst(p => p.ParameterType == typeof(bool) && p.Name == "is_baby");
                var add = typeof(ChoreTable.Builder).GetMethodSafe(nameof(ChoreTable.Builder.Add), false,
                    typeof(StateMachine.BaseDef), typeof(bool), typeof(int));
                var emote = typeof(CritterEmoteStates.Def).GetConstructors()[0];
                var inject = typeof(BaseCrabConfig_BaseCrab).GetMethodSafe(nameof(Inject), true, typeof(ChoreTable.Builder), typeof(bool));

                if (add == null || emote == null || inject == null)
                    return false;

                int i = instructions.FindIndex(inst => inst.Is(OpCodes.Newobj, emote));
                if (i == -1)
                    return false;

                int j = instructions.FindLastIndex(i, inst => inst.Calls(add));
                if (j == -1)
                    return false;

                instructions.Insert(++j, is_baby.GetLoadArgInstruction());
                instructions.Insert(++j, new CodeInstruction(OpCodes.Call, inject));
                return true;
            }
        }

        // играть анимацию несколько раз
        [HarmonyPatch(typeof(ApproachBehaviourStates), nameof(ApproachBehaviourStates.InitializeStates))]
        private static class ApproachBehaviourStates_InitializeStates
        {
            private static void Postfix(ApproachBehaviourStates __instance)
            {
                __instance.interact.loop.Enter(QueueMoreAnims);
            }
            private static void QueueMoreAnims(ApproachBehaviourStates.Instance smi)
            {
                const int count = (FlippCompostMonitor.ANIM_LOOPS * 2) - 1;
                var kbac = smi.Get<KAnimControllerBase>();
                if (smi.def.monitorId == FlippCompostMonitor.ID && kbac != null)
                {
                    for (int i = 0; i < count; i++)
                        kbac.Queue(smi.def.loopAnim);
                }
            }
        }

        // новый статус в компосте, без чоры
        [HarmonyPatch(typeof(Compost.States), nameof(Compost.States.InitializeStates))]
        internal static class Compost_States
        {
            internal static Compost.States.State waiting;

            private static void Postfix(Compost.States __instance)
            {
                waiting = __instance.CreateState("waiting_critter");

                __instance.inert
                    .TagTransition(GameTags.Creatures.ReservedByCreature, waiting, false);

                waiting
                    .EventTransition(GameHashes.OperationalChanged, __instance.disabled, smi => !smi.GetComponent<Operational>().IsOperational)
                    .TagTransition(GameTags.Creatures.ReservedByCreature, __instance.inert, true)
                    .PlayAnim("on")
                    .ToggleStatusItem(Db.Get().BuildingStatusItems.AwaitingCompostFlip, null);
            }
        }
    }
}
