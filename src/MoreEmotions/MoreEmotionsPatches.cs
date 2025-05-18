using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Klei.AI;
using TUNING;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace MoreEmotions
{
    using static MoreEmotionsEffects;

    internal sealed class MoreEmotionsPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(MoreEmotionsPatches));
            new POptions().RegisterOptions(this, typeof(MoreEmotionsOptions));
        }

        // todo: добавлять звуки к анимациям

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
            Utils.LoadEmbeddedAudioSheet("AudioSheets/SFXTags_Duplicants.csv");
#if DEBUG
            var path = System.IO.Path.Combine(Utils.modInfo.rootDirectory, "AudioSheets", "SFXTags_Duplicants.csv");
            if (System.IO.File.Exists(path))
                Utils.LoadAudioSheetFromFile(path);
#endif
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            new MoreMinionEmotes(Db.Get().Emotes);
            Init();
            STRINGS.PostProcess();
#if DEBUG
            // чтобы можно было тестить эмоции через MoveTo
            var choreTypes = Db.Get().ChoreTypes;
            choreTypes.MoveTo.priority = choreTypes.Hug.priority;
            choreTypes.MoveTo.explicitPriority = choreTypes.Hug.explicitPriority;
#endif
        }

        internal static bool ReactorIsOnFloor(GameObject _, Navigator.ActiveTransition transition)
        {
            return transition.end == NavType.Floor;
        }

        internal static bool ReactorIsFacingMe(GameObject reactable, GameObject reactor)
        {
            return reactable != null && reactor != null && reactable != reactor
                && reactor.TryGetComponent(out Facing facing)
                && reactable.transform.GetPosition().x < reactor.transform.GetPosition().x == facing.GetFacing();
        }

        private static bool ReactorNotCarryMe(GameObject reactable, GameObject reactor)
        {
            if (reactable.TryGetComponent(out Pickupable pickupable) && pickupable.storage != null && pickupable.storage.gameObject == reactor)
                return false;
            return true;
        }

        // пинать нарколептика
        public static GameHashes SleepDisturbedByKick = (GameHashes)Hash.SDBMLower(nameof(SleepDisturbedByKick));
        private const float STAMINA_THRESHOLD = 0.2f;

        [HarmonyPatch(typeof(SleepChore.States), nameof(SleepChore.States.InitializeStates))]
        private static class SleepChore_States_InitializeStates
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.wake_up_lazy_ass;

            private static void Postfix(SleepChore.States __instance)
            {
                var not_so_uninterruptable = __instance.CreateState("not_so_uninterruptable", __instance.sleep.uninterruptable);
                var interrupt_kick = __instance.CreateState("interrupt_kick", __instance.sleep.uninterruptable);
                var interrupt_kick_and_sleep_again = __instance.CreateState("interrupt_kick_and_sleep_again", __instance.sleep.uninterruptable);

                __instance.sleep.uninterruptable
                    .EnterTransition(not_so_uninterruptable, smi => !smi.hadPeacefulSleep)
                    .ToggleReactable(smi => new KickLazyAssReactable(smi.gameObject, smi.hadTerribleSleep));

                not_so_uninterruptable
                    .QueueAnim("working_loop", true)
                    .EventHandler(SleepDisturbedByKick, smi =>
                    {
                        if (smi.gameObject.TryGetComponent(out Schedulable schedulable)
                            && schedulable.IsAllowed(Db.Get().ScheduleBlockTypes.Sleep))
                        {
                            smi.GoTo(interrupt_kick_and_sleep_again);
                            return;
                        }
                        var stamina = Db.Get().Amounts.Stamina.Lookup(smi.gameObject);
                        if (stamina != null && stamina.value < stamina.GetMax() * STAMINA_THRESHOLD)
                        {
                            smi.GoTo(interrupt_kick_and_sleep_again);
                            return;
                        }
                        smi.GoTo(interrupt_kick);
                    });

                interrupt_kick
                    .QueueAnim("working_pst")
                    .OnAnimQueueComplete(__instance.success);

                interrupt_kick_and_sleep_again
                    .QueueAnim("interrupt_light")
                    .OnAnimQueueComplete(not_so_uninterruptable);
            }
        }

        // альтернативные анимации сна
        [HarmonyPatch(typeof(SleepChore.States), nameof(SleepChore.States.InitializeStates))]
        internal static class SleepChore_States_InitializeStates_Alternative
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.alternative_sleep_anims;

            private static SleepChore.States.State peaceful;
            private static SleepChore.States.State bad;
            private static SleepChore.States.State phospho_rufus; // для совместимости с фосфорными специями

            private static void Postfix(SleepChore.States __instance)
            {
                peaceful = __instance.CreateState(nameof(peaceful), __instance.sleep.normal);
                bad = __instance.CreateState(nameof(bad), __instance.sleep.normal);
                phospho_rufus = __instance.CreateState(nameof(peaceful), __instance.sleep.uninterruptable);

                __instance.sleep
                    .Enter(CheckPeacefulSleep);

                __instance.sleep.normal
                    .Transition(peaceful, smi => smi.hadPeacefulSleep)
                    .Transition(bad, smi => smi.hadBadSleep && smi.timeinstate > 2 * UpdateManager.SecondsPerSimTick);

                __instance.sleep.uninterruptable
                    .EnterTransition(phospho_rufus, smi => smi.hadPeacefulSleep);

                peaceful
                    .Enter(smi => smi.hadPeacefulSleep = false)
                    .QueueAnim("trans_peaceful", false)
                    .QueueAnim("peaceful_loop", true)
                    .QueueAnimOnExit("trans_working", false);

                bad
                    .Enter(smi => smi.hadBadSleep = false)
                    .QueueAnim("trans_bad", false)
                    .QueueAnim("bad_loop", true)
                    .QueueAnimOnExit("trans_bad_working", false);

                phospho_rufus
                    .Enter(smi => smi.hadPeacefulSleep = false)
                    .QueueAnim("trans_peaceful", false)
                    .QueueAnim("peaceful_loop", true)
                    .QueueAnimOnExit("trans_working", false);

                __instance.sleep.interrupt_scared
                    .Exit(CheckBadSleep);

                __instance.sleep.interrupt_movement
                    .Exit(CheckBadSleep);

                __instance.sleep.interrupt_cold
                    .Exit(CheckBadSleep);

                __instance.sleep.interrupt_noise
                    .Exit(CheckBadSleep);

                __instance.sleep.interrupt_light
                    .Exit(CheckBadSleep);
            }

            private static void CheckPeacefulSleep(SleepChore.StatesInstance smi)
            {
                var bed = smi.sm.bed.Get(smi);
                if (bed != null && bed.TryGetComponent<Building>(out _))
                {
                    smi.hadPeacefulSleep = smi.IsLoudSleeper() || smi.HasTag(GameTags.EmitsLight)
                        || (UnityEngine.Random.value < (bed.PrefabID() == LuxuryBedConfig.ID ? 0.75f : 0.25f));
                }
            }

            private static void CheckBadSleep(SleepChore.StatesInstance smi)
            {
                var bed = smi.sm.bed.Get(smi);
                if (bed != null && bed.TryGetComponent<Building>(out _))
                {
                    smi.hadBadSleep = true;
                }
            }

            // подмена PlayAnim на QueueAnim в некоторых "прерываниях" сна
            private static SleepChore.States.State PlayQueueAnim(SleepChore.States.State @this, string anim)
            {
                @this.QueueAnim(anim, false);
                return @this;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Transpile(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var PlayAnim = typeof(SleepChore.States.State).GetMethodSafe("PlayAnim", false, typeof(string));
                var QueueAnim = typeof(SleepChore_States_InitializeStates_Alternative).GetMethodSafe(nameof(PlayQueueAnim), true, PPatchTools.AnyArguments);
                if (PlayAnim != null && QueueAnim != null)
                {
                    instructions = PPatchTools.ReplaceMethodCallSafe(instructions, PlayAnim, QueueAnim).ToList();
                    return true;
                }
                return false;
            }

            internal static HashedString GetWorkPstAnim(SleepChore.StatesInstance smi)
            {
                if (!smi.IsNullOrStopped())
                {
                    if (smi.IsInsideState(peaceful))
                        return "trans_working";
                    if (smi.IsInsideState(bad))
                        return "trans_bad_working";
                    if (smi.IsInsideState(phospho_rufus))
                        return "trans_working";
                }
                return HashedString.Invalid;
            }
        }

        // поправляем анимацию завершения сна
        [HarmonyPatch(typeof(Sleepable), nameof(Sleepable.GetWorkPstAnims))]
        private static class Sleepable_GetWorkPstAnims
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.alternative_sleep_anims;

            private static void Postfix(WorkerBase worker, ref HashedString[] __result)
            {
                if (worker != null)
                {
                    var smi = worker.GetSMI<SleepChore.StatesInstance>();
                    var anim = SleepChore_States_InitializeStates_Alternative.GetWorkPstAnim(smi);
                    if (anim.IsValid)
                    {
                        var anims = new HashedString[__result.Length + 1];
                        anims[0] = anim;
                        Array.Copy(__result, 0, anims, 1, __result.Length);
                        __result = anims;
                    }
                }
            }
        }

        // альтернативные анимации нарколептика
        [HarmonyPatch(typeof(SleepChore.StatesInstance), nameof(SleepChore.StatesInstance.SetAnim))]
        private static class SleepChore_StatesInstance_SetAnim
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.alternative_narcoleptic_anims;

            private static void Postfix(SleepChore.StatesInstance __instance)
            {
                if (!__instance.sm.isInterruptable.Get(__instance) && UnityEngine.Random.value < 0.3f)
                {
                    var sleepable = __instance.sm.bed.Get<Sleepable>(__instance);
                    var sleeper = __instance.sm.sleeper.Get(__instance);
                    if (!sleepable.TryGetComponent(out Building _)
                        && sleeper.TryGetComponent(out Navigator navigator) && navigator.CurrentNavType == NavType.Floor)
                    {
                        var stamina = Db.Get().Amounts.Stamina.Lookup(sleeper);
                        if (stamina != null && stamina.value > stamina.GetMax() * STAMINA_THRESHOLD)
                        {
                            sleepable.overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_sleep_narcoleptic_kanim") };
                            __instance.hadTerribleSleep = true;
                        }
                    }
                }
            }
        }

        // а) очень хочет в сортир
        // б) обделался и был обсмеян
        [HarmonyPatch(typeof(BladderMonitor), nameof(BladderMonitor.InitializeStates))]
        private static class BladderMonitor_InitializeStates
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.full_bladder_emote || MoreEmotionsOptions.Instance.full_bladder_laugh;

            private static void Postfix(BladderMonitor __instance)
            {
                if (MoreEmotionsOptions.Instance.full_bladder_emote)
                    __instance.urgentwant.wanting.ToggleReactable(CreateSelfReactable);
                if (MoreEmotionsOptions.Instance.full_bladder_laugh)
                    __instance.urgentwant.peeing
                        .ToggleReactable(CreatePasserbyReactable)
                        .ToggleReactable(CreatePasserbyReactable)
                        .ToggleReactable(CreatePasserbyReactable)
                        .ToggleReactable(CreatePasserbyReactable)
                        .ToggleReactable(CreatePasserbyReactable); // чтобы сразу несколько дуплей могли среагировать
            }

            private static Reactable CreateSelfReactable(BladderMonitor.Instance smi)
            {
                float cooldown = 0.25f * TUNING.DUPLICANTSTATS.GetStatsFor(smi.PrefabID()).Secretions.PEE_FUSE_TIME;
                var reactable = new SelfEmoteReactable(smi.master.gameObject, "FullBladder", Db.Get().ChoreTypes.EmoteHighPriority, 0f, cooldown)
                    .SetEmote(MoreMinionEmotes.Instance.FullBladder)
                    .AddPrecondition(ReactorIsOnFloor);
                reactable.preventChoreInterruption = true;
                return reactable;
            }

            private static Reactable CreatePasserbyReactable(BladderMonitor.Instance smi)
            {
                void AddEffect(GameObject reactor)
                {
                    if (MoreEmotionsOptions.Instance.full_bladder_add_effect
                        && !smi.IsNullOrDestroyed() && !smi.gameObject.IsNullOrDestroyed()
                        && smi.gameObject.TryGetComponent(out Effects effects))
                        effects.AddOrExtend(FullBladderLaugh, true);
                }

                if (smi.IsPeeing() && smi.GetComponent<ChoreDriver>().GetCurrentChore() is PeeChore)
                {
                    var reactable = new EmoteReactable(smi.gameObject, "PeeLaugh", Db.Get().ChoreTypes.EmoteHighPriority, 9, 5)
                        .SetEmote(MoreMinionEmotes.Instance.Laugh)
                        .RegisterEmoteStepCallbacks("react", AddEffect, null)
                        .AddPrecondition(ReactorIsOnFloor);
                    reactable.preventChoreInterruption = true;
                    return reactable;
                }
                else return null;
            }
        }

        // очень голодная анимация
        [HarmonyPatch(typeof(CalorieMonitor), nameof(CalorieMonitor.InitializeStates))]
        private static class CalorieMonitor_InitializeStates
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.starvation_emote;

            private static void Postfix(CalorieMonitor __instance)
            {
                __instance.hungry.starving.ToggleReactable(CreateSelfReactable);
            }

            private static Reactable CreateSelfReactable(CalorieMonitor.Instance smi)
            {
                const float cooldown = 0.1f * Constants.SECONDS_PER_CYCLE;
                var reactable = new SelfEmoteReactable(smi.master.gameObject, "EatHand", Db.Get().ChoreTypes.EmoteHighPriority, 0f, cooldown)
                    .SetEmote(MoreMinionEmotes.Instance.EatHand)
                    .AddPrecondition(ReactorIsOnFloor);
                reactable.preventChoreInterruption = true;
                return reactable;
            }
        }

        // альтернативная стрессовая анимация обожрунов
        [HarmonyPatch]
        private static class StressEmoteChore_StatesInstance_Constructor
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.alternative_binge_eat_emote;
            private static MethodBase TargetMethod() => typeof(StressEmoteChore.StatesInstance).GetConstructors()[0];

            private static HashedString orig_emote_kanim = "anim_interrupt_binge_eat_kanim";
            private static HashedString swap_emote_kanim = "anim_out_of_reach_binge_eat_kanim";
            private static HashedString[] swap_emote_anims = { "working_pre", "working_loop", "work_pst" };

            private static void Prefix(ref HashedString emote_kanim, ref HashedString[] emote_anims)
            {
                if (emote_kanim == orig_emote_kanim && UnityEngine.Random.value < 0.35f)
                {
                    emote_kanim = swap_emote_kanim;
                    emote_anims = swap_emote_anims;
                }
            }
        }

        // вытирание рук об себя а) после умывайника б) после вытирания
        private static void CreateHandWipeChore(WorkerBase worker)
        {
            if (worker.TryGetComponent(out ChoreProvider provider))
                new EmoteChore(provider, Db.Get().ChoreTypes.EmoteHighPriority, MoreMinionEmotes.Instance.HandWipe);
        }

        [HarmonyPatch(typeof(HandSanitizer.Work), "OnCompleteWork")]
        private static class HandSanitizer_Work_OnCompleteWork
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.wet_hands_emote;

            private static void Postfix(HandSanitizer.Work __instance, WorkerBase worker)
            {
                if (UnityEngine.Random.value < 0.25f)
                {
                    var id = __instance.PrefabID();
                    if (id == WashBasinConfig.ID || id == WashSinkConfig.ID)
                        CreateHandWipeChore(worker);
                }
            }
        }

        [HarmonyPatch(typeof(Moppable), "OnStopWork")]
        private static class Moppable_OnCompleteWork
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.wet_hands_emote;

            private static void Postfix(WorkerBase worker)
            {
                if (UnityEngine.Random.value < 0.25f)
                    CreateHandWipeChore(worker);
            }
        }

        // скорьбь а) возле трупа б) возле могилы
        [HarmonyPatch(typeof(DeathMonitor), nameof(DeathMonitor.InitializeStates))]
        private static class DeathMonitor_InitializeStates
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.saw_corpse_emote;

            private static void Postfix(DeathMonitor __instance)
            {
                __instance.dead
                    .ToggleReactable(CreatePasserbyReactable)
                    .ToggleReactable(CreatePasserbyReactable)
                    .ToggleReactable(CreatePasserbyReactable)
                    .ToggleReactable(CreatePasserbyReactable); // чтобы сразу несколько дуплей могли среагировать
            }

            private static void AddEffect(GameObject reactor)
            {
                if (MoreEmotionsOptions.Instance.saw_corpse_add_effect
                    && !reactor.IsNullOrDestroyed() && reactor.TryGetComponent(out Effects effects))
                    effects.Add(SawCorpse, true);
            }

            private static Reactable CreatePasserbyReactable(DeathMonitor.Instance smi)
            {
                var reactable = new EmoteReactable(
                        gameObject: smi.gameObject,
                        id: "Saw_Corpse",
                        chore_type: Db.Get().ChoreTypes.EmoteHighPriority,
                        range_width: 7,
                        range_height: 5,
                        globalCooldown: 0f,
                        localCooldown: 30f)
                    .SetEmote(MoreMinionEmotes.Instance.PutOff)
                    .RegisterEmoteStepCallbacks("putoff_pre", null, AddEffect)
                    .AddPrecondition(ReactorIsOnFloor)
                    .AddPrecondition((reactor, transition) => ReactorNotCarryMe(smi.gameObject, reactor));
                reactable.preventChoreInterruption = true;
                return reactable;
            }
        }

        [HarmonyPatch(typeof(Grave.States), nameof(Grave.States.InitializeStates))]
        private static class Grave_States_InitializeStates
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.respect_grave_emote;

            private static void Postfix(Grave.States __instance)
            {
                __instance.full.ToggleReactable(CreatePasserbyReactable);
            }

            private static Reactable CreatePasserbyReactable(Grave.StatesInstance smi)
            {
                var reactable = new RespectGraveReactable(smi.gameObject)
                    .AddPrecondition(ReactorIsOnFloor);
                reactable.preventChoreInterruption = true;
                return reactable;
            }
        }

        // успокаивание стрессующего
        [HarmonyPatch(typeof(StressBehaviourMonitor), nameof(StressBehaviourMonitor.InitializeStates))]
        private static class StressBehaviourMonitor_InitializeStates
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.stress_cheering;

            private static void Postfix(StressBehaviourMonitor __instance)
            {
                __instance.stressed.tierOne.ToggleStateMachine(smi => new StressCheeringMonitor.Instance(smi.master));
            }
        }

