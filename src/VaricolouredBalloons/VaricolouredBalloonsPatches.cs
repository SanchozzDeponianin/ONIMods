using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace VaricolouredBalloons
{
    internal static class VaricolouredBalloonsPatches
    {
        [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
        internal static class Db_Initialize
        {
            private static void Prefix()
            {
                VaricolouredBalloonsHelper.Initialize();
            }
        }

        [HarmonyPatch(typeof(MinionConfig), nameof(MinionConfig.CreatePrefab))]
        internal static class MinionConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<VaricolouredBalloonsHelper>();
            }
        }

        // артист:

        // рандомим индекс символа когда артист начинает раздачу
        // применяем подмену символа анимации в начале каждой итерации
        [HarmonyPatch(typeof(BalloonArtistChore.States), nameof(BalloonArtistChore.States.InitializeStates))]
        internal static class BalloonArtistChore_States_InitializeStates
        {
            private static void Postfix(BalloonArtistChore.States __instance)
            {
                __instance.balloonStand.Enter((BalloonArtistChore.StatesInstance smi) => smi.GetComponent<VaricolouredBalloonsHelper>()?.SetArtistBalloonSymbolIdx(VaricolouredBalloonsHelper.GetRandomSymbolIdx()));
                __instance.balloonStand.idle.Enter((BalloonArtistChore.StatesInstance smi) =>
                {
                    VaricolouredBalloonsHelper.ApplySymbolOverrideByIdx(smi.GetComponent<SymbolOverrideController>(), smi.GetComponent<VaricolouredBalloonsHelper>()?.ArtistBalloonSymbolIdx ?? 0);
                });
            }
        }

        // получатель:

        // внедряем перехватчик "задание 'получить баллон' начато"
        [HarmonyPatch(typeof(BalloonStandConfig), nameof(BalloonStandConfig.OnSpawn))]
        internal static class BalloonStandConfig_OnSpawn
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return TranspilerInjectOnBeginChoreAction(instructions);
            }
        }

        [HarmonyPatch(typeof(BalloonStandConfig), "MakeNewBalloonChore")]
        internal static class BalloonStandConfig_MakeNewBalloonChore
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return TranspilerInjectOnBeginChoreAction(instructions);
            }

            // когда "задание 'получить баллон' завершено" - рандомим новый индекс для артиста
            private static void Postfix(Chore chore)
            {
                int idx = VaricolouredBalloonsHelper.GetRandomSymbolIdx();
                GetBalloonWorkable balloonWorkable = chore.target?.GetComponent<GetBalloonWorkable>();
                if (balloonWorkable != null)
                {
                    Traverse.Create(balloonWorkable).Field<BalloonArtistChore.StatesInstance>("balloonArtist").Value?.GetComponent<VaricolouredBalloonsHelper>()?.SetArtistBalloonSymbolIdx(idx);
                }
            }
        }

        // перехватываем "задание 'получить баллон' начато"
        // вытаскиваем индекс из артиста, запихиваем его в получателя, и применяем подмену символа анимации 
        private static void OnBeginBalloonChore(BalloonStandConfig balloonStandConfig, Chore chore)
        {
            int idx = 0;
            GetBalloonWorkable balloonWorkable = chore.target?.GetComponent<GetBalloonWorkable>();
            if (balloonWorkable != null)
            {
                idx = Traverse.Create(balloonWorkable).Field<BalloonArtistChore.StatesInstance>("balloonArtist").Value?.GetComponent<VaricolouredBalloonsHelper>()?.ArtistBalloonSymbolIdx ?? 0;
                chore.driver?.GetComponent<VaricolouredBalloonsHelper>()?.SetReceiverBalloonSymbolIdx(idx);
            }
            VaricolouredBalloonsHelper.ApplySymbolOverrideByIdx(chore.driver?.GetComponent<SymbolOverrideController>(), idx);
        }

        // внедряем перехватчик "задание 'получить баллон' начато"
        /*
            WorkChore<GetBalloonWorkable> workChore = new WorkChore<GetBalloonWorkable>(
                chore_type: Db.Get().ChoreTypes.JoyReaction, 
                target: component, 
                chore_provider: null, 
                run_until_complete: true, 
                on_complete: MakeNewBalloonChore, 
        ---     on_begin: null, 
        +++     on_begin: VaricolouredBalloonsPatches.OnBeginBalloonChore
                on_end: null, 
                allow_in_red_alert: true, 
                schedule_block: Db.Get().ScheduleBlockTypes.Recreation, 
                ignore_schedule_block: false, 
                only_when_operational: true, 
                override_anims: null, 
                is_preemptable: false, 
                allow_in_context_menu: true, 
                allow_prioritization: true, 
                priority_class: PriorityScreen.PriorityClass.high, 
                priority_class_value: 5, 
                ignore_building_assignment: true, 
                add_to_daily_report: true
                );            
        */
        private static IEnumerable<CodeInstruction> TranspilerInjectOnBeginChoreAction(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo makenewballoonchore = AccessTools.Method(typeof(BalloonStandConfig), "MakeNewBalloonChore");
            MethodInfo onbeginballoonchore = AccessTools.Method(typeof(VaricolouredBalloonsPatches), "OnBeginBalloonChore");

            List<CodeInstruction> instructionsList = instructions.ToList();
            bool result = false;
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction instruction = instructionsList[i];
                if (instruction.opcode == OpCodes.Ldftn && (MethodInfo)instruction.operand == makenewballoonchore)
                {
                    yield return instruction;
                    i++;
                    instruction = instructionsList[i];
                    yield return instruction;           // new Action<Chore>(this.MakeNewBalloonChore)
                    i++;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldftn, onbeginballoonchore); // 
                    yield return instruction;           // new Action<Chore>(VaricolouredBalloonsPatches.OnBeginBalloonChore)
                    result = true;
                }
                else
                {
                    yield return instruction;
                }
            }
            if (!result)
            {
                Debug.LogWarning($"Could not apply {nameof(TranspilerInjectOnBeginChoreAction)}");
            }
        }

        // сам носимый баллон:

        // внедряем в FX-объект баллона контроллер подмены анимации
        // вытаскиваем индекс из носителя и применяем подмену символа анимации
        public static void ApplySymbolOverrideBalloonFX(BalloonFX.Instance smi, KBatchedAnimController kbac)
        {
            kbac.usingNewSymbolOverrideSystem = true;
            SymbolOverrideController symbolOverrideController = SymbolOverrideControllerUtil.AddToPrefab(kbac.gameObject);
            VaricolouredBalloonsHelper receiver = smi.master.GetComponent<VaricolouredBalloonsHelper>();
            if (receiver != null)
            {
                VaricolouredBalloonsHelper.ApplySymbolOverrideByIdx(symbolOverrideController, receiver.ReceiverBalloonSymbolIdx);
            }
        }

        // внедряем перехватчик создания нового FX-объекта баллона
        /*
            public Instance(IStateMachineTarget master) : base(master)
		    {
			    KBatchedAnimController kBatchedAnimController = FXHelpers.CreateEffect("balloon_anim_kanim", master.gameObject.transform.GetPosition() + new Vector3(0f, 0.3f, 1f), master.transform, true, Grid.SceneLayer.Creatures, false);
			    base.sm.fx.Set(kBatchedAnimController.gameObject, base.smi);
			    kBatchedAnimController.GetComponent<KBatchedAnimController>().defaultAnim = "idle_default";
			    master.GetComponent<KBatchedAnimController>().GetSynchronizer().Add(kBatchedAnimController.GetComponent<KBatchedAnimController>());
        +++     VaricolouredBalloonsPatches.ApplySymbolOverrideBalloonFX(this, kBatchedAnimController);
		    }
        */
        [HarmonyPatch(typeof(BalloonFX.Instance), MethodType.Constructor, new Type[] { typeof(IStateMachineTarget) })]
        internal static class BalloonFX_Instance_Constructor
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                bool result = false;
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction instruction = instructionsList[i];
                    if (instruction.opcode == OpCodes.Ret)
                    {
#if DEBUG
                        Debug.Log($"'{nameof(BalloonFX.Instance)}.Constructor' Transpiler injected");
#endif
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return new CodeInstruction(OpCodes.Call, typeof(VaricolouredBalloonsPatches).GetMethod(nameof(VaricolouredBalloonsPatches.ApplySymbolOverrideBalloonFX)));
                        yield return instruction;
                        result = true;
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
                if (!result)
                {
                    Debug.LogWarning($"Could not apply Transpiler to the '{nameof(BalloonFX.Instance)}.Constructor'");
                }
            }
        }
    }
}
