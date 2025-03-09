using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using STRINGS;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;

namespace SupplyToClosest
{
    internal sealed class SupplyToClosestPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(SupplyToClosestPatches));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            harmony.PatchAll();
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            try
            {
                var method = PPatchTools.GetTypeSafe("PeterHan.FastTrack.PathPatches.PathCacher", "FastTrack")?
                    .GetMethodSafe("SetValid", true, typeof(PathProber), typeof(bool));
                if (method != null)
                {
                    var options_type = PPatchTools.GetTypeSafe("PeterHan.FastTrack.FastTrackOptions", "FastTrack");
                    if (options_type != null)
                    {
                        var options_instance = Traverse.Create(options_type).Property("Instance").GetValue();
                        if (options_instance != null && Traverse.Create(options_instance).Property<bool>("CachePaths").Value)
                        {
                            FastTrack_PathCacher_SetValid = method.CreateDelegate<Action<PathProber, bool>>(null);
                            return;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utils.LogExcWarn(e);
            }
            FastTrack_PathCacher_SetValid = null;
        }
        private static Action<PathProber, bool> FastTrack_PathCacher_SetValid;

        private static FetchAreaChore.States.BoolParameter do_search_better; // ищем лучшую чору прям сейчас
        private static FetchAreaChore.States.BoolParameter already_searched; // уже искали, не нужно искать ещё раз
        private static FetchAreaChore.States.IntParameter cashed_path_cost;  // цена пути до нынешней чоры

        [HarmonyPatch(typeof(FetchAreaChore.States), nameof(FetchAreaChore.States.InitializeStates))]
        private static class FetchAreaChore_States_InitializeStates
        {
            private static void Postfix(FetchAreaChore.States __instance)
            {
                do_search_better = __instance.AddParameter(nameof(do_search_better), new FetchAreaChore.States.BoolParameter(false));
                already_searched = __instance.AddParameter(nameof(already_searched), new FetchAreaChore.States.BoolParameter(false));
                cashed_path_cost = __instance.AddParameter(nameof(cashed_path_cost), new FetchAreaChore.States.IntParameter());

                __instance.fetching
                    .EventHandler(GameHashes.DestinationReached, FindBetterFetchChore);
            }

            private static void FindBetterFetchChore(FetchAreaChore.StatesInstance smi)
            {
                if (!smi.pickingup)
                    return;
                if (already_searched.Get(smi))
                    return;
                already_searched.Set(true, smi);
                var rootChore = smi.rootChore;
                if (rootChore == null || rootChore.destination == null)
                    return;
                var consumerState = smi.rootContext.consumerState;
                if (consumerState == null || consumerState.hasSolidTransferArm || consumerState.consumer.IsWithinReach(rootChore.destination))
                    return;
                var target = smi.master.GetFetchTarget;
                if (target == null || !target.TryGetComponent(out Pickupable pickupable))
                    return;
                // ищем лючшую чору, мимикрируем под
                // Brain.UpdateBrain -> ChoreConsumer.FindNextChore -> GlobalChoreProvider.CollectChores
                FastTrack_PathCacher_SetValid?.Invoke(consumerState.navigator.PathProber, false);
                consumerState.navigator.UpdateProbe(true);
                if (!consumerState.consumer.GetNavigationCost(rootChore.destination, out int root_cost))
                    return;
                cashed_path_cost.Set(root_cost, smi);
                GlobalChoreProvider_Patch.UpdateFetchesWithoutClearables(GlobalChoreProvider.Instance, consumerState.navigator.PathProber);
                consumerState.Refresh();
                // поскольку выбранный для переноса кусок уже имеет резервацию (как минимум этим же дупликом)
                // если этот кусок достаточно мал, то его UnreservedAmount == 0ф
                // и FetchManager.IsFetchablePickup будет кидать False
                // перед поиском отменим резервацию
                var reservation = smi.reservations[0];
                reservation.Cleanup();
                var succeeded = ListPool<Chore.Precondition.Context, ChoreConsumer>.Allocate();
                var failed = ListPool<Chore.Precondition.Context, ChoreConsumer>.Allocate();
                do_search_better.Set(true, smi);
                GlobalChoreProvider_Patch.CollectOnlyFetchChores(GlobalChoreProvider.Instance, consumerState, succeeded, failed);
                // вызов выше не собирает чоры, которые уже кем то выполняются
                // todo: возможно, всё же стоит выбрать чоры которые выполняет этот дупель, с целью сравнения
                /*
                foreach (var sub_chore in smi.chores)
                    sub_chore.CollectChoresFromGlobalChoreProvider(consumerState, succeeded, failed, false);
                */
                do_search_better.Set(false, smi);
                succeeded.Sort();
                Chore.Precondition.Context candidat_context = default;
                bool found = false;
#if DEBUG
                const string log = "#{0}\tChoreType: {1}\tMasterPriority: {2}\tPriority: {3} \tCost: {4}\tGameObject: {5}";
                Debug.LogFormat("{0} is looking for a better FetchChore", UI.StripLinkFormatting(smi.gameObject.GetProperName()));
                var chore_type_priority = Game.Instance.advancedPersonalPriorities ?
                    smi.master.choreType.explicitPriority : smi.master.choreType.priority;
                Debug.LogFormat(log, "Now:", smi.master.choreType.Id,
                    smi.master.masterPriority.priority_value, chore_type_priority,
                    root_cost, UI.StripLinkFormatting(rootChore.gameObject.GetProperName()));
                Debug.LogFormat("Candidates: {0}", succeeded.Count);
                for (int i = succeeded.Count - 1; i >= 0; i--)
                {
                    candidat_context = succeeded[i];
                    if (candidat_context.IsSuccess() && candidat_context.chore is FetchChore)
                    {
                        Debug.LogFormat(log, i, candidat_context.chore.choreType.Id,
                            candidat_context.masterPriority.priority_value, candidat_context.priority,
                            candidat_context.cost, UI.StripLinkFormatting(candidat_context.chore.gameObject.GetProperName()));
                    }
                }
                Debug.LogFormat("Deputates: {0}", failed.Count);
                for (int i = failed.Count - 1; i >= 0; i--)
                {
                    candidat_context = failed[i];
                    if (!candidat_context.IsSuccess() && candidat_context.chore is FetchChore)
                    {
                        Debug.LogFormat(log, i, candidat_context.chore.choreType.Id,
                            candidat_context.masterPriority.priority_value, candidat_context.priority,
                            candidat_context.cost, UI.StripLinkFormatting(candidat_context.chore.gameObject.GetProperName()));
                        Debug.Log(candidat_context.chore.GetPreconditions()[candidat_context.failedPreconditionId].condition.id);
                    }
                }
#endif
                for (int j = succeeded.Count - 1; j >= 0; j--)
                {
                    candidat_context = succeeded[j];
                    if (candidat_context.IsSuccess() && candidat_context.chore is FetchChore candidat_chore)
                    {
                        if (candidat_chore == rootChore || smi.SameDestination(candidat_chore))
                            break;
                        if (candidat_context.cost < root_cost)
                        {
                            found = true;
                            break;
                        }
                    }
                }
                succeeded.Recycle();
                failed.Recycle();
#if DEBUG
                Debug.LogFormat("Found Better : {0}", found);
#endif
                if (found && candidat_context.chore.IsValid())
                {
#if DEBUG
                    Debug.LogFormat(log, "New:", candidat_context.chore.choreType.Id,
                        candidat_context.masterPriority.priority_value, candidat_context.priority,
                        candidat_context.cost, UI.StripLinkFormatting(candidat_context.chore.gameObject.GetProperName()));
#endif
                    // заменяем чору с некоторыми манипуляциями чтобы не обновлять мозг и выключить поиск на следующей итерации
                    bool prioritize = consumerState.consumer.prioritizeBrainIfNoChore;
                    consumerState.consumer.prioritizeBrainIfNoChore = false;
                    consumerState.choreDriver.StopChore();
                    consumerState.consumer.prioritizeBrainIfNoChore = prioritize;
                    candidat_context.chore.PrepareChore(ref candidat_context);
                    if (candidat_context.chore is FetchAreaChore new_chore)
                        already_searched.Set(true, new_chore.smi);
                    consumerState.choreDriver.SetChore(candidat_context);
                }
                else
                {
                    // нафсякий вернём резервацию как было
                    reservation.handle = reservation.pickupable.Reserve(nameof(FetchAreaChore), consumerState.prefabid.InstanceID, reservation.amount);
                    smi.reservations[0] = reservation;
                }
            }
        }

        [HarmonyPatch]
        private static class FetchChore_Constructor
        {
            private static MethodBase TargetMethod()
            {
                // меняем прекондицию для флудо чтобы выполнялась гарантированно после FetchChore.IsFetchTargetAvailable
                // как оно вообще работает с параметрами по умолчанию ?
                var flydo_can = FetchChore.CanFetchDroneComplete;
                flydo_can.sortOrder = 1;
                flydo_can.canExecuteOnAnyThread = false;
                Traverse.Create<FetchChore>().Field<Chore.Precondition>(nameof(FetchChore.CanFetchDroneComplete)).Value = flydo_can;
                return typeof(FetchChore).GetConstructors()[0];
            }

            private static void Postfix(FetchChore __instance)
            {
                __instance.AddPrecondition(FindBetterChore_IsAttemptingOverride);
                __instance.AddPrecondition(FindBetterChore_IsMoreSatisfying);
                __instance.AddPrecondition(FindBetterChore_IsFetchablePickup);
                __instance.AddPrecondition(FindBetterChore_IsCloseEnough);
            }

            /*
            sortOrder следует подобрать так чтобы прекондиции выполнялись в таком порядке:
            -3  FindBetterChore_IsAttemptingOverride
            -2  ChorePreconditions.IsMoreSatisfyingEarly | FindBetterChore_IsMoreSatisfying
            -1  FindBetterChore_IsFetchablePickup
            0   FetchChore.IsFetchTargetAvailable
            1   FetchChore.CanFetchDroneComplete
            1   FindBetterChore_IsCloseEnough

            todo: нужно ли проверять близость здесь ? без проверки дупли смогут относить на новые более приоритетные цели
            */

            // если идёт поиск, выставляем isAttemptingOverride
            // что приведет к пропуску IsMoreSatisfyingEarly/Later и еще нескольких прекондицый
            private static Chore.Precondition FindBetterChore_IsAttemptingOverride = new Chore.Precondition
            {
                id = nameof(FindBetterChore_IsAttemptingOverride),
                description = DUPLICANTS.CHORES.PRECONDITIONS.IS_MORE_SATISFYING,
                sortOrder = -3,
                canExecuteOnAnyThread = true,
                fn = delegate (ref Chore.Precondition.Context context, object data)
                {
                    if (context.consumerState.choreDriver.GetCurrentChore() is FetchAreaChore areaChore
                        && do_search_better.Get(areaChore.smi))
                    {
                        context.isAttemptingOverride = true;
                        context.choreTypeForPermission = areaChore.smi.rootContext.choreTypeForPermission;
                    }
                    return true;
                }
            };

            // доп. проверка поскольку полагаться только на isAttemptingOverride нельзя
            // поскольку он может быть выставлен из других мест
            private static bool IsSearchBetterChore(ref Chore.Precondition.Context context, out FetchAreaChore currentChore)
            {
                if (context.isAttemptingOverride
                    && context.consumerState.choreDriver.GetCurrentChore() is FetchAreaChore areaChore
                    && do_search_better.Get(areaChore.smi))
                {
                    currentChore = areaChore;
                    return true;
                }
                else
                {
                    currentChore = null;
                    return false;
                }
            }

            // отсеиваем менее приоритетные чоры
            private static Chore.Precondition FindBetterChore_IsMoreSatisfying = new Chore.Precondition
            {
                id = nameof(FindBetterChore_IsMoreSatisfying),
                description = DUPLICANTS.CHORES.PRECONDITIONS.IS_MORE_SATISFYING,
                sortOrder = -2,
                canExecuteOnAnyThread = true,
                fn = delegate (ref Chore.Precondition.Context context, object data)
                {
                    // скопипизжено из ChorePreconditions.IsMoreSatisfyingEarly с парой небольших но важных отличий
                    // считываем значения приоритетов из FetchChore а не FetchAreaChore
                    // возвращаем true если в конечном итоге величины равны
                    if (IsSearchBetterChore(ref context, out var areaChore))
                    {
                        var currentChore = areaChore.smi.rootChore;
#if DEBUG
                        Debug.LogFormat("{0}\tDriver: {1}\toverrideTarget: {2}",
                            UI.StripLinkFormatting(context.chore.gameObject.name),
                            context.chore.driver, context.chore.overrideTarget);
                        const string log = "{0}\t{1}\t<==>\t{2}";
                        Debug.LogFormat(log, "priority_class", context.masterPriority.priority_class, currentChore.masterPriority.priority_class);
                        Debug.LogFormat(log, "PersonalPriority", context.personalPriority,
                            context.consumerState.consumer.GetPersonalPriority(currentChore.choreType));
                        Debug.LogFormat(log, "masterPriority", context.masterPriority.priority_value, currentChore.masterPriority.priority_value);
                        Debug.LogFormat(log, "choreType.priority", context.priority, (Game.Instance.advancedPersonalPriorities ?
                            currentChore.choreType.explicitPriority : currentChore.choreType.priority));
#endif
                        if (context.masterPriority.priority_class != currentChore.masterPriority.priority_class)
                            return context.masterPriority.priority_class > currentChore.masterPriority.priority_class;
                        if (context.consumerState.consumer != null && context.personalPriority != context.consumerState.consumer.GetPersonalPriority(currentChore.choreType))
                            return context.personalPriority > context.consumerState.consumer.GetPersonalPriority(currentChore.choreType);
                        if (context.masterPriority.priority_value != currentChore.masterPriority.priority_value)
                            return context.masterPriority.priority_value > currentChore.masterPriority.priority_value;
                        // todo: оставить как было или правильно считать explicitPriority ?
                        //return context.priority >= currentChore.choreType.priority;
                        return context.priority >= (Game.Instance.advancedPersonalPriorities ?
                            currentChore.choreType.explicitPriority : currentChore.choreType.priority);
                    }
                    return true;
                }
            };

            // отсеиваем чоры неподходящие для хранения уже выбранного куска
            private static Chore.Precondition FindBetterChore_IsFetchablePickup = new Chore.Precondition()
            {
                id = nameof(FindBetterChore_IsFetchablePickup),
                description = DUPLICANTS.CHORES.PRECONDITIONS.IS_FETCH_TARGET_AVAILABLE,
                sortOrder = -1,
                canExecuteOnAnyThread = false,
                fn = delegate (ref Chore.Precondition.Context context, object data)
                {
                    if (IsSearchBetterChore(ref context, out var areaChore))
                    {
                        if (context.chore is FetchChore candidat_chore)
                        {
                            var target = areaChore.GetFetchTarget;
                            if (target != null && target.TryGetComponent(out Pickupable pickupable)
                               && FetchManager.IsFetchablePickup(pickupable, candidat_chore, candidat_chore.destination))
                            {
                                context.data = pickupable;
                                return true;
                            }
                        }
                        return false;
                    }
                    return true;
                }
            };

            // отсеиваем чоры, более далёкие чем изначальная
            private static Chore.Precondition FindBetterChore_IsCloseEnough = new Chore.Precondition()
            {
                id = nameof(FindBetterChore_IsCloseEnough),
                description = DUPLICANTS.CHORES.PRECONDITIONS.IS_FETCH_TARGET_AVAILABLE,
                sortOrder = 1,
                canExecuteOnAnyThread = false,
                fn = delegate (ref Chore.Precondition.Context context, object data)
                {
                    if (IsSearchBetterChore(ref context, out var areaChore))
                    {
                        return context.cost <= cashed_path_cost.Get(areaChore.smi);
                    }
                    return true;
                }
            };
        }

        // для оптимизации и уменьшения нагрузки
        [HarmonyPatch]
        private static class GlobalChoreProvider_Patch
        {
            // копия GlobalChoreProvider.CollectChores из которой вырезаны вызовы в начале
            // base.CollectChores - чтобы не дёргать обычные чоры которые не FetchChore
            // ClearableManager.CollectChores - чтобы не дёргать чоры предназначенные для переноса Clearable
            [HarmonyReversePatch(HarmonyReversePatchType.Original)]
            [HarmonyPatch(typeof(GlobalChoreProvider), nameof(GlobalChoreProvider.CollectChores))]
            internal static void CollectOnlyFetchChores(GlobalChoreProvider provider, ChoreConsumerState consumer_state, List<Chore.Precondition.Context> succeeded, List<Chore.Precondition.Context> failed_contexts)
            {
#pragma warning disable CS8321
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) =>
                    TranspilerUtils.Transpile(instructions, original, RemoveCollectChores);
#pragma warning restore CS8321
                provider.CollectChores(consumer_state, succeeded, failed_contexts);
            }

            private static bool RemoveCollectChores(List<CodeInstruction> instructions)
            {
                var method = typeof(ClearableManager).GetMethodSafe(nameof(ClearableManager.CollectChores), false,
                    typeof(List<GlobalChoreProvider.Fetch>), typeof(ChoreConsumerState),
                    typeof(List<Chore.Precondition.Context>), typeof(List<Chore.Precondition.Context>));
                if (method != null)
                {
                    int i = instructions.FindIndex(inst => inst.Calls(method));
                    if (i != -1)
                    {
                        instructions.RemoveRange(0, i + 1);
                        return true;
                    }
                }
                return false;
            }

            // копия GlobalChoreProvider.UpdateFetches из которой вырезаны вызовы в конце
            // clearableManager.CollectAndSortClearables - так как мы всё равно не дёргаем Clearable
            [HarmonyReversePatch(HarmonyReversePatchType.Original)]
            [HarmonyPatch(typeof(GlobalChoreProvider), nameof(GlobalChoreProvider.UpdateFetches))]
            internal static void UpdateFetchesWithoutClearables(GlobalChoreProvider provider, PathProber path_prober)
            {
#pragma warning disable CS8321
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) =>
                    TranspilerUtils.Transpile(instructions, original, RemoveClearables);
#pragma warning restore CS8321
                provider.UpdateFetches(path_prober);
            }

            private static bool RemoveClearables(List<CodeInstruction> instructions)
            {
                var method = typeof(ClearableManager).GetMethodSafe(nameof(ClearableManager.CollectAndSortClearables), false,
                    typeof(Navigator));
                var field = typeof(GlobalChoreProvider).GetFieldSafe(nameof(GlobalChoreProvider.clearableManager), false);
                if (method != null && field != null)
                {
                    int i = instructions.FindIndex(inst => inst.Calls(method));
                    if (i != -1)
                    {
                        int j = instructions.FindLastIndex(i, inst => inst.LoadsField(field));
                        if (j != -1)
                        {
                            if (instructions[j - 1].opcode == OpCodes.Ldarg_0)
                            {
                                instructions.RemoveRange(j, i - j + 1);
                                instructions.Insert(j, new CodeInstruction(OpCodes.Pop));
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
        }
    }
}
