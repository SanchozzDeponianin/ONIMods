using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Klei.AI;
using TUNING;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace VaricolouredBalloons
{
    internal sealed class VaricolouredBalloonsPatches : KMod.UserMod2
    {
        private const string HasBalloon = "HasBalloon";

        private static readonly IDetouredField<GetBalloonWorkable, BalloonArtistChore.StatesInstance> BALLOONARTIST = PDetours.DetourField<GetBalloonWorkable, BalloonArtistChore.StatesInstance>("balloonArtist");

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary(true);
            new PPatchManager(harmony).RegisterPatchClass(typeof(VaricolouredBalloonsPatches));
            new POptions().RegisterOptions(this, typeof(VaricolouredBalloonsOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
            VaricolouredBalloonsHelper.InitializeAnims();
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            VaricolouredBalloonsOptions.Reload();
            EquippableBalloon_OnSpawn.HasBalloonDuration = Db.Get().effects.Get(HasBalloon).duration;
        }

        [HarmonyPatch(typeof(MinionConfig), nameof(MinionConfig.CreatePrefab))]
        private static class MinionConfig_CreatePrefab
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
        private static class BalloonArtistChore_States_InitializeStates
        {
            private static void Postfix(BalloonArtistChore.States __instance)
            {
                __instance.balloonStand.Enter((BalloonArtistChore.StatesInstance smi) => smi.GetComponent<VaricolouredBalloonsHelper>()?.RandomizeArtistBalloonSymbolIdx());
                __instance.balloonStand.idle.Enter((BalloonArtistChore.StatesInstance smi) =>
                {
                    var artist = smi.GetComponent<VaricolouredBalloonsHelper>();
                    if (artist != null)
                    {
                        artist.ApplySymbolOverrideByIdx(artist.ArtistBalloonSymbolIdx);
                    }
                });
            }
        }

        // получатель:

        // внедряем перехватчик "задание 'получить баллон' начато"
        [HarmonyPatch(typeof(BalloonStandConfig), nameof(BalloonStandConfig.OnSpawn))]
        private static class BalloonStandConfig_OnSpawn
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                return TranspilerInjectOnBeginChoreAction(instructions, method);
            }
        }

        [HarmonyPatch(typeof(BalloonStandConfig), "MakeNewBalloonChore")]
        private static class BalloonStandConfig_MakeNewBalloonChore
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                return TranspilerInjectOnBeginChoreAction(instructions, method);
            }

            // когда "задание 'получить баллон' завершено" - рандомим новый индекс для артиста
            private static void Postfix(Chore chore)
            {
                var balloonWorkable = chore.target?.GetComponent<GetBalloonWorkable>();
                if (balloonWorkable != null)
                {
                    BALLOONARTIST.Get(balloonWorkable)?.GetComponent<VaricolouredBalloonsHelper>()?.RandomizeArtistBalloonSymbolIdx();
                }
            }
        }

        // перехватываем "задание 'получить баллон' начато"
        // вытаскиваем индекс из артиста, запихиваем его в получателя, и применяем подмену символа анимации 
        // уничтожаем предыдущий FX-объект баллона если он есть
        private static void OnBeginGetBalloonChore(BalloonStandConfig balloonStandConfig, Chore chore)
        {
            var balloonWorkable = chore.target?.GetComponent<GetBalloonWorkable>();
            if (balloonWorkable != null)
            {
                uint idx = BALLOONARTIST.Get(balloonWorkable)?.GetComponent<VaricolouredBalloonsHelper>()?.ArtistBalloonSymbolIdx ?? 0;
                var receiver = chore.driver?.GetComponent<VaricolouredBalloonsHelper>();
                if (receiver != null)
                {
                    receiver.ReceiverBalloonSymbolIdx = idx;
                    receiver.ApplySymbolOverrideByIdx(idx);
                    if (receiver.fx != null)
                    {
                        receiver.fx.StopSM("Unequipped");
                        receiver.fx = null;
                    }
                }
            }
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
        +++     on_begin: VaricolouredBalloonsPatches.OnBeginGetBalloonChore
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
        private static IEnumerable<CodeInstruction> TranspilerInjectOnBeginChoreAction(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            var iList = instructions.ToList();
            string methodName = method.DeclaringType.FullName + "." + method.Name;

            var makenewballoonchore = AccessTools.Method(typeof(BalloonStandConfig), "MakeNewBalloonChore");
            var onbegingetballoonchore = AccessTools.Method(typeof(VaricolouredBalloonsPatches), nameof(VaricolouredBalloonsPatches.OnBeginGetBalloonChore));

            bool result = false;
            for (int i = 0; i < iList.Count; i++)
            {
                var instruction = iList[i];
                if (instruction.opcode == OpCodes.Ldftn && (MethodInfo)instruction.operand == makenewballoonchore)
                {
#if DEBUG
                    PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                    yield return instruction;
                    i++;
                    instruction = iList[i];
                    yield return instruction;           // new Action<Chore>(this.MakeNewBalloonChore)
                    i++;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldftn, onbegingetballoonchore);
                    yield return instruction;           // new Action<Chore>(VaricolouredBalloonsPatches.OnBeginGetBalloonChore)
                    result = true;
                }
                else
                {
                    yield return instruction;
                }
            }
            if (!result)
            {
                PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
            }
        }

        // сам носимый баллон:

        // внедряем в FX-объект баллона контроллер подмены анимации
        // вытаскиваем индекс из носителя и применяем подмену символа анимации
        // запоминаем ссылку на новый FX-объект 
        private static void ApplySymbolOverrideBalloonFX(BalloonFX.Instance smi, KBatchedAnimController kbac)
        {
            kbac.usingNewSymbolOverrideSystem = true;
            var symbolOverrideController = SymbolOverrideControllerUtil.AddToPrefab(kbac.gameObject);
            var receiver = smi.master.GetComponent<VaricolouredBalloonsHelper>();
            if (receiver != null)
            {
                receiver.fx = smi;
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
        private static class BalloonFX_Instance_Constructor
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var applysymboloverrideballoonfx = AccessTools.Method(typeof(VaricolouredBalloonsPatches), nameof(VaricolouredBalloonsPatches.ApplySymbolOverrideBalloonFX));
                var iList = instructions.ToList();
                bool result = false;
                for (int i = 0; i < iList.Count; i++)
                {
                    var instruction = iList[i];
                    if (instruction.opcode == OpCodes.Ret)
                    {
#if DEBUG
                        PUtil.LogDebug($"'{nameof(BalloonFX.Instance)}.Constructor' Transpiler injected");
#endif
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return new CodeInstruction(OpCodes.Call, applysymboloverrideballoonfx);
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
                    PUtil.LogWarning($"Could not apply Transpiler to the '{nameof(BalloonFX.Instance)}.Constructor'");
                }
            }
        }

        // обработка косяка в базовой игре
        // когда пришло время разэкипировки баллона, в зависимости от настроек:
        // либо уничтожаем FX-объект баллона
        // либо не трогаем его и превращаем баг в фичу
        // а также обнуляем ссылку на FX-объект в классе конфига, во избежание конфликтов.

        // обработка третьего косяка в базовой игре
        // если применить инструмент удаления песочницы к дупликанту с шариком - игра вылетит
        // тут замешана магия Unity и её объекты Шрёдингера которые null и not null одновременно
        [HarmonyPatch(typeof(EquippableBalloonConfig), "OnUnequipBalloon")]
        private static class EquippableBalloonConfig_OnUnequipBalloon
        {
            private static void Prefix(ref BalloonFX.Instance ___fx)
            {
                ___fx = null;
            }

            private static KMonoBehaviour DestroyFX(KMonoBehaviour target)
            {
                if (VaricolouredBalloonsOptions.Instance.DestroyFXAfterEffectExpired && target != null)
                {
                    var carrier = target.GetComponent<VaricolouredBalloonsHelper>();
                    if (carrier != null && carrier.fx != null)
                    {
                        carrier.fx.StopSM("Unequipped");
                        carrier.fx = null;
                    }
                }
                return target;
            }

            /*
                var minionAssignablesProxy = soleOwner.GetComponent<MinionAssignablesProxy>();
            --- if (minionAssignablesProxy.target != null)
            +++ if ((minionAssignablesProxy.target as KMonoBehaviour) != (UnityEngine.Object)null)
                {
            +++     DestroyFX(minionAssignablesProxy.target as KMonoBehaviour);
                    var effects = (minionAssignablesProxy.target as KMonoBehaviour).GetComponent<Effects>();
                    if (effects != null)
                        effects.Remove("HasBalloon");
                }
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var iList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var get_target = typeof(MinionAssignablesProxy)
                    .GetPropertySafe<IAssignableIdentity>(nameof(MinionAssignablesProxy.target), false)?.GetGetMethod(true);
                var typeKMonoBehaviour = typeof(KMonoBehaviour);
                var op_Inequality = typeof(UnityEngine.Object)
                    .GetMethodSafe("op_Inequality", true, typeof(UnityEngine.Object), typeof(UnityEngine.Object));
                var destroyFX = typeof(EquippableBalloonConfig_OnUnequipBalloon)
                    .GetMethodSafe(nameof(DestroyFX), true, PPatchTools.AnyArguments);

                bool result1 = false, result2 = false;

                if (get_target != null && typeKMonoBehaviour != null && op_Inequality != null && destroyFX != null)
                {
                    for (int i = 0; i < iList.Count; i++)
                    {
                        if (iList[i].Calls(get_target))
                        {
                            i++;
                            if (iList[i].Branches(out _))
                            {
                                iList.Insert(i++, new CodeInstruction(OpCodes.Isinst, typeKMonoBehaviour));
                                iList.Insert(i++, new CodeInstruction(OpCodes.Ldnull));
                                iList.Insert(i++, new CodeInstruction(OpCodes.Call, op_Inequality));
                                result1 = true;
                            }
                            else if (iList[i].opcode == OpCodes.Isinst && Equals(iList[i].operand, typeKMonoBehaviour))
                            {
                                iList.Insert(++i, new CodeInstruction(OpCodes.Call, destroyFX));
                                result2 = true;
                            }
                        }
                        if (result1 && result2)
                        {
#if DEBUG
                            PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                            break;
                        }
                    }
                }
                if (!(result1 && result2))
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return iList;
            }
        }

        // исправление второго косяка в базовой игре
        // фактическое время разэкипировки баллона вдвое меньше длительности эффекта, так что эффект исчезает преждевременно
        // также при загрузке сейва эффект вешается с полным сроком, и исчезает преждевременно еще быстрее
        // корректируем длительность эффекта до актуального значения
        // опционально корректируем время разэкипировки баллона, чтобы соответствовало заявленной длительности эффекта
        [HarmonyPatch(typeof(EquippableBalloon), "OnSpawn")]
        private static class EquippableBalloon_OnSpawn
        {
            /*
                base.OnSpawn();
	        --- smi.transitionTime = GameClock.Instance.GetTime() + TRAITS.JOY_REACTIONS.JOY_REACTION_DURATION;
            +++ smi.transitionTime = GameClock.Instance.GetTime() + FixJoyReactionDuration(TRAITS.JOY_REACTIONS.JOY_REACTION_DURATION);
	            smi.StartSM();
            */
            public static float HasBalloonDuration;
            private static float FixJoyReactionDuration(float duration)
            {
                return VaricolouredBalloonsOptions.Instance.FixEffectDuration ? HasBalloonDuration : duration;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var iList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var Duration = typeof(TRAITS.JOY_REACTIONS).GetFieldSafe(nameof(TRAITS.JOY_REACTIONS.JOY_REACTION_DURATION), true);
                var FixDuration = typeof(EquippableBalloon_OnSpawn).GetMethodSafe(nameof(FixJoyReactionDuration), true, PPatchTools.AnyArguments);

                bool result = false;
                if (Duration != null && FixDuration != null)
                {
                    for (int i = 0; i < iList.Count; i++)
                    {
                        var instruction = iList[i];
                        if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo info && info == Duration)
                        {
                            iList.Insert(++i, new CodeInstruction(OpCodes.Call, FixDuration));
#if DEBUG
                            PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                            result = true;
                            break;
                        }
                    }
                }
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return iList;
            }

            private static void Postfix(EquippableBalloon __instance)
            {
                var effect = (__instance?.GetComponent<Equippable>()?.assignee?.GetSoleOwner()?
                    .GetComponent<MinionAssignablesProxy>()?.target as KMonoBehaviour)?.GetComponent<Effects>()?.Get(HasBalloon);
                if (effect != null)
                    effect.timeRemaining = __instance.smi.transitionTime - GameClock.Instance.GetTime();
            }
        }
    }
}
