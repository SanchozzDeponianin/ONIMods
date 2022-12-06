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
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, TranspilerInjectOnBeginChoreAction);
            }
        }

        [HarmonyPatch(typeof(BalloonStandConfig), "MakeNewBalloonChore")]
        private static class BalloonStandConfig_MakeNewBalloonChore
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, TranspilerInjectOnBeginChoreAction);
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
        private static void OnBeginGetBalloonChore(Chore chore)
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
            WorkChore<GetBalloonWorkable> workChore = new WorkChore<GetBalloonWorkable>(mnogo blablabla);
        +++ workChore.onBegin += VaricolouredBalloonsPatches.OnBeginGetBalloonChore;
        */
        
        private static readonly IDetouredField<Chore, Action<Chore>> ON_BEGIN = PDetours.DetourFieldLazy<Chore, Action<Chore>>("onBegin");
        private static void InjectOnBeginChoreAction(Chore chore)
        {
            if (chore != null)
            {
                ON_BEGIN.Set(chore, (Action<Chore>)Delegate.Combine(ON_BEGIN.Get(chore), new Action<Chore>(OnBeginGetBalloonChore)));
            }
        }

        private static bool TranspilerInjectOnBeginChoreAction(List<CodeInstruction> instructions)
        {
            var workChore = typeof(WorkChore<GetBalloonWorkable>).GetConstructors().FirstOrDefault();
            var inject = typeof(VaricolouredBalloonsPatches)
                .GetMethodSafe(nameof(VaricolouredBalloonsPatches.InjectOnBeginChoreAction), true, PPatchTools.AnyArguments);
            if (workChore != null && inject != null)
            {
                for (int i = 0; i < instructions.Count; i++)
                {
                    if (instructions[i].Is(OpCodes.Newobj, workChore))
                    {
                        i++;
                        if (instructions[i].IsStloc())
                        {
                            var ldloc_workChore = TranspilerUtils.GetMatchingLoadInstruction(instructions[i]);
                            instructions.Insert(++i, ldloc_workChore);
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, inject));
                            return true;
                        }
                    }
                }
            }
            return false;
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
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var createEffect = typeof(FXHelpers).GetMethodSafe(nameof(FXHelpers.CreateEffect), true, PPatchTools.AnyArguments);
                var applysymboloverrideballoonfx = typeof(VaricolouredBalloonsPatches)
                    .GetMethodSafe(nameof(VaricolouredBalloonsPatches.ApplySymbolOverrideBalloonFX), true, PPatchTools.AnyArguments);
                if (createEffect != null && applysymboloverrideballoonfx != null)
                {
                    CodeInstruction ldloc_kbac = null;
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(createEffect) && instructions[i + 1].IsStloc())
                        {
                            ldloc_kbac = TranspilerUtils.GetMatchingLoadInstruction(instructions[i + 1]);
                            continue;
                        }
                        if (ldloc_kbac != null && instructions[i].opcode == OpCodes.Ret)
                        {
                            instructions.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
                            instructions.Insert(i++, ldloc_kbac);
                            instructions.Insert(i++, new CodeInstruction(OpCodes.Call, applysymboloverrideballoonfx));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // обработка косяка в базовой игре
        // когда пришло время разэкипировки баллона, в зависимости от настроек:
        // либо уничтожаем FX-объект баллона
        // либо не трогаем его и превращаем баг в фичу
        // а также обнуляем ссылку на FX-объект в классе конфига, во избежание конфликтов.
        [HarmonyPatch(typeof(EquippableBalloonConfig), "OnUnequipBalloon")]
        private static class EquippableBalloonConfig_OnUnequipBalloon
        {
            private static void Prefix(ref BalloonFX.Instance ___fx)
            {
                ___fx = null;
            }

            private static KMonoBehaviour DestroyFX(KMonoBehaviour target)
            {
                if (VaricolouredBalloonsOptions.Instance.DestroyFXAfterEffectExpired && target != null
                    && target.TryGetComponent<VaricolouredBalloonsHelper>(out var carrier) && carrier.fx != null)
                {
                    carrier.fx.StopSM("Unequipped");
                    carrier.fx = null;
                }
                return target;
            }
            /*
                var minionAssignablesProxy = soleOwner.GetComponent<MinionAssignablesProxy>();
                if (!minionAssignablesProxy.target.IsNullOrDestroyed())
                {
            +++     DestroyFX(minionAssignablesProxy.target as KMonoBehaviour);
                    var effects = (minionAssignablesProxy.target as KMonoBehaviour).GetComponent<Effects>();
                    if (effects != null)
                        effects.Remove("HasBalloon");
                }
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var get_target = typeof(MinionAssignablesProxy)
                    .GetPropertySafe<IAssignableIdentity>(nameof(MinionAssignablesProxy.target), false)?.GetGetMethod(true);
                var typeKMonoBehaviour = typeof(KMonoBehaviour);
                var destroyFX = typeof(EquippableBalloonConfig_OnUnequipBalloon)
                    .GetMethodSafe(nameof(DestroyFX), true, PPatchTools.AnyArguments);
                if (get_target != null && typeKMonoBehaviour != null && destroyFX != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(get_target))
                        {
                            i++;
                            if (instructions[i].Is(OpCodes.Isinst, typeKMonoBehaviour))
                            {
                                instructions.Insert(++i, new CodeInstruction(OpCodes.Call, destroyFX));
                                return true;
                            }
                        }
                    }
                }
                return false;
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

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var Duration = typeof(TRAITS.JOY_REACTIONS).GetFieldSafe(nameof(TRAITS.JOY_REACTIONS.JOY_REACTION_DURATION), true);
                var FixDuration = typeof(EquippableBalloon_OnSpawn).GetMethodSafe(nameof(FixJoyReactionDuration), true, PPatchTools.AnyArguments);
                if (Duration != null && FixDuration != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].LoadsField(Duration))
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, FixDuration));
                            return true;
                        }
                    }
                }
                return false;
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