#if DEBUG
        // для тесту, приветствие чащее
        [HarmonyPatch(typeof(DupeGreetingManager), "OnPrefabInit")]
        private static class DupeGreetingManager_OnPrefabInit
        {
            private static void Postfix()
            {
                var tuning = DupeGreetingManager.Tuning.Get();
                tuning.cyclesBeforeFirstGreeting = 0;
                tuning.greetingDelayMultiplier = 0.04f;
            }
        }
#endif

        // приветствие кулаками и пятюней
        private static HashedString ReactableId = "NavigatorPassingGreeting";

        [HarmonyPatch(typeof(DupeGreetingManager), nameof(DupeGreetingManager.BeginNewGreeting))]
        private static class DupeGreetingManager_BeginNewGreeting
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.double_greeting;
            private const float LocalCooldown = 20f;
            private static List<Emote> new_emotes;

            private static bool CanReact(MinionIdentity minion, float time)
            {
                var smi = minion.GetSMI<ReactionMonitor.Instance>();
                if (smi == null)
                    return false;
                if ((smi.lastReactTimes.TryGetValue(ReactableId, out float num) && num == smi.lastReaction) || time - num < LocalCooldown)
                    return false;
                return true;
            }

            private static Reactable GetReactable(MinionIdentity minion, Emote emote, Action<GameObject> onstart_cb, Reactable.ReactablePrecondition precondition)
            {
                var reactable = new SelfEmoteReactable(minion.gameObject, ReactableId, Db.Get().ChoreTypes.Emote, 1000f, LocalCooldown)
                    .SetEmote(emote).SetThought(Db.Get().Thoughts.Chatty)
                    .RegisterEmoteStepCallbacks("react_l", onstart_cb, null)
                    //.RegisterEmoteStepCallbacks("react_l", DEBUG_PAUSE, null)
                    .AddPrecondition(precondition);
                return reactable;
            }
