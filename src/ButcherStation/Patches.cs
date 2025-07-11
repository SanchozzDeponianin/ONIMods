﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Database;
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
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
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
            RoomsExpandedCompat(harmony);
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

        // добавляем тэги для убиваемых животных
        [HarmonyPatch]
        private static class EntityTemplates_ExtendEntityToBasicCreature
        {
            private static MethodBase TargetMethod()
            {
                // ищем метод с большим количеством параметров (У52 методов всего два)
                return typeof(EntityTemplates).GetOverloadWithMostArguments(nameof(EntityTemplates.ExtendEntityToBasicCreature), true);
            }

            private static void Postfix(GameObject __result)
            {
                __result.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject inst)
                {
                    if (inst.GetDef<RanchableMonitor.Def>() != null)
                    {
                        inst.TryGetComponent<KPrefabID>(out var prefabID);
                        Tag creatureEligibleTag = prefabID.HasTag(GameTags.SwimmingCreature) ? ButcherStation.FisherableCreature : ButcherStation.ButcherableCreature;
                        prefabID.AddTag(creatureEligibleTag);
                        DiscoveredResources.Instance.Discover(prefabID.PrefabTag, creatureEligibleTag);
                    }
                };
            }
        }

        // сделать рыб приручаемыми - чтобы ловились на рыбалке
        [HarmonyPatch(typeof(BasePacuConfig), nameof(BasePacuConfig.CreatePrefab))]
        private static class BasePacuConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result, bool is_baby)
            {
                if (!is_baby)
                {
                    __result.AddOrGetDef<RanchableMonitor.Def>();
                }
            }

            private static ChoreTable.Builder Inject(ChoreTable.Builder builder, bool is_baby)
            {
                return builder.Add(new RanchedStates.Def() { WaitingAnim = "idle_loop" }, !is_baby);
            }
            /*
                ChoreTable.Builder chore_table = new ChoreTable.Builder().Add
                blablabla
                .PushInterruptGroup()
                .Add(new FixedCaptureStates.Def(), true)
           +++  .Add(new RanchedStates.Def(), !is_baby)
                .Add(new LayEggStates.Def(), !is_baby)
                blablabla
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var LayEggDef = typeof(LayEggStates.Def).GetConstructor(Type.EmptyTypes);
                var is_baby = typeof(BasePacuConfig).GetMethodSafe(nameof(BasePacuConfig.CreatePrefab), true, PPatchTools.AnyArguments)
                    ?.GetParameters().First(arg => arg.Name == "is_baby");
                var Inject = typeof(BasePacuConfig_CreatePrefab).GetMethodSafe(nameof(BasePacuConfig_CreatePrefab.Inject), true, PPatchTools.AnyArguments);
                if (LayEggDef != null && Inject != null && is_baby != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Is(OpCodes.Newobj, LayEggDef))
                        {
                            instructions.Insert(i++, TranspilerUtils.GetLoadArgInstruction(is_baby.Position));
                            instructions.Insert(i++, new CodeInstruction(OpCodes.Call, Inject));
                            return true;
                        }
                    }
                }
                return false;
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
                var creatures = this.ranch.cavity.creatures;
            +++ creatures = GetOrderedCreatureList(creatures, this);
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
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

        // чтобы рыбалка работала, 
        // при проверке допустимости жеготного сравнивать его пещеру с пещерой точки призыва, а не комнатой самой постройки
        [HarmonyPatch]
        private static class RanchStation_CanRanchableBeRanchedAtRanchStation
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                var smi_type = typeof(RanchStation.Instance);
                var methods = new List<MethodBase>();
                var CanBeRanched = smi_type.GetMethodSafe("CanRanchableBeRanchedAtRanchStation", false, typeof(RanchableMonitor.Instance));
                if (CanBeRanched != null)
                    methods.Add(CanBeRanched);
                else
                    PUtil.LogWarning("Method not found 'RanchStation.Instance.CanRanchableBeRanchedAtRanchStation'");
                var GetCavityInfo = smi_type.GetMethodSafe(nameof(RanchStation.Instance.GetCavityInfo), false);
                if (GetCavityInfo != null)
                    methods.Add(GetCavityInfo);
                else
                    PUtil.LogWarning("Method not found 'RanchStation.Instance.GetCavityInfo'");
                return methods;
            }
            private static CavityInfo GetFishingCavity(CavityInfo cavity, RanchStation.Instance smi)
            {
                if (!smi.IsNullOrStopped() && smi.gameObject.TryGetComponent<FishingStationGuide>(out var fishingStation))
                {
                    return Game.Instance.roomProber.GetCavityForCell(fishingStation.TargetRanchCell);
                }
                return cavity;
            }
            /*  RanchStation.Instance.CanRanchableBeRanchedAtRanchStation:

                int cell = Grid.PosToCell(ranchable.transform.GetPosition());
				var cavityForCell = Game.Instance.roomProber.GetCavityForCell(cell);
				bool flag = cavityForCell == null 
            ---     || cavityForCell != this.ranch.cavity;
            +++     || cavityForCell != GetFishingCavity(this.ranch.cavity, this);

                RanchStation.Instance.GetCavityInfo:

            --- return this.ranch.cavity;
            +++ return GetFishingCavity(this.ranch.cavity, this);
            */

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var cavity = typeof(Room).GetField(nameof(Room.cavity));
                var GetFishingCavity = typeof(RanchStation_CanRanchableBeRanchedAtRanchStation).GetMethodSafe(nameof(RanchStation_CanRanchableBeRanchedAtRanchStation.GetFishingCavity), true, PPatchTools.AnyArguments);
                if (cavity != null && GetFishingCavity != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].LoadsField(cavity))
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, GetFishingCavity));
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
                if (!__instance.IsNullOrStopped() && __instance.gameObject.TryGetComponent<FishingStationGuide>(out var fishingStation)
                    && Grid.IsValidCell(fishingStation.TargetRanchCell))
                {
                    Grid.CellToXY(fishingStation.TargetRanchCell, out _, out int y);
                    __result.y = y;
                }
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

        // для устранения конфликта с модом "Rooms Expanded" с комнатой "аквариум"
        // детектим, модифицируем комнату "ранчо" чтобы она могла быть обгрейднутна до "аквариума"
        // применяем патч для ранчстанций
        public static bool RoomsExpandedFound { get; private set; } = false;
        public static RoomType AquariumRoom { get; private set; }
        private static void RoomsExpandedCompat(Harmony harmony)
        {
            var db = Db.Get();
            var RoomsExpanded = PPatchTools.GetTypeSafe("RoomsExpanded.RoomTypes_AllModded");
            if (RoomsExpanded != null)
            {
                PUtil.LogDebug("RoomsExpanded found. Attempt to add compatibility.");
                try
                {
                    AquariumRoom = (RoomType)RoomsExpanded.GetPropertySafe<RoomType>("Aquarium", true)?.GetValue(null);
                    if (AquariumRoom != null)
                    {
                        var upgrade_paths = db.RoomTypes.CreaturePen.upgrade_paths.AddToArray(AquariumRoom);
                        Traverse.Create(db.RoomTypes.CreaturePen).Property(nameof(RoomType.upgrade_paths)).SetValue(upgrade_paths);
                        var priority = Math.Max(db.RoomTypes.CreaturePen.priority, AquariumRoom.priority);
                        Traverse.Create(AquariumRoom).Property(nameof(RoomType.priority)).SetValue(priority);
                        RoomsExpandedFound = true;
                    }
                }
                catch (Exception e)
                {
                    PUtil.LogExcWarn(e);
                }
            }
            if (RoomsExpandedFound)
            {
                harmony.PatchTranspile(typeof(RanchStation.Instance), "OnRoomUpdated",
                    new HarmonyMethod(typeof(RanchStation_OnRoomUpdated), nameof(RanchStation_OnRoomUpdated.TranspilerCompat)));
            }
        }

        // заменить жесткокодированую проверку типа комнату "ранчо"
        // на комнату, заданную в роомтракере - для совместимости с "роом эхпандед"
        //[HarmonyPatch(typeof(RanchStation.Instance), "OnRoomUpdated")]
        private static class RanchStation_OnRoomUpdated
        {
            private static RoomType GetTrueRoomType(RoomType type, RanchStation.Instance smi)
            {
                if (smi.PrefabID() == FishingStationConfig.ID)
                {
                    var roomtype = smi.GetComponent<RoomTracker>()?.requiredRoomType;
                    if (string.IsNullOrEmpty(roomtype))
                        return type;
                    return Db.Get().RoomTypes.TryGet(roomtype) ?? type;
                }
                return type;
            }

            internal static IEnumerable<CodeInstruction> TranspilerCompat(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var CreaturePen = typeof(RoomTypes).GetField(nameof(RoomTypes.CreaturePen));
                var GetTrueRoomType = typeof(RanchStation_OnRoomUpdated).GetMethodSafe(nameof(RanchStation_OnRoomUpdated.GetTrueRoomType), true, PPatchTools.AnyArguments);
                if (CreaturePen != null && GetTrueRoomType != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].LoadsField(CreaturePen))
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, GetTrueRoomType));
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
