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

    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            ModOptions.Reload();
        }

        public static Tag RobotSuspend = TagManager.Create(nameof(RobotSuspend));
        public static Tag RobotSuspendBehaviour = TagManager.Create(nameof(RobotSuspendBehaviour));
        public static Tag RobotLandedIdleBehaviour = TagManager.Create(nameof(RobotLandedIdleBehaviour));
        public static Dictionary<Tag, AttributeModifier> SuspendedBatteryModifiers = new();
        public static Dictionary<Tag, AttributeModifier> IdleBatteryModifiers = new();
        public static Dictionary<Tag, AttributeModifier> LandedIdleBatteryModifiers = new();

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
            harmony.PatchAll();
            if (PPatchTools.GetTypeSafe("MassMoveTo.ModAssets") != null)
                harmony.Patch(typeof(Movable).GetMethodSafe(nameof(Movable.MoveToLocation), false, typeof(int)),
                    prefix: new HarmonyMethod(typeof(Movable_MoveToLocation_Kompot), nameof(Movable_MoveToLocation_Kompot.Prefix)));
        }

        [HarmonyPatch(typeof(BaseRoverConfig), nameof(BaseRoverConfig.BaseRover))]
        private static class BaseRoverConfig_BaseRover
        {
            private static void Postfix(GameObject __result, string id, float batteryDepletionRate, Amount batteryType)
            {
                __result.AddOrGet<RobotTurnOffOn>();
                __result.AddOrGetDef<MoveToLocationMonitor.Def>().invalidTagsForMoveTo = new Tag[] { GameTags.Dead, GameTags.Stored, RobotSuspend };
                __result.AddOrGet<Movable>(); // переносить спящего
                SuspendedBatteryModifiers[id] = new AttributeModifier(batteryType.deltaAttribute.Id, batteryDepletionRate, NAME);
                if (ModOptions.Instance.low_power_mode_enable)
                {
                    float rate = batteryDepletionRate * (1f - ModOptions.Instance.low_power_mode_value / 100f);
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
            // подобно FetchDroneConfig
            /*
            var chore_table = new ChoreTable.Builder()
                .Add(new RobotDeathStates.Def(), true, Db.Get().ChoreTypes.Die.priority)
                .Add(new FallStates.Def(), true, -1)
                .Add(new DebugGoToStates.Def(), true, -1)
            +++ .PushInterruptGroup()
            +++ .Add(new RoverSleepStates.Def(), true, Db.Get().ChoreTypes.Die.priority)
            +++ .PopInterruptGroup()
                .Add(new IdleStates.Def блаблабла );
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static ChoreTable.Builder Inject(ChoreTable.Builder chore_table)
            {
                return chore_table
                    .PushInterruptGroup()
                    .Add(new RoverSleepStates.Def(), true, Db.Get().ChoreTypes.Die.priority)
                    .PopInterruptGroup();
            }

            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var idle = typeof(IdleStates.Def).GetConstructors()[0];
                var inject = typeof(BaseRoverConfig_BaseRover).GetMethodSafe(nameof(Inject), true, typeof(ChoreTable.Builder));
                if (idle != null && inject != null)
                {
                    int i = instructions.FindIndex(inst => inst.Is(OpCodes.Newobj, idle));
                    if (i != -1)
                    {
                        instructions.Insert(i, new CodeInstruction(OpCodes.Call, inject));
                        return true;
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(BaseRoverConfig), nameof(BaseRoverConfig.OnSpawn))]
        private static class BaseRoverConfig_OnSpawn
        {
            // ровер не должен заряжаться при переносе
            private static void FixEffect(GameObject go)
            {
                const string effect = "ScoutBotCharging";
                if (go.TryGetComponent(out Effects effects) && effects.HasEffect(effect))
                    effects.Remove(effect);
            }

            // анимация до/после переноса
            private static void PlayAnim(GameObject go)
            {
                if (go.TryGetComponent(out KBatchedAnimController kbac))
                    kbac.Play(go.HasTag(GameTags.Dead) ? "idle_dead" : "in_storage", KAnim.PlayMode.Once);
            }

            private static void Postfix(GameObject inst)
            {
                var movable = inst.AddOrGet<Movable>();
                movable.tagRequiredForMove = GameTags.Creatures.Deliverable;
                movable.onPickupComplete += FixEffect;
                movable.onPickupComplete += PlayAnim;
                movable.onDeliveryComplete += PlayAnim;
            }
        }

        // афтоматически разбирать трупоф
        [HarmonyPatch(typeof(MorbRoverConfig), nameof(MorbRoverConfig.OnSpawn))]
        private static class MorbRoverConfig_OnSpawn
        {
            private static bool Prepare() => !ModOptions.Instance.deconstruct_dead_biobot;

            private static void Postfix(MorbRoverConfig __instance, GameObject inst)
            {
                inst.Unsubscribe((int)GameHashes.Died, __instance.TriggerDeconstructChoreOnDeath);
            }
        }

        [HarmonyPatch(typeof(ScoutRoverConfig), nameof(ScoutRoverConfig.OnSpawn))]
        private static class ScoutRoverConfig_OnSpawn
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active()
                && ModOptions.Instance.deconstruct_dead_rover;

            private static void Postfix(GameObject inst)
            {
                inst.Subscribe((int)GameHashes.Died, TriggerDeconstructChoreOnDeath);
            }

            private static void TriggerDeconstructChoreOnDeath(object obj)
            {
                if (obj is GameObject go && go != null && go.TryGetComponent(out Deconstructable deconstructable)
                    && !deconstructable.IsMarkedForDeconstruction())
                {
                    deconstructable.QueueDeconstruction(false);
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

        // вкл выкл
        [HarmonyPatch(typeof(RobotAi), nameof(RobotAi.InitializeStates))]
        private static class RobotAi_InitializeStates
        {
            private static void Postfix(RobotAi __instance)
            {
                StatusItem RobotSleeping = new(nameof(RobotSleeping), NAME, TOOLTIP, "status_item_exhausted",
                    StatusItem.IconType.Custom, NotificationType.Neutral, false, default,
                    showWorldIcon: ModOptions.Instance.zzz_icon_enable);

                var suspended = __instance.CreateState("suspended", __instance.alive);

                __instance.alive.normal
                    .TagTransition(RobotSuspend, suspended, false);

                suspended
                    .TagTransition(RobotSuspend, __instance.alive.normal, true)
                    .ToggleStatusItem(RobotSleeping)
                    .ToggleAttributeModifier("save battery",
                        smi => SuspendedBatteryModifiers[smi.PrefabID()],
                        smi => SuspendedBatteryModifiers.ContainsKey(smi.PrefabID()))
                    .ToggleStateMachine(smi => new RobotSleepFX.Instance(smi.master))
                    .ToggleBehaviour(RobotSuspendBehaviour, smi => true)
                    .Enter(smi => smi.RefreshUserMenu())
                    .ScheduleActionNextFrame("Clean StatusItem", CleanStatusItem)
                    .Exit(smi => smi.RefreshUserMenu());

                __instance.dead
                    .ToggleTag(GameTags.Creatures.Deliverable);
            }

            // очищаем лишний StatusItem который может появиться при прерывании выполнения FetchAreaChore
            private static void CleanStatusItem(StateMachine.Instance smi)
            {
                if (!smi.IsNullOrStopped() && smi.gameObject.TryGetComponent<KSelectable>(out var selectable))
                    selectable.SetStatusItem(Db.Get().StatusItemCategories.Main, null, null);
            }
        }

        //возвращять материалы из убитого флудо
        [HarmonyPatch(typeof(RobotAi), nameof(RobotAi.DeleteOnDeath))]
        private static class RobotAi_DeleteOnDeath
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC3_ID)
                && ModOptions.Instance.dead_flydo_returns_materials;

            private static void Prefix(RobotAi.Instance smi)
            {
                if (((RobotAi.Def)smi.def).DeleteOnDead && smi.gameObject.TryGetComponent(out Deconstructable deconstructable))
                {
                    deconstructable.enabled = true;
                    if (deconstructable.constructionElements == null || deconstructable.constructionElements.Length == 0)
                        deconstructable.constructionElements = new Tag[] { deconstructable.GetComponent<PrimaryElement>().Element.tag };
                    deconstructable.SpawnItemsFromConstruction(null);
                }
            }
        }

        // для совместимости с MassMoveTo. отменить перемещение если это робот и он не выключен
        //[HarmonyPatch(typeof(Movable), nameof(Movable.MoveToLocation))]
        private static class Movable_MoveToLocation_Kompot
        {
            private static readonly Action<Movable> ClearStorageProxy = typeof(Movable).Detour<Action<Movable>>("ClearStorageProxy");
            private static readonly Func<Movable, bool> HasTagRequiredToMove = typeof(Movable).Detour<Func<Movable, bool>>("HasTagRequiredToMove");

            internal static bool Prefix(Movable __instance)
            {
                if (__instance != null && !HasTagRequiredToMove(__instance))
                {
                    if (!__instance.IsMarkedForMove && __instance.StorageProxy != null)
                        ClearStorageProxy(__instance);
                    return false;
                }
                return true;
            }
        }

        // низкая мощность при безделии
        [HarmonyPatch(typeof(IdleStates), nameof(IdleStates.InitializeStates))]
        private static class IdleStates_InitializeStates
        {
            private static bool Prepare() => ModOptions.Instance.low_power_mode_enable;

            private static void Postfix(IdleStates.State ___loop)
            {
                ___loop.ToggleAttributeModifier("low power mode",
                        smi => IdleBatteryModifiers[smi.PrefabID()],
                        smi => IdleBatteryModifiers.ContainsKey(smi.PrefabID()));
            }
        }

        // не падать когда несут
        [HarmonyPatch(typeof(FallWhenDeadMonitor.Instance), nameof(FallWhenDeadMonitor.Instance.IsFalling))]
        private static class FallWhenDeadMonitor_Instance_IsFalling
        {
            private static void Postfix(FallWhenDeadMonitor.Instance __instance, ref bool __result)
            {
                if (__result && __instance.HasTag(GameTags.Stored))
                    __result = false;
            }
        }

        // миграция настроек дверей под новую систему У57
        [HarmonyPatch(typeof(AccessControl), "UpgradeSavesToPostRobotDoorPermissions")]
        private static class UpgradeSavesToPostRobotDoorPermissions
        {
            private static readonly DetouredMethod<Action<AccessControl, KPrefabID>> ClearGridRestrictions
                = typeof(AccessControl).DetourLazy<Action<AccessControl, KPrefabID>>("ClearGridRestrictions");

            private static void Prefix(AccessControl __instance, AccessControl.Permission ____defaultPermission,
            List<KeyValuePair<Ref<KPrefabID>, AccessControl.Permission>> ___savedPermissions)
            {
                var list = ListPool<Tuple<MinionAssignablesProxy, AccessControl.Permission>, AccessControl>.Allocate();
                for (int i = ___savedPermissions.Count - 1; i >= 0; i--)
                {
                    var kprefabID = ___savedPermissions[i].Key.Get();
                    if (kprefabID != null)
                    {
                        var proxy = kprefabID.GetComponent<RobotAssignablesProxy>();
                        if (proxy != null)
                        {
                            list.Add(new(proxy, ___savedPermissions[i].Value));
                            ___savedPermissions.RemoveAt(i);
                            ClearGridRestrictions.Invoke(__instance, kprefabID);
                        }
                    }
                }
                foreach (var tuple in list)
                {
                    var robot_tag = ((RobotAssignablesProxy)tuple.first).PrefabID;
                    var permission = tuple.second;
#pragma warning disable CS0612
                    // если ранее фича для запрета флудо всех дверей была включена, не будем мигрировать вариант "ты не пройдёш"
                    if (robot_tag == GameTags.Robots.Models.FetchDrone && permission == AccessControl.Permission.Neither
                        && ModOptions.Instance.flydo_can_pass_door && ModOptions.Instance.restrict_flydo_by_default)
                        continue;
#pragma warning restore CS0612
                    if (GridRestrictionSerializer.Instance.ValidRobotTypes.Contains(robot_tag))
                        __instance.SetPermission(robot_tag, permission);
                }
                list.Recycle();
                if (____defaultPermission > AccessControl.Permission.Both)
                    __instance.SetDefaultPermission(GameTags.Robot, ____defaultPermission);
            }
        }

        // внедряем портреты роботов в эээ.. все портреты
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
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions)
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
                return instructions.Transpile(original, IL, transpiler);
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
            private static bool transpiler(ref List<CodeInstruction> instructions, ILGenerator IL)
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
