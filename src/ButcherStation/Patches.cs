using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Klei.AI;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;

namespace ButcherStation
{
    internal sealed class Patches : KMod.UserMod2
    {
        public static AttributeConverter RanchingEffectExtraMeat;

        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
            harmony.PatchAllUncategorized();
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuildingsAndModifier(Harmony harmony)
        {
            Utils.AddBuildingToPlanScreen(BUILD_CATEGORY.Equipment, FishingStationConfig.ID, BUILD_SUBCATEGORY.ranching, MilkingStationConfig.ID);
            Utils.AddBuildingToPlanScreen(BUILD_CATEGORY.Equipment, ButcherStationConfig.ID, BUILD_SUBCATEGORY.ranching, MilkingStationConfig.ID);
            Utils.AddBuildingToTechnology("AnimalControl", ButcherStationConfig.ID, FishingStationConfig.ID);

            var formatter = new ToPercentAttributeFormatter(1f, GameUtil.TimeSlice.None);
            RanchingEffectExtraMeat = Db.Get().AttributeConverters.Create(
                id: "RanchingEffectExtraMeat",
                name: "Ranching Effect Extra Meat",
                description: STRINGS.DUPLICANTS.ATTRIBUTES.RANCHING.EFFECTEXTRAMEATMODIFIER,
                attribute: Db.Get().Attributes.Ranching,
                multiplier: ModOptions.Instance.extra_meat_per_ranching_attribute / 100f,
                base_value: 0f,
                formatter: formatter);
        }

        // добавляем тэги для убиваемых животных
        public static readonly Tag ButcherableCreature = TagManager.Create(nameof(ButcherableCreature));
        public static readonly Tag FisherableCreature = TagManager.Create(nameof(FisherableCreature));
        public static readonly Tag DoNotRanchMe = TagManager.Create(nameof(DoNotRanchMe));

        private static void AddEligibleTag(GameObject inst)
        {
            if (inst.TryGetComponent(out KPrefabID kpid))
            {
                Tag EligibleTag = kpid.HasTag(GameTags.SwimmingCreature) ? FisherableCreature : ButcherableCreature;
                kpid.AddTag(EligibleTag);
                DiscoveredResources.Instance.Discover(kpid.PrefabTag, EligibleTag);
            }
        }

        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess(Harmony harmony)
        {
            foreach (var go in Assets.GetPrefabsWithTag(GameTags.Creature))
                if (go.GetDef<RanchableMonitor.Def>() != null && go.TryGetComponent(out KPrefabID kpid))
                    kpid.prefabSpawnFn += AddEligibleTag;
            if (Assets.GetBuildingDef(UnderwaterRanchStationConfig.ID) != null)
                harmony.PatchCategory(SeaFairyConfig.ID);
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            RanchingEffectExtraMeat.multiplier = ModOptions.Instance.extra_meat_per_ranching_attribute / 100f;
        }

        [PLibMethod(RunAt.OnDetailsScreenInit)]
        private static void OnDetailsScreenInit()
        {
            PUIUtils.AddSideScreenContent<ButcherStationSideScreen>();
        }

        // сделать водорослевых рыб приручаемыми - чтобы ловились на рыбалке
        [HarmonyPatch(typeof(BaseSeaFairyConfig), nameof(BaseSeaFairyConfig.BaseSeaFairy))]
        private static class BaseSeaFairyConfig_BaseSeaFairy
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC5_ID);
            private static void Postfix(GameObject __result)
            {
                __result.AddOrGetDef<RanchableMonitor.Def>();
                __result.AddTag(DoNotRanchMe);
            }

