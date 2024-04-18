using System;
using System.Collections.Generic;
using System.Reflection;
using Database;
using Klei.AI;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;

namespace MoreEmotions
{
    internal sealed class MoreEmotionsPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Utils.LogModVersion();
            base.OnLoad(harmony);
        }

        // todo: опции для включения и шансов

        // todo: добавить звуки к анимациям (сложна)
        // * терпёжъ bladder
        // * объсмеяние laugh
        // * сожалениё putoff
        // * приветствия fistbump и highfive

        [HarmonyPatch(typeof(Emotes), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(ResourceSet) })]
        private static class Emotes_Constructor
        {
            private static void Postfix(Emotes __instance)
            {
                new MoreMinionEmotes(__instance);
            }
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

        // а) очень хочет в сортир
        // б) обделался и был обсмеян
        [HarmonyPatch(typeof(BladderMonitor), nameof(BladderMonitor.InitializeStates))]
        private static class BladderMonitor_InitializeStates
        {
            private static void Postfix(BladderMonitor __instance)
            {
                __instance.urgentwant.wanting.ToggleReactable(CreateSelfReactable);
                __instance.urgentwant.peeing.ToggleReactable(CreatePasserbyReactable);
            }

            private static Reactable CreateSelfReactable(BladderMonitor.Instance smi)
            {
                const float cooldown = TUNING.DUPLICANTSTATS.PEE_FUSE_TIME / 4f;
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
                    // todo: добавление эффекта
                    /*
                    if (!smi.IsNullOrDestroyed() && !smi.gameObject.IsNullOrDestroyed() && smi.gameObject.TryGetComponent(out Effects effects))
                        effects.Add();
                    */
                }

                if (smi.IsPeeing() && smi.GetComponent<ChoreDriver>().GetCurrentChore() is PeeChore)
                {
                    var reactable = new EmoteReactable(smi.gameObject, "PeeLaugh", Db.Get().ChoreTypes.EmoteHighPriority, 7, 5)
                        .SetEmote(MoreMinionEmotes.Instance.Laugh)
                        .RegisterEmoteStepCallbacks("react", AddEffect, null)
                        .AddPrecondition(ReactorIsOnFloor)
                        .AddPrecondition((reactor, transition) => ReactorIsFacingMe(smi.gameObject, reactor));
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
            private static void Postfix(CalorieMonitor __instance)
            {
                __instance.hungry.starving.ToggleReactable(CreateSelfReactable);
            }

            private static Reactable CreateSelfReactable(CalorieMonitor.Instance smi)
            {
                const float cooldown = 60f;
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
        private static void CreateHandWipeChore(Worker worker)
        {
            if (worker.TryGetComponent(out ChoreProvider provider))
                new EmoteChore(provider, Db.Get().ChoreTypes.EmoteHighPriority, MoreMinionEmotes.Instance.HandWipe);
        }

        [HarmonyPatch(typeof(HandSanitizer.Work), "OnCompleteWork")]
        private static class HandSanitizer_Work_OnCompleteWork
        {
            private static void Postfix(HandSanitizer.Work __instance, Worker worker)
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
            private static void Postfix(Worker worker)
            {
                if (UnityEngine.Random.value < 0.25f)
                    CreateHandWipeChore(worker);
            }
        }

        // скорьбь а) возле трупа б) возле могилы
        // todo: добавление эффектов
        [HarmonyPatch(typeof(DeathMonitor), nameof(DeathMonitor.InitializeStates))]
        private static class DeathMonitor_InitializeStates
        {
            private static void Postfix(DeathMonitor __instance)
            {
                __instance.dead.ToggleReactable(CreatePasserbyReactable);
            }

            private static Reactable CreatePasserbyReactable(DeathMonitor.Instance smi)
            {
                var reactable = new EmoteReactable(smi.gameObject, "Respect_Corpse", Db.Get().ChoreTypes.EmoteHighPriority, 7, 5)
                    .SetEmote(MoreMinionEmotes.Instance.PutOff)
                    .AddPrecondition(ReactorIsOnFloor)
                    .AddPrecondition((reactor, transition) => ReactorIsFacingMe(smi.gameObject, reactor))
                    .AddPrecondition((reactor, transition) => ReactorNotCarryMe(smi.gameObject, reactor));
                reactable.preventChoreInterruption = true;
                return reactable;
            }
        }

        [HarmonyPatch(typeof(Grave.States), nameof(Grave.States.InitializeStates))]
        private static class Grave_States_InitializeStates
        {
            private static void Postfix(Grave.States __instance)
            {
                __instance.full.ToggleReactable(CreatePasserbyReactable);
            }

            private static Reactable CreatePasserbyReactable(Grave.StatesInstance smi)
            {
                var reactable = new RespectGraveReactable(smi.gameObject)
                    .AddPrecondition(ReactorIsOnFloor)
                    .AddPrecondition((reactor, transition) => ReactorIsFacingMe(smi.gameObject, reactor));
                reactable.preventChoreInterruption = true;
                reactable.Begin(null); // для выставления таймаута
                return reactable;
            }
        }

        // успокаивание стрессующего
        [HarmonyPatch(typeof(StressBehaviourMonitor), nameof(StressBehaviourMonitor.InitializeStates))]
        private static class StressBehaviourMonitor_InitializeStates
        {
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
        [HarmonyPatch(typeof(DupeGreetingManager), nameof(DupeGreetingManager.BeginNewGreeting))]
        private static class DupeGreetingManager_BeginNewGreeting
        {
            private const float LocalCooldown = 20f;
            private static HashedString ReactableId = "NavigatorPassingGreeting";
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
                var reactable = new SelfEmoteReactable(minion.gameObject, ReactableId, Db.Get().ChoreTypes.Emote, 1000f, LocalCooldown, float.PositiveInfinity)
                    .SetEmote(emote).SetThought(Db.Get().Thoughts.Chatty)
                    .RegisterEmoteStepCallbacks("react_l", onstart_cb, null)
                    //.RegisterEmoteStepCallbacks("react_l", DEBUG_PAUSE, null)
                    .AddPrecondition(precondition);
                return reactable;
            }

            private static void DEBUG_PAUSE(GameObject go)
            {
                if (!SpeedControlScreen.Instance.IsPaused)
                    SpeedControlScreen.Instance.TogglePause(false);
            }

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
    }
}
