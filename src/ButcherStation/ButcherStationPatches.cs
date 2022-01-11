using System;
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
    internal sealed class ButcherStationPatches : KMod.UserMod2
    {
        private static Harmony harmony;
        public static AttributeConverter RanchingEffectExtraMeat;

        public override void OnLoad(Harmony harmony)
        {
            ButcherStationPatches.harmony = harmony;
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(ButcherStationPatches));
            new POptions().RegisterOptions(this, typeof(ButcherStationOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuildingsAndModifier()
        {
            Utils.AddBuildingToPlanScreen("Equipment", FishingStationConfig.ID, ShearingStationConfig.ID);
            Utils.AddBuildingToPlanScreen("Equipment", ButcherStationConfig.ID, ShearingStationConfig.ID);
            Utils.AddBuildingToTechnology("AnimalControl", ButcherStationConfig.ID, FishingStationConfig.ID);

            var formatter = new ToPercentAttributeFormatter(1f, GameUtil.TimeSlice.None);
            RanchingEffectExtraMeat = Db.Get().AttributeConverters.Create(
                id: "RanchingEffectExtraMeat",
                name: "Ranching Effect Extra Meat",
                description: STRINGS.DUPLICANTS.ATTRIBUTES.RANCHING.EFFECTEXTRAMEATMODIFIER,
                attribute: Db.Get().Attributes.Ranching,
                multiplier: ButcherStationOptions.Instance.extra_meat_per_ranching_attribute / 100f,
                base_value: 0f,
                formatter: formatter,
                available_dlcs: DlcManager.AVAILABLE_ALL_VERSIONS);

            RoomsExpandedCompat();
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            RanchingEffectExtraMeat.multiplier = ButcherStationOptions.Instance.extra_meat_per_ranching_attribute / 100f;
        }

        // хаки для того чтобы отобразить заголовок и начинку бокового окна в правильном порядке
        // на длц переопределяем GetSideScreenSortOrder
        // ёбаный холодец. какого хрена крысиного этот патч сдесь крашится на линухе, но если его вынести в отдельный мелкий мод, то не крашится.
        /*
        [HarmonyPatch(typeof(SideScreenContent), nameof(SideScreenContent.GetSideScreenSortOrder))]
        private static class SideScreenContent_GetSideScreenSortOrder
        {
            private static bool Prepare()
            {
                return Environment.OSVersion.Platform.Equals(PlatformID.Win32NT);
            }

            private static void Postfix(SideScreenContent __instance, ref int __result)
            {
                switch (__instance)
                {
                    case IntSliderSideScreen _:
                        __result += 30;
                        break;
                    case SingleCheckboxSideScreen _:
                        __result += 20;
                        break;
                    case CapacityControlSideScreen _:
                        __result += 10;
                        break;
                }
            }
        }*/

        // добавление сидескреена
        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        private static class DetailsScreen_OnPrefabInit
        {
            private static void Postfix()
            {
                PUIUtils.AddSideScreenContent<ButcherStationSideScreen>();
            }
        }

        // добавляем тэги для убиваемых животных и дополнительное мясо
        [HarmonyPatch(typeof(EntityTemplates), nameof(EntityTemplates.ExtendEntityToBasicCreature))]
        private static class EntityTemplates_ExtendEntityToBasicCreature
        {
            private static void Postfix(GameObject __result, string onDeathDropID, int onDeathDropCount)
            {
                if (onDeathDropCount > 0 && (onDeathDropID == MeatConfig.ID || onDeathDropID == FishMeatConfig.ID))
                {
                    var extraMeatSpawner = __result.AddOrGet<ExtraMeatSpawner>();
                    extraMeatSpawner.onDeathDropID = onDeathDropID;
                    extraMeatSpawner.onDeathDropCount = onDeathDropCount;
                }
                __result.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject inst)
                {
                    if (inst.GetDef<RanchableMonitor.Def>() != null)
                    {
                        var prefabID = inst.GetComponent<KPrefabID>();
                        Tag creatureEligibleTag = prefabID.HasTag(GameTags.SwimmingCreature) ? ButcherStation.FisherableCreature : ButcherStation.ButcherableCreature;
                        prefabID.AddTag(creatureEligibleTag);
                        DiscoveredResources.Instance.Discover(prefabID.PrefabTag, creatureEligibleTag);
                    }
                };
            }
        }

        // хак чтобы сделать рыб приручаемыми - чтобы ловились на рыбалке
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

            private static ChoreTable.Builder Inject(ChoreTable.Builder builder)
            {
                return builder.Add(new RanchedStates.Def());
            }
            /*
                ChoreTable.Builder chore_table = new ChoreTable.Builder().Add
                blablabla
                .PushInterruptGroup()
           +++  .Add(new RanchedStates.Def(), true)
                .Add(new FixedCaptureStates.Def(), true)
                blablabla
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var PushInterruptGroup = typeof(ChoreTable.Builder).GetMethod(nameof(ChoreTable.Builder.PushInterruptGroup));
                var Inject = typeof(BasePacuConfig_CreatePrefab).GetMethod(nameof(BasePacuConfig_CreatePrefab.Inject), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                bool result = false;
                if (PushInterruptGroup != null && Inject != null)
                {
                    for (int i = 0; i < instructionsList.Count; i++)
                    {
                        var instruction = instructionsList[i];
                        if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info) && PushInterruptGroup == info)
                        {
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Call, Inject));
                            result = true;
#if DEBUG
                            Debug.Log($"'{methodName}' Transpiler injected");
#endif
                        }
                    }
                }
                if (!result)
                {
                    Debug.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }
        }

        // хак, чтобы ранчо-станции проверяли допустимость комнаты по главной клетке постройки
        // а не по конечной клетке для приручения жеготного. иначе рыбалка не работает
        // хак, чтобы убивать в первую очередь совсем лишних, затем старых, затем просто лишних.
        // хак, чтобы заменить жесткокодированую проверку типа комнату "ранчо" на комнату, заданную в роомтракере - для совместимости с "роом эхпандед"
        [HarmonyPatch(typeof(RanchStation.Instance), nameof(RanchStation.Instance.FindRanchable))]
        private static class RanchStation_Instance_FindRanchable
        {
            private static CavityInfo GetCavityForCell(int cell)
            {
                return Game.Instance.roomProber.GetCavityForCell(cell);
            }

            private static List<KPrefabID> GetOrderedCreatureList(List<KPrefabID> creatures, RanchStation.Instance instance)
            {
                if (creatures.Count > 0)
                {
                    var butcherStation = instance.GetComponent<ButcherStation>();
                    if (butcherStation != null)
                    {
                        butcherStation.RefreshCreatures(creatures);
                        return butcherStation.Creatures;
                    }
                }
                return creatures;
            }
            /*
            --- int targetRanchCell = this.GetTargetRanchCell();
            +++ int targetRanchCell = Grid.PosToCell(this);
                CavityInfo cavityForCell = Game.Instance.roomProber.GetCavityForCell(targetRanchCell);
                if (cavityForCell blablabla || blablabla
            ---     || cavityForCell.room.roomType != Db.Get().RoomTypes.CreaturePen)
            +++     || cavityForCell.room.roomType != GetTrueRoomType(Db.Get().RoomTypes.CreaturePen, this)
                {
                    blablabla
                    return;
                }
            +++ targetRanchCell = this.GetTargetRanchCell();
            +++ cavityForCell = Game.Instance.roomProber.GetCavityForCell(targetRanchCell);
                if (this.targetRanchable blablabla)
                blablabla
                blablabla
            --- foreach (KPrefabID creature in cavity_info.creatures)
            +++ foreach (KPrefabID creature in GetOrderedCreatureList(cavity_info.creatures, this))
                { blablabla
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var GetTargetRanchCell = typeof(RanchStation.Instance).GetMethod(nameof(RanchStation.Instance.GetTargetRanchCell));
                var PosToCell = typeof(Grid).GetMethod(nameof(Grid.PosToCell), new Type[] { typeof(StateMachine.Instance) });
                var get_targetRanchable = typeof(RanchStation.Instance).GetProperty(nameof(RanchStation.Instance.targetRanchable)).GetGetMethod(true);
                var creatures = typeof(CavityInfo).GetField(nameof(CavityInfo.creatures));
                var GetCavityForCell = typeof(RanchStation_Instance_FindRanchable).GetMethodSafe(nameof(RanchStation_Instance_FindRanchable.GetCavityForCell), true, PPatchTools.AnyArguments);
                var GetOrderedCreatureList = typeof(RanchStation_Instance_FindRanchable).GetMethodSafe(nameof(RanchStation_Instance_FindRanchable.GetOrderedCreatureList), true, PPatchTools.AnyArguments);

                bool flag1 = true;
                bool flag2 = true;
                bool flag3 = true;
                if (GetTargetRanchCell != null && PosToCell != null && get_targetRanchable != null && creatures != null && GetCavityForCell != null && GetOrderedCreatureList != null)
                {
                    for (int i = 0; i < instructionsList.Count; i++)
                    {
                        var instruction = instructionsList[i];
                        if (flag1 && ((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info1) && info1 == GetTargetRanchCell)
                        {

                            instructionsList[i] = new CodeInstruction(OpCodes.Call, PosToCell);
                            flag1 = false;
#if DEBUG
                            Debug.Log($"'{methodName}' Transpiler #1 injected");
#endif
                        }
                        else if (flag2 && ((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info2) && info2 == get_targetRanchable)
                        {
                            instructionsList.Insert(i, new CodeInstruction(OpCodes.Call, GetTargetRanchCell));
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Stloc_0));
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Ldloc_0));
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Call, GetCavityForCell));
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Stloc_1));
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            flag2 = false;
#if DEBUG
                            Debug.Log($"'{methodName}' Transpiler #2 injected");
#endif
                        }
                        else if (flag3 && (instruction.opcode == OpCodes.Ldfld) && (instructionsList[i + 1].opcode == OpCodes.Callvirt) && (instruction.operand is FieldInfo info3) && info3 == creatures)
                        {
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Call, GetOrderedCreatureList));
                            flag3 = false;
#if DEBUG
                            Debug.Log($"'{methodName}' Transpiler #3 injected");
#endif
                        }
                    }
                }
                if (flag1 || flag2 || flag3)
                {
                    Debug.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }

            private static RoomType GetTrueRoomType(RoomType type, RanchStation.Instance instance)
            {
                var roomtype = instance?.GetComponent<RoomTracker>()?.requiredRoomType;
                if (string.IsNullOrEmpty(roomtype))
                    return type;
                return Db.Get().RoomTypes.TryGet(roomtype) ?? type;
            }

            internal static IEnumerable<CodeInstruction> TranspilerCompat(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var CreaturePen = typeof(RoomTypes).GetField(nameof(RoomTypes.CreaturePen));
                var GetTrueRoomType = typeof(RanchStation_Instance_FindRanchable).GetMethodSafe(nameof(RanchStation_Instance_FindRanchable.GetTrueRoomType), true, PPatchTools.AnyArguments);

                bool flag4 = true;
                if (CreaturePen != null && GetTrueRoomType != null)
                {
                    for (int i = 0; i < instructionsList.Count; i++)
                    {
                        var instruction = instructionsList[i];
                        if (flag4 && (instruction.opcode == OpCodes.Ldfld) && (instruction.operand is FieldInfo info4) && info4 == CreaturePen)
                        {
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Call, GetTrueRoomType));
                            flag4 = false;
#if DEBUG
                            Debug.Log($"'{methodName}' Transpiler #4 injected");
#endif
                            break;
                        }
                    }
                }
                if (flag4)
                {
                    Debug.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }
        }

        // хак, чтобы пропустить телодвижения жеготного после "ухаживания", чтобы сразу помирало
        [HarmonyPatch(typeof(RanchedStates), nameof(RanchedStates.InitializeStates))]
        private static class RanchedStates_InitializeStates
        {
            private static void Postfix(RanchedStates.State ___runaway, RanchedStates.State ___behaviourcomplete)
            {
                ___runaway
                    .TagTransition(GameTags.Creatures.Bagged, ___behaviourcomplete)
                    .TagTransition(GameTags.Creatures.Die, ___behaviourcomplete)
                    .TagTransition(GameTags.Dead, ___behaviourcomplete);
            }
        }

        // чисто косметика - подмена анимации пойманого жеготного - ради отловленной живой рыбы
        [HarmonyPatch(typeof(BaggedStates), nameof(BaggedStates.InitializeStates))]
        private static class BaggedStates_InitializeStates
        {
            private static GameStateMachine<BaggedStates, BaggedStates.Instance, IStateMachineTarget, BaggedStates.Def>.State PlayAnimStub(GameStateMachine<BaggedStates, BaggedStates.Instance, IStateMachineTarget, BaggedStates.Def>.State @this, string _1, KAnim.PlayMode _2)
            {
                return @this;
            }
            private static string ChooseBaggedAnim(BaggedStates.Instance smi)
            {
                return smi.HasTag(GameTags.SwimmingCreature) ? "flop_loop" : "trussed";
            }
            private static void Postfix(BaggedStates __instance)
            {
                __instance.bagged.PlayAnim(ChooseBaggedAnim, KAnim.PlayMode.Loop);
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var PlayAnim = typeof(GameStateMachine<BaggedStates, BaggedStates.Instance, IStateMachineTarget, BaggedStates.Def>.State).GetMethodSafe("PlayAnim", false, typeof(string), typeof(KAnim.PlayMode));
                var Stub = typeof(BaggedStates_InitializeStates).GetMethodSafe(nameof(PlayAnimStub), true, PPatchTools.AnyArguments);
                return PPatchTools.ReplaceMethodCall(instructions, PlayAnim, Stub);
            }
        }

        // фикс для правильного подсчета рыбы в точке доставки
        [HarmonyPatch(typeof(CreatureDeliveryPoint), "RefreshCreatureCount")]
        private static class CreatureDeliveryPoint_RefreshCreatureCount
        {
            /*
            --- int cell = Grid.PosToCell(this);
            +++ int cell = Grid.OffsetCell( Grid.PosToCell(this), spawnOffset );
            */
            private static int CorrectedCell(int cell, CreatureDeliveryPoint cdp)
            {
                int corrected = Grid.OffsetCell(cell, cdp.spawnOffset);
                return Grid.IsValidCell(corrected) ? corrected : cell;
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var PosToCell = typeof(Grid).GetMethod(nameof(Grid.PosToCell), new Type[] { typeof(KMonoBehaviour) });
                var x = typeof(CreatureDeliveryPoint_RefreshCreatureCount).GetMethodSafe(nameof(CorrectedCell), true, PPatchTools.AnyArguments);

                bool result = false;
                if (PosToCell != null && x != null)
                {
                    for (int i = 0; i < instructionsList.Count; i++)
                    {
                        var instruction = instructionsList[i];
                        if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info) && info == PosToCell)
                        {
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Call, x));
                            result = true;
#if DEBUG
                            Debug.Log($"'{methodName}' Transpiler injected");
#endif
                            break;
                        }
                    }
                }
                if (!result)
                {
                    Debug.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }
        }

        // для замены максимума жеготных
        [HarmonyPatch(typeof(CreatureDeliveryPoint), "IUserControlledCapacity.get_MaxCapacity")]
        private static class CreatureDeliveryPoint_get_MaxCapacity
        {
            private static bool Prefix(ref float __result)
            {
                __result = ButcherStationOptions.Instance.max_creature_limit;
                return false;
            }
        }

        // фикс чтобы станции правильно потребляли искричество
        // похоже на удаление гланд ректально, но тут по другому будет сложно подлезть
        // фикс зависшей анимации, когда ранчер уходит например жрать, бросив задание посередине
        [HarmonyPatch(typeof(RancherChore.RancherChoreStates), nameof(RancherChore.RancherChoreStates.InitializeStates))]
        private static class RancherChore_RancherChoreStates_InitializeStates
        {
            private static void Postfix(GameStateMachine<RancherChore.RancherChoreStates, RancherChore.RancherChoreStates.Instance, IStateMachineTarget, object>.State ___ranchcreature)
            {
                ___ranchcreature
                    .Enter(smi => smi?.ranchStation?.GetComponent<Operational>()?.SetActive(true))
                    .Exit(smi => smi?.ranchStation?.GetComponent<Operational>()?.SetActive(false))
                    .Exit(smi =>
                    {
                        var kbak = smi?.ranchStation?.GetComponent<KBatchedAnimController>();
                        if (kbak != null && kbak.PlayMode == KAnim.PlayMode.Loop)
                            kbak.PlayMode = KAnim.PlayMode.Once;
                    });
            }
        }

        // для устранения конфликта с модом "Rooms Expanded" с комнатой "аквариум"
        // детектим, модифицируем комнату "ранчо" чтобы она могла быть обгрейднутна до "аквариума"
        // применяем патч для ранчстанций
        public static bool RoomsExpandedFound { get; private set; } = false;
        public static RoomType AquariumRoom { get; private set; }
        private static void RoomsExpandedCompat()
        {
            var db = Db.Get();
            var RoomsExpanded = PPatchTools.GetTypeSafe("RoomsExpanded.RoomTypes_AllModded", "RoomsExpandedMerged");
            if (RoomsExpanded != null)
            {
                PUtil.LogDebug("RoomsExpanded found. Attempt to add compatibility.");
                try
                {
                    AquariumRoom = (RoomType)RoomsExpanded.GetPropertySafe<RoomType>("Aquarium", true)?.GetValue(null, null);
                    if (AquariumRoom != null)
                    {
                        var upgrade_paths = db.RoomTypes.CreaturePen.upgrade_paths.AddToArray(AquariumRoom);
                        Traverse.Create(db.RoomTypes.CreaturePen).Property(nameof(RoomType.upgrade_paths)).SetValue(upgrade_paths);
                        Traverse.Create(AquariumRoom).Property(nameof(RoomType.priority)).SetValue(db.RoomTypes.CreaturePen.priority);
                        RoomsExpandedFound = true;
                    }
                }
                catch (System.Exception e)
                {
                    PUtil.LogExcWarn(e);
                }
            }
            if (RoomsExpandedFound)
            {
                harmony.PatchTranspile(typeof(RanchStation.Instance), nameof(RanchStation.Instance.FindRanchable),
                    new HarmonyMethod(typeof(RanchStation_Instance_FindRanchable), nameof(RanchStation_Instance_FindRanchable.TranspilerCompat)));
            }
        }
    }
}
