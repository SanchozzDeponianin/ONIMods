using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Klei.AI;
using STRINGS;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace ControlYourRobots
{
    using static STRINGS.ROBOTS.STATUSITEMS.SLEEP_MODE;

    internal sealed class ControlYourRobotsPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(ControlYourRobotsPatches));
            new POptions().RegisterOptions(this, typeof(ControlYourRobotsOptions));
            ControlYourRobotsOptions.Reload();
        }

        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<KMod.Mod> mods)
        {
            if (PPatchTools.GetTypeSafe("MassMoveTo.ModAssets") != null)
                harmony.Patch(typeof(Movable).GetMethodSafe(nameof(Movable.MoveToLocation), false, typeof(int)),
                    prefix: new HarmonyMethod(typeof(Movable_MoveToLocation_Kompot), nameof(Movable_MoveToLocation_Kompot.Prefix)));
        }

        public static Tag RobotSuspend = TagManager.Create(nameof(RobotSuspend));
        private static Dictionary<Tag, AttributeModifier> SuspendedBatteryModifiers = new Dictionary<Tag, AttributeModifier>();
        private static Dictionary<Tag, AttributeModifier> IdleBatteryModifiers = new Dictionary<Tag, AttributeModifier>();

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [HarmonyPatch(typeof(BaseRoverConfig), nameof(BaseRoverConfig.BaseRover))]
        private static class BaseRoverConfig_BaseRover
        {
            private static void Postfix(GameObject __result, string id, float batteryDepletionRate, Amount batteryType)
            {
                __result.AddOrGet<RobotTurnOffOn>();
                __result.AddOrGet<Movable>(); // переносить спящего
                SuspendedBatteryModifiers[id] = new AttributeModifier(batteryType.deltaAttribute.Id, batteryDepletionRate, NAME);
                if (ControlYourRobotsOptions.Instance.low_power_mode_enable)
                {
                    float rate = batteryDepletionRate * (1f - ControlYourRobotsOptions.Instance.low_power_mode_value / 100f);
                    IdleBatteryModifiers[id] = new AttributeModifier(batteryType.deltaAttribute.Id, rate, CREATURES.STATUSITEMS.IDLE.NAME);
                }
                __result.AddOrGet<RobotPersonalPriorityProxy>();
                // поправим небольшой косячок клеев (гоботам была разрешена Rocketry)
                // а также разрешим LifeSupport
                var trait = Db.Get().traits.TryGet(id + "BaseTrait");
                if (trait != null)
                {
                    var disabled = trait.disabledChoreGroups.ToList();
                    disabled.Remove(Db.Get().ChoreGroups.LifeSupport);
                    if (DlcManager.FeatureClusterSpaceEnabled())
                        disabled.Add(Db.Get().ChoreGroups.Rocketry);
                    trait.disabledChoreGroups = disabled.ToArray();
                }
            }
        }

        // запретить хоронить трупов (иначе вылетает при поднятии тела)
        [HarmonyPatch(typeof(Grave.StatesInstance), nameof(Grave.StatesInstance.CreateFetchTask))]
        private static class Grave_StatesInstance_CreateFetchTask
        {
            private static void Postfix(FetchChore ___chore)
            {
                ___chore.AddPrecondition(ChorePreconditions.instance.IsNotARobot, FetchDroneConfig.ID);
            }
        }

        // иди туды и вкл выкл
        [HarmonyPatch(typeof(RobotAi), nameof(RobotAi.InitializeStates))]
        private static class RobotAi_InitializeStates
        {
            public class SuspendedStates : RobotAi.State
            {
#pragma warning disable CS0649
                public RobotAi.State satisfied;
                public RobotAi.State moved;
#pragma warning restore CS0649
            }

            private static void Postfix(RobotAi __instance)
            {
                const string name = "suspended";
                var suspended = (SuspendedStates)__instance.CreateState(name, __instance.alive, new SuspendedStates());

                __instance.alive.normal
                    // иди туды, но не для летуна
                    //.ToggleStateMachine(smi => new MoveToLocationMonitor.Instance(smi.master))
                    .Enter(smi =>
                    {
                        if (!smi.HasTag(GameTags.Robots.Models.FetchDrone))
                            new MoveToLocationMonitor.Instance(smi.master);
                    })
                    .Exit(smi =>
                    {
                        var move_monitor = smi.GetSMI<MoveToLocationMonitor.Instance>();
                        if (!move_monitor.IsNullOrStopped())
                            move_monitor.StopSM("");
                    })
                    .TagTransition(RobotSuspend, suspended, false);

                suspended
                    .TagTransition(RobotSuspend, __instance.alive.normal, true)
                    .DefaultState(suspended.satisfied)
                    .ToggleStatusItem(NAME, TOOLTIP)
                    .ToggleAttributeModifier("save battery",
                        smi => SuspendedBatteryModifiers[smi.PrefabID()],
                        smi => SuspendedBatteryModifiers.ContainsKey(smi.PrefabID()))
                    .ToggleBrain(name)
                    .Enter(smi =>
                    {
                        smi.GetComponent<Navigator>().Pause(name);
                        smi.GetComponent<Storage>().DropAll();
                        smi.RefreshUserMenu();
                    })
                    .ScheduleActionNextFrame("Clean StatusItem", CleanStatusItem)
                    .Exit(smi =>
                    {
                        // отменить перемещение при просыпании
                        var movable = smi.GetComponent<Movable>();
                        if (movable != null && movable.StorageProxy != null)
                            movable.StorageProxy.GetComponent<CancellableMove>().OnCancel(movable);
                        smi.GetComponent<Navigator>().Unpause(name);
                        smi.RefreshUserMenu();
                    });

                suspended.satisfied
                    .PlayAnim("in_storage")
                    .TagTransition(GameTags.Stored, suspended.moved, false)
                    .ToggleStateMachine(smi => new RobotSleepFX.Instance(smi.master))
                    .ToggleStateMachine(smi => new FallWhenDeadMonitor.Instance(smi.master))
                    .Enter(smi =>
                    {
                        // принудительно "роняем" робота чтобы он не зависал в воздухе после перемещения
                        var fall_smi = smi.GetSMI<FallWhenDeadMonitor.Instance>();
                        if (!fall_smi.IsNullOrStopped())
                            fall_smi.GoTo(fall_smi.sm.falling);
                    });

                suspended.moved
                    .PlayAnim("in_storage")
                    .TagTransition(GameTags.Stored, suspended.satisfied, true);
            }

            // очищаем лишний StatusItem который может появиться при прерывании выполнения FetchAreaChore
            private static void CleanStatusItem(StateMachine.Instance smi)
            {
                if (!smi.IsNullOrStopped() && smi.gameObject.TryGetComponent<KSelectable>(out var selectable))
                    selectable.SetStatusItem(Db.Get().StatusItemCategories.Main, null, null);
            }
        }

        // иди туды, для летуна
        [HarmonyPatch(typeof(RobotElectroBankMonitor), nameof(RobotElectroBankMonitor.InitializeStates))]
        private static class RobotElectroBankMonitor_InitializeStates
        {
            private static void Postfix(RobotElectroBankMonitor __instance)
            {
                __instance.powered
                    .ToggleStateMachine(smi => new MoveToLocationMonitor.Instance(smi.master))
                    .Toggle(nameof(RefreshUserMenu), RefreshUserMenu, RefreshUserMenu);
            }

            private static void RefreshUserMenu(RobotElectroBankMonitor.Instance smi)
            {
                Game.Instance.userMenu.Refresh(smi.master.gameObject);
            }
        }

        // скрываем кнопку MoveTo для перемещения объектов если это робот и он не выключен
        private static bool IsRobotAndNotSuspend(KPrefabID kPrefabID)
        {
            return kPrefabID.HasTag(GameTags.Robot) && !kPrefabID.HasTag(RobotSuspend) && !kPrefabID.HasTag(GameTags.Dead);
        }

        [HarmonyPatch(typeof(Movable), "OnRefreshUserMenu")]
        private static class Movable_OnRefreshUserMenu
        {
            private static bool Prefix(Pickupable ___pickupable)
            {
                return !IsRobotAndNotSuspend(___pickupable.KPrefabID);
            }
        }

        // для совместимости с MassMoveTo. отменить перемещение если это робот и он не выключен
        //[HarmonyPatch(typeof(Movable), nameof(Movable.MoveToLocation))]
        private static class Movable_MoveToLocation_Kompot
        {
            private static readonly Action<Movable> ClearStorageProxy = typeof(Movable).Detour<Action<Movable>>("ClearStorageProxy");

            internal static bool Prefix(Movable __instance, Pickupable ___pickupable)
            {
                if (__instance != null && IsRobotAndNotSuspend(___pickupable.KPrefabID))
                {
                    if (!__instance.IsMarkedForMove && __instance.StorageProxy != null)
                        ClearStorageProxy(__instance);
                    return false;
                }
                return true;
            }
        }

        // если команда MoveTo применена к выключенному роботу
        // патчим чору, чтобы его переносили как кусок ресурса, а не как жеготное, так как у роботов нет некоторых компонентов
        [HarmonyPatch(typeof(MovePickupableChore), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(IStateMachineTarget), typeof(GameObject), typeof(Action<Chore>) })]
        private static class MovePickupableChore_Constructor
        {
            /*
        --- if (pickupable.GetComponent<CreatureBrain>())
        +++ if (pickupable.GetComponent<CreatureBrain>() && !pickupable.HasTag(GameTags.Robot))
            {
                AddPrecondition(blabla);
                AddPrecondition(blabla);
            }
            else
            {
                AddPrecondition(blabla);
            }
            */
            private static Component IsNotRobot(Component cmp)
            {
                if (cmp != null && cmp.HasTag(GameTags.Robot))
                    return null;
                return cmp;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions, MethodBase original)
            {
                var gc = typeof(GameObject).GetMethodSafe(nameof(GameObject.GetComponent), false)
                    ?.MakeGenericMethod(typeof(CreatureBrain));
                var inject = typeof(MovePickupableChore_Constructor)
                    .GetMethodSafe(nameof(IsNotRobot), true, typeof(Component));
                if (gc != null && inject != null)
                {
                    int n = 0;
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(gc))
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, inject));
                            n++;
                        }
                    }
                    if (n > 0)
                        return true;
                }
                return false;
            }
        }

        // низкая мощность при безделии
        [HarmonyPatch(typeof(IdleStates), nameof(IdleStates.InitializeStates))]
        private static class IdleStates_InitializeStates
        {
            private static bool Prepare() => ControlYourRobotsOptions.Instance.low_power_mode_enable;

            private static void Postfix(IdleStates.State ___loop)
            {
                ___loop.ToggleAttributeModifier("low power mode",
                        smi => IdleBatteryModifiers[smi.PrefabID()],
                        smi => IdleBatteryModifiers.ContainsKey(smi.PrefabID()));
            }
        }

        // поиск пути с учётом разрешения дверей
        [HarmonyPatch(typeof(CreatureBrain), "OnPrefabInit")]
        private static class CreatureBrain_OnPrefabInit
        {
            private static Tag[] Robot_AI_Tags = { GameTags.Robot, GameTags.DupeBrain };
            private static void Postfix(CreatureBrain __instance)
            {
                if (__instance.HasAllTags(Robot_AI_Tags) && __instance.TryGetComponent<Navigator>(out var navigator)
                    && navigator.NavGridName == "RobotNavGrid") // не для летуна
                    navigator.SetAbilities(new RobotPathFinderAbilities(navigator));
            }
        }

        // внедряем роботов в экран доступов двери
        [HarmonyPatch(typeof(AccessControlSideScreen), nameof(AccessControlSideScreen.SetTarget))]
        private static class AccessControlSideScreen_SetTarget
        {
            private static List<MinionAssignablesProxy> Inject(List<MinionAssignablesProxy> list)
            {
                list.AddRange(RobotAssignablesProxy.Cmps.Items);
                return list;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var list = typeof(AccessControlSideScreen).GetFieldSafe("identityList", false);
                var inject = typeof(AccessControlSideScreen_SetTarget).GetMethodSafe(nameof(Inject), true, PPatchTools.AnyArguments);
                if (list != null && inject != null)
                {
                    int i = instructions.FindIndex(ins => ins.StoresField(list));
                    if (i != -1)
                    {
                        instructions.Insert(i, new CodeInstruction(OpCodes.Call, inject));
                        return true;
                    }
                }
                return false;
            }
        }

        // внедряем портреты роботов в эээ.. экран доступов двери.. все портреты
        [HarmonyPatch(typeof(CrewPortrait), "Rebuild")]
        private static class CrewPortrait_SetIdentityObject
        {
            private static void Postfix(CrewPortrait __instance)
            {
                if (__instance.controller == null)
                    return;
                var children = __instance.GetComponentsInChildren<KBatchedAnimController>(true);
                // в норме тут ^ только один kbac, и он равен controller. его трогать не будем.
                if (!__instance.identityObject.IsNullOrDestroyed() && __instance.identityObject is RobotAssignablesProxy proxy)
                {
                    KBatchedAnimController robot = null;
                    // ищем ранее внедрённый портрет
                    for (int i = 0; i < children.Length; i++)
                    {
                        if (children[i] != __instance.controller)
                        {
                            robot = children[i];
                            break;
                        }
                    }
                    // или внедряем новый
                    if (robot == null)
                    {
                        robot = Util.KInstantiateUI<KBatchedAnimController>(__instance.controller.gameObject, __instance.controller.gameObject.transform.parent.gameObject, false);
                    }
                    robot.gameObject.SetActive(false);
                    robot.SwapAnims(Assets.GetPrefab(proxy.PrefabID).GetComponent<KBatchedAnimController>().AnimFiles);
                    robot.initialAnim = "ui";
                    robot.gameObject.SetActive(true);
                    robot.Play("ui", KAnim.PlayMode.Loop);
                }
                else
                {
                    // если не робот - ищем и скрываем ранее внедренные портреты
                    // надеюсь другие моды не будут внедрять левые портреты
                    for (int i = 0; i < children.Length; i++)
                    {
                        if (children[i] != __instance.controller)
                        {
                            children[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        // далее: для внедрения в экран приоритетов

        [HarmonyPatch(typeof(TableRow), nameof(TableRow.ConfigureContent))]
        private static class TableRow_ConfigureContent
        {
            private static void Prefix(TableRow __instance, IAssignableIdentity minion)
            {
                if (minion is RobotAssignablesProxy)
                    __instance.rowType = TableRow.RowType.Minion;
            }
        }

        [HarmonyPatch(typeof(JobsTableScreen), "GetPriorityManager")]
        private static class JobsTableScreen_GetPriorityManager
        {
            private static void Postfix(TableRow row, ref IPersonalPriorityManager __result)
            {
                if (__result == null && row.rowType == TableRow.RowType.Minion && row.GetIdentity() is RobotAssignablesProxy proxy)
                {
                    __result = proxy;
                }
            }
        }

        // внедряемся в экран приоритетов
        [HarmonyPatch(typeof(TableScreen), "RefreshRows")]
        private static class TableScreen_RefreshRows
        {
            private static Action<TableScreen, IAssignableIdentity> AddRow = typeof(TableScreen).Detour<Action<TableScreen, IAssignableIdentity>>("AddRow");

            private static void Inject(TableScreen screen)
            {
                if (screen is JobsTableScreen)
                {
                    // не добавлять если все гоботы уничтожены
                    foreach (var proxy in RobotAssignablesProxy.Cmps.Items)
                    {
                        if (proxy != null)
                        {
                            foreach (var rppp in RobotPersonalPriorityProxy.Cmps.Items)
                            {
                                if (rppp != null && rppp.PrefabID == proxy.PrefabID)
                                {
                                    AddRow(screen, proxy);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var cmi = typeof(ClusterManager).GetFieldSafe(nameof(ClusterManager.Instance), true);
                var inject = typeof(TableScreen_RefreshRows).GetMethodSafe(nameof(Inject), true, PPatchTools.AnyArguments);
                if (cmi != null && inject != null)
                {
                    int i = instructions.FindIndex(ins => ins.LoadsField(cmi));
                    if (i != -1)
                    {
                        var Ldfld_cmi = instructions[i];
                        instructions.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(Ldfld_cmi).MoveBlocksFrom(Ldfld_cmi));
                        instructions.Insert(i++, new CodeInstruction(OpCodes.Call, inject));
                        return true;
                    }
                }
                return false;
            }
        }

        // тоолтипы для гоботов
        [HarmonyPatch(typeof(JobsTableScreen), "HoverPersonalPriority")]
        private static class JobsTableScreen_HoverPersonalPriority
        {
            private static Func<TableScreen, GameObject, TableRow> GetWidgetRow
                = typeof(TableScreen).Detour<Func<TableScreen, GameObject, TableRow>>("GetWidgetRow");

            private static Func<TableScreen, GameObject, TableColumn> GetWidgetColumn
                = typeof(TableScreen).Detour<Func<TableScreen, GameObject, TableColumn>>("GetWidgetColumn");

            private static Func<JobsTableScreen, int, LocString> GetPriorityStr
                = typeof(JobsTableScreen).Detour<Func<JobsTableScreen, int, LocString>>("GetPriorityStr");

            private static Func<JobsTableScreen, int, string> GetPriorityValue
                = typeof(JobsTableScreen).Detour<Func<JobsTableScreen, int, string>>("GetPriorityValue");

            private static Func<JobsTableScreen, string> GetUsageString
                = typeof(JobsTableScreen).Detour<Func<JobsTableScreen, string>>("GetUsageString");

            private static void Postfix(object widget_go_obj, JobsTableScreen __instance, ref string __result)
            {
                if (!string.IsNullOrEmpty(__result))
                    return;
                var go = widget_go_obj as GameObject;
                var row = GetWidgetRow(__instance, go);
                if (row.rowType != TableRow.RowType.Minion)
                    return;
                var proxy = row.GetIdentity() as RobotAssignablesProxy;
                if (proxy == null)
                    return;
                var group = (GetWidgetColumn(__instance, go) as PrioritizationGroupTableColumn).userData as ChoreGroup;
                var toolTip = go.GetComponentInChildren<ToolTip>();
                string text;
                if (proxy.IsChoreGroupDisabled(group, out var trait))
                {
                    text = UI.JOBSSCREEN.TRAIT_DISABLED.ToString()
                        .Replace("{Name}", proxy.GetProperName())
                        .Replace("{Job}", group.Name)
                        .Replace("{Trait}", trait.Name);
                    toolTip.ClearMultiStringTooltip();
                    toolTip.AddMultiStringTooltip(text, null);
                }
                else
                {
                    int priority = proxy.GetPersonalPriority(group);
                    text = UI.JOBSSCREEN.ITEM_TOOLTIP.ToString()
                        .Replace("{Name}", row.name)
                        .Replace("{Job}", group.Name)
                        .Replace("{Priority}", GetPriorityStr(__instance, priority))
                        .Replace("{PriorityValue}", GetPriorityValue(__instance, priority));
                    toolTip.ClearMultiStringTooltip();
                    toolTip.AddMultiStringTooltip(text, null);
                    text = "\n" + UI.JOBSSCREEN.MINION_SKILL_TOOLTIP.ToString()
                        .Replace("{Name}", proxy.GetProperName())
                        .Replace("{Attribute}", group.attribute.Name);
                    text += GameUtil.ColourizeString(__instance.TooltipTextStyle_Ability.textColor, proxy.GetAssociatedSkillLevel(group).ToString());
                    toolTip.AddMultiStringTooltip(text, null);
                    toolTip.AddMultiStringTooltip(UI.HORIZONTAL_RULE + "\n" + GetUsageString(__instance), null);
                }
            }
        }

        [HarmonyPatch(typeof(TableScreen), "SortRows")]
        private static class TableScreen_SortRows
        {
            // предотвращение краша
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL)
            {
                return TranspilerUtils.Wrap(instructions, original, IL, transpiler);
            }
            /*
                пропускаем эти вызовы методов если переменная является RobotAssignablesProxy
                myWorld = keyValuePair.Key
                --- .GetSoleOwner()
                --- .GetComponent<MinionAssignablesProxy>()
                    .GetTargetGameObject()
                    .GetComponent<KMonoBehaviour>()
                    .GetMyWorld();
            */
            private static bool transpiler(List<CodeInstruction> instructions, ILGenerator IL)
            {
                var GetSoleOwner = typeof(IAssignableIdentity).GetMethodSafe(nameof(IAssignableIdentity.GetSoleOwner), false);
                var GetTargetGameObject = typeof(MinionAssignablesProxy).GetMethodSafe(nameof(MinionAssignablesProxy.GetTargetGameObject), false);
                if (GetSoleOwner != null && GetTargetGameObject != null)
                {
                    int i = instructions.FindIndex(ins => ins.Calls(GetSoleOwner));
                    int j = instructions.FindIndex(ins => ins.Calls(GetTargetGameObject));
                    if (i != -1 && j != -1)
                    {
                        var @goto = IL.DefineLabel();
                        instructions[j].labels.Add(@goto);
                        instructions.Insert(i++, new CodeInstruction(OpCodes.Dup));
                        instructions.Insert(i++, new CodeInstruction(OpCodes.Isinst, typeof(RobotAssignablesProxy)));
                        instructions.Insert(i++, new CodeInstruction(OpCodes.Brtrue_S, @goto));
                        return true;
                    }
                }
                return false;
            }

            // сортировка на верх
            private static void Postfix(TableScreen __instance)
            {
                if (__instance is JobsTableScreen)
                {
                    foreach (var row in __instance.all_sortable_rows)
                    {
                        if (row.GetIdentity() is RobotAssignablesProxy)
                            row.gameObject.transform.SetSiblingIndex(1);
                    }
                }
            }
        }

        // ищо предотвращение краша
        [HarmonyPatch(typeof(ChoreConsumer), nameof(ChoreConsumer.GetAssociatedSkillLevel))]
        private static class ChoreConsumer_GetAssociatedSkillLevel
        {
            private static bool Prefix(ChoreGroup group, ChoreConsumer __instance, ref int __result)
            {
                if (__instance.GetAttributes().Get(group.attribute.Id) == null)
                {
                    __result = 0;
                    return false;
                }
                return true;
            }
        }
    }
}