#if DEBUG
            private static void DEBUG_PAUSE(GameObject go)
            {
                if (!SpeedControlScreen.Instance.IsPaused)
                    SpeedControlScreen.Instance.TogglePause(false);
            }
#endif
            private static bool Prefix(MinionIdentity minion_a, MinionIdentity minion_b, int cell, DupeGreetingManager __instance)
            {
                // затычка чтобы не создавались 100500 приветствий когда множество дуплей тусуются в одной области
                for (int j = 0; j < __instance.activeSetups.Count; j++)
                {
                    var setup = __instance.activeSetups[j];
                    if (setup.A.minion == minion_a || setup.A.minion == minion_b || setup.B.minion == minion_a || setup.B.minion == minion_b)
                        return false;
                }
                if (DupeGreetingManager.emotes == null)
                    return true;
                if (new_emotes == null)
                {
                    var fist_bump = MoreMinionEmotes.Instance.FistBump;
                    var high_five = MoreMinionEmotes.Instance.HighFive;
                    new_emotes = new List<Emote> { fist_bump, high_five, fist_bump, high_five }; // удваяем шансы, потому что см ниже
                }
                // первично, шансы обычного или нашего приветствия пропорцилнальны количеству возможных приветствий
                int m = DupeGreetingManager.emotes.Count;
                if (MoreEmotionsOptions.Instance.moonwalk_greeting)
                    m++;
                int n = new_emotes.Count;
                int i = UnityEngine.Random.Range(0, m + n);
                if (i >= n)
                    return true;
                // оба дуплика должны быть готовы совершить приветствие, иначе откатываемся к обычному приветствию
                float time = GameClock.Instance.GetTime();
                if (!CanReact(minion_a, time) || !CanReact(minion_b, time))
                    return true;
                var emote = new_emotes[i];
                // регистрируем приветствие
                var greetingSetup = new DupeGreetingManager.GreetingSetup();
                greetingSetup.cell = cell;
                greetingSetup.A = new DupeGreetingManager.GreetingUnit(minion_a, GetReactable(minion_a, emote, __instance.BeginReacting, IsCloseEnough));
                greetingSetup.B = new DupeGreetingManager.GreetingUnit(minion_b, GetReactable(minion_b, emote, __instance.BeginReacting, IsCloseEnough));
                __instance.activeSetups.Add(greetingSetup);
                bool IsCloseEnough(GameObject reactor, Navigator.ActiveTransition _)
                {
                    // требуемое расстояние
                    // если другой дуп движется к приветствующему - 0-1 клетки, наоборот - 1-2 клетки
                    if (minion_a == null || minion_b == null)
                        return false;
                    var opponent = (minion_a.gameObject == reactor) ? minion_b.gameObject : minion_a.gameObject;
                    var offset = opponent.transform.GetPosition().x - reactor.transform.GetPosition().x;
                    bool is_left = offset < 0f;
                    offset = Mathf.Abs(offset);
                    if (is_left ^ opponent.GetComponent<Facing>().GetFacing())
                        offset -= 1f;
                    return offset >= 0f && offset < 1f;
                }
                return false;
            }
        }

        // танцуюющеее приветсвие
        [HarmonyPatch(typeof(DupeGreetingManager), nameof(DupeGreetingManager.GetReactable))]
        private static class DupeGreetingManager_GetReactable
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.moonwalk_greeting;
            private static bool Prefix(MinionIdentity minion, DupeGreetingManager __instance, ref Reactable __result)
            {
                if (DupeGreetingManager.emotes == null)
                    return true;
                int m = DupeGreetingManager.emotes.Count;
                int i = UnityEngine.Random.Range(0, m + 1);
                if (i >= 1)
                    return true;
                __result = new SelfEmoteReactable(minion.gameObject, ReactableId, Db.Get().ChoreTypes.Emote, 1000f)
                    .SetEmote(MoreMinionEmotes.Instance.MoonWalk).SetThought(Db.Get().Thoughts.Chatty)
                    .RegisterEmoteStepCallbacks("floor_floor_moonwalk_1_0_pre", __instance.BeginReacting, null);
                return false;
            }
        }

        // возвращаем анимацию заражённой еды
        // но прикрутим её также к другим возможным источникам - типа коффее
        private static GermExposureMonitor.State react_contaminated_food;

        [HarmonyPatch(typeof(GermExposureMonitor), nameof(GermExposureMonitor.InitializeStates))]
        private static class GermExposureMonitor_InitializeStates
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.contaminated_food_emote;
            private static void Postfix(GermExposureMonitor __instance)
            {
                react_contaminated_food = __instance.CreateState(nameof(react_contaminated_food), __instance.root);
                react_contaminated_food.ToggleChore(CreateEmoteChore, __instance.root);
            }

            private static Chore CreateEmoteChore(GermExposureMonitor.Instance smi)
            {
                return new EmoteChore(smi.master, Db.Get().ChoreTypes.EmoteHighPriority, Db.Get().Emotes.Minion.FoodPoisoning,
                    KAnim.PlayMode.Once, flip_x: smi.GetComponent<Facing>().GetFacing());
            }
        }

        [HarmonyPatch(typeof(GermExposureMonitor.Instance), nameof(GermExposureMonitor.Instance.InjectDisease))]
        private static class GermExposureMonitor_Instance_InjectDisease
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.contaminated_food_emote;
            private static void Postfix(GermExposureMonitor.Instance __instance, Disease disease, int count, Sickness.InfectionVector vector)
            {
                if (vector == Sickness.InfectionVector.Digestion && __instance.IsInsideState(__instance.sm.root))
                {
                    foreach (var exposureType in GERM_EXPOSURE.TYPES)
                    {
                        if (disease.id == exposureType.germ_id && count > exposureType.exposure_threshold
                            && __instance.IsExposureValidForTraits(exposureType))
                        {
                            __instance.GoTo(react_contaminated_food);
                            break;
                        }
                    }
                }
            }
        }

        // контузия при падении
        [HarmonyPatch(typeof(FallMonitor), nameof(FallMonitor.InitializeStates))]
        private static class FallMonitor_InitializeStates
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.fall_contusion_emote;

            private static FallMonitor.IntParameter fall_cell;
            private static FallMonitor.IntParameter fall_height;

            private static void Postfix(FallMonitor __instance)
            {
                fall_cell = __instance.AddParameter(nameof(fall_cell), new FallMonitor.IntParameter());
                fall_height = __instance.AddParameter(nameof(fall_height), new FallMonitor.IntParameter());

                __instance.falling
                    .Enter(smi => fall_cell.Set(Grid.PosToCell(smi.transform.GetPosition()), smi))
                    .Exit(smi =>
                    {
                        var a = Grid.PosToCell(smi.transform.GetPosition());
                        var b = fall_cell.Get(smi);
                        var y = (Grid.IsValidCell(a) && Grid.IsValidCell(b)) ? Grid.GetOffset(a, b).y : 0;
                        fall_height.Set(y, smi);
                    });

                if (__instance.landfloor.updateActions != null)
                    __instance.landfloor.updateActions.Clear(); // убрать GoTo(standing)

                __instance.landfloor
                    .ToggleBrain(nameof(__instance.landfloor))
                    .PlayAnim(smi => ShouldContuze(smi) ? "fall_hard_pst" : "fall_pst")
                    .OnAnimQueueComplete(__instance.standing);

                if (MoreEmotionsOptions.Instance.fall_contusion_add_effect)
                    __instance.landfloor.Exit(Contuze);
            }

            private static bool ShouldContuze(FallMonitor.Instance smi)
            {
                return smi.shouldPlayEmotes && fall_height.Get(smi) >= CONTUSION_HEIGHT;
            }

            private static void Contuze(FallMonitor.Instance smi)
            {
                if (ShouldContuze(smi) && smi.gameObject.TryGetComponent(out Effects effects))
                {
                    var effect = effects.AddOrExtend(Contusion, true);
                    effect.timeRemaining += (fall_height.Get(smi) - CONTUSION_HEIGHT) * CONTUSION_DURATION / CONTUSION_HEIGHT;
                }
            }
        }

        [HarmonyPatch(typeof(BaseMinionConfig), nameof(BaseMinionConfig.BaseRationalAiStateMachines))]
        private static class BaseMinionConfig_BaseRationalAiStateMachines
        {
            private static bool Prepare() => MoreEmotionsOptions.Instance.fall_contusion_emote && MoreEmotionsOptions.Instance.fall_contusion_add_effect;

            private static void Postfix(ref Func<RationalAi.Instance, StateMachine.Instance>[] __result)
            {
                __result = __result.Append(smi => new ContusionFX.Instance(smi.master));
            }
        }
    }
}