            private static ChoreTable.Builder Inject(ChoreTable.Builder builder)
            {
                return builder.Add(new RanchedStates.Def() { WaitingAnim = "idle_loop" });
            }
            /*
                ChoreTable.Builder chore_table = new ChoreTable.Builder().Add
                blablabla
                .PushInterruptGroup()
                .Add(new FixedCaptureStates.Def(), true)
           +++  .Add(new RanchedStates.Def(), true)
                .Add(new MoveToLureStates.Def(), true)
                blablabla
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var capture_def = typeof(FixedCaptureStates.Def).GetConstructor(Type.EmptyTypes);
                var add = typeof(ChoreTable.Builder).GetMethodSafe(nameof(ChoreTable.Builder.Add), false, PPatchTools.AnyArguments);
                var inject = typeof(BaseSeaFairyConfig_BaseSeaFairy).GetMethodSafe(nameof(Inject), true, PPatchTools.AnyArguments);
                if (capture_def == null || add == null || inject == null)
                    return false;
                int i = instructions.FindIndex(instr => instr.Is(OpCodes.Newobj, capture_def));
                if (i == -1)
                    return false;
                i = instructions.FindIndex(i, instr => instr.Calls(add));
                if (i == -1)
                    return false;
                instructions.Insert(++i, new CodeInstruction(OpCodes.Call, inject));
                return true;
            }
        }

        // не надо чесать водорослевую рыбу
        [HarmonyPatch]
        [HarmonyPatchCategory(SeaFairyConfig.ID)]
        private static class UnderwaterRanchStationConfig_DoNotRanch_SeaFairy
        {
            private static MethodBase TargetMethod()
            {
                return Assets.GetBuildingDef(UnderwaterRanchStationConfig.ID).BuildingComplete
                    .GetDef<RanchStation.Def>().IsCritterEligibleToBeRanchedCb.Method;
            }
            private static bool Prefix(GameObject creature_go, ref bool __result)
            {
                if (creature_go.HasTag(DoNotRanchMe))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        // подменяем список жеготных, чтобы убивать в первую очередь совсем лишних, затем старых, затем просто лишних.
        [HarmonyPatch(typeof(RanchStation.Instance), nameof(RanchStation.Instance.FindRanchable))]
        private static class RanchStation_Instance_FindRanchable
        {
            private static List<KPrefabID> GetOrderedCreatureList(List<KPrefabID> creatures, RanchStation.Instance smi)
            {
                if (smi.gameObject.TryGetComponent<ButcherStation>(out var butcherStation))
                {
                    butcherStation.RefreshCreatures();
                    return butcherStation.CachedCreatures;
                }
                return creatures;
            }
            /*
                var creatures = this.GetStationCavity().creatures;
            +++ creatures = GetOrderedCreatureList(creatures, this);
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var creatures = typeof(CavityInfo).GetField(nameof(CavityInfo.creatures));
                var GetOrderedCreatureList = typeof(RanchStation_Instance_FindRanchable).GetMethodSafe(nameof(RanchStation_Instance_FindRanchable.GetOrderedCreatureList), true, PPatchTools.AnyArguments);
                if (creatures != null && GetOrderedCreatureList != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].LoadsField(creatures))
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, GetOrderedCreatureList));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // пропустить телодвижения жеготного после "ухаживания", чтобы сразу помирало
        [HarmonyPatch(typeof(RanchedStates), nameof(RanchedStates.InitializeStates))]
        private static class RanchedStates_InitializeStates
        {
            private static void Postfix(RanchedStates.RanchStates ___ranch)
            {
                ___ranch.Runaway
                    .TagTransition(GameTags.Creatures.Bagged, null)
                    .TagTransition(GameTags.Creatures.Die, null)
                    .TagTransition(GameTags.Dead, null);
            }
        }

        // тоже косметика - чтобы рыба могла переместиться в место ожидания очереди
        [HarmonyPatch(typeof(RanchStation.Instance), nameof(RanchStation.Instance.StationExtents), MethodType.Getter)]
        private static class RanchStation_Instance_StationExtents
        {
            private static void Postfix(RanchStation.Instance __instance, ref Extents __result)
            {
                if (!__instance.IsNullOrStopped() && __instance.GetTargetRanchCell() is int cell && Grid.IsValidCell(cell))
                {
                    Grid.CellToXY(cell, out _, out int y);
                    __result.y = y;
                }
            }
        }

        // поскольку рабочая точка станции рыбалки на одну клетку выше
        // уточняем куда идти ранчеру в .moveToRanch.MoveTo()
        [HarmonyPatch]
        private static class RancherChore_RancherChoreStates_InitializeStates
        {
            private static IEnumerable<MethodBase> methods;
            private static bool Prepare()
            {
                bool ok = TargetMethods().Count() > 0;
                if (!ok)
                    PUtil.LogWarning("Something went wrong when looking for a method 'RancherChore.RancherChoreStates.InitializeStates'");
                return ok;
            }
            private static IEnumerable<MethodBase> TargetMethods()
            {
                // ищем метод дёргающий Grid.PosToCell
                // .moveToRanch.MoveTo( (smi) => Grid.PosToCell(smi.transform.GetPosition()), blabla)
                if (methods == null)
                {
                    methods = typeof(RancherChore.RancherChoreStates).GetNestedTypes(PPatchTools.BASE_FLAGS)
                        .Where(t => t.IsDefined(typeof(CompilerGeneratedAttribute)))
                        .SelectMany(t => t.GetMethods(PPatchTools.BASE_FLAGS | BindingFlags.Instance))
                        .Where(m => m.Name.Contains(nameof(RancherChore.RancherChoreStates.InitializeStates)) && m.ReturnType == typeof(int))
                        .Where(m => PatchProcessor.ReadMethodBody(m)
                            .Where(code => code.Key == OpCodes.Call && code.Value is MethodBase method
                                && method.DeclaringType == typeof(Grid) && method.Name == nameof(Grid.PosToCell)).Any());
                }
                return methods;
            }
            private static void Cleanup() => methods = null;

            private static bool Prefix(RancherChore.RancherChoreStates.Instance smi, ref int __result)
            {
                if (smi.gameObject.TryGetComponent(out FisherWorkable workable))
                {
                    __result = Grid.OffsetCell(Grid.PosToCell(smi), workable.workOffset);
                    return false;
                }
                return true;
            }
        }

        // в прекондиции CanMoveTo заменяем building на workable
        // запрещаем пацыфистов
        [HarmonyPatch]
        private static class RancherChore_Constructor
        {
            public static Chore.Precondition ConsumerNotPacifist = new()
            {
                id = nameof(ConsumerNotPacifist),
                description = global::STRINGS.DUPLICANTS.TRAITS.SCAREDYCAT.DESC,
                sortOrder = -1,
                canExecuteOnAnyThread = true,
                fn = delegate (ref Chore.Precondition.Context context, object data)
                {
                    return context.consumerState.consumer.IsPermittedByTraits(Db.Get().ChoreGroups.Combat);
                }
            };

            private static MethodBase TargetMethod() => typeof(RancherChore).GetConstructors()[0];

            private static void Postfix(RancherChore __instance, KPrefabID rancher_station)
            {
                if (rancher_station.TryGetComponent(out FisherWorkable workable))
                {
                    var preconditions = __instance.GetPreconditions();
                    for (int i = 0; i < preconditions.Count; i++)
                    {
                        var precondition = preconditions[i];
                        if (precondition.condition.id == ChorePreconditions.instance.CanMoveTo.id)
                        {
                            precondition.data = workable;
                            preconditions[i] = precondition;
                            break;
                        }
                    }
                }
                if (!ModOptions.Instance.allow_pacifists && rancher_station.TryGetComponent(out ButcherStation _))
                {
                    __instance.AddPrecondition(ConsumerNotPacifist);
                }
            }
        }

        // проверка фундамента задней стены немедленно дает false если Grid.Solid
        // обойдём это для рыбалки, так как она выставляет оба Grid.Solid и Grid.FakeFloor
        [HarmonyPatch(typeof(BuildingDef), nameof(BuildingDef.CheckBackWallFoundation))]
        private static class BuildingDef_CheckBackWallFoundation
        {
            /*
            --- if (Grid.Solid[cell])
            +++ if (Grid.Solid[cell] && !Grid.FakeFloor[cell])
                    return false;
            */
            private static bool Inject(bool solid, int cell)
            {
                return solid && !Grid.FakeFloor[cell];
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var solid = typeof(Grid.BuildFlagsSolidIndexer).GetPropertyIndexedSafe<bool>("Item", false, typeof(int))?.GetGetMethod();
                var inject = typeof(BuildingDef_CheckBackWallFoundation).GetMethodSafe(nameof(Inject), true, PPatchTools.AnyArguments);
                if (solid == null || inject == null)
                    return false;
                int i = instructions.FindIndex(inst => inst.Calls(solid));
                if (i == -1 || !instructions[i - 1].IsLdloc())
                    return false;
                var ld_cell = new CodeInstruction(instructions[i - 1]);
                instructions.Insert(++i, ld_cell);
                instructions.Insert(++i, new CodeInstruction(OpCodes.Call, inject));
                return true;
            }
        }

        // для замены максимума жеготных
        [HarmonyPatch(typeof(BaggableCritterCapacityTracker), "OnPrefabInit")]
        private static class BaggableCritterCapacityTracker_OnPrefabInit
        {
            private static void Postfix(BaggableCritterCapacityTracker __instance)
            {
                __instance.maximumCreatures = ModOptions.Instance.max_creature_limit;
            }
        }

        // фикс чтобы станции правильно потребляли искричество
        [HarmonyPatch(typeof(RancherChore.RancherWorkable), "OnPrefabInit")]
        private static class RancherChore_RancherWorkable_OnPrefabInit
        {
            private static void Postfix(RancherChore.RancherWorkable __instance)
            {
                __instance.OnWorkableEventCB += OnWorkableEvent;
            }

            private static void OnWorkableEvent(Workable workable, Workable.WorkableEvent @event)
            {
                if (workable != null && workable.TryGetComponent<Operational>(out var operational))
                {
                    switch (@event)
                    {
                        case Workable.WorkableEvent.WorkStarted:
                            operational.SetActive(true);
                            break;
                        case Workable.WorkableEvent.WorkStopped:
                            operational.SetActive(false);
                            break;
                    }
                }
            }
        }
    }
}
