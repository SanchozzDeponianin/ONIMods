using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Klei.AI;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace ButcherStation
{
    internal sealed class ButcherStationPatches : KMod.UserMod2
    {
        public static AttributeConverter RanchingEffectExtraMeat;

        [HarmonyPatch(typeof(Db), "Initialize")]
        internal static class Db_Initialize
        {
            private static void Postfix(Db __instance)
            {
                Utils.AddBuildingToPlanScreen("Equipment", FishingStationConfig.ID, "ShearingStation");
                Utils.AddBuildingToPlanScreen("Equipment", ButcherStationConfig.ID, "ShearingStation");
                Utils.AddBuildingToTechnology("AnimalControl", ButcherStationConfig.ID, FishingStationConfig.ID);

                var formatter = new ToPercentAttributeFormatter(1f, GameUtil.TimeSlice.None);
                RanchingEffectExtraMeat = __instance.AttributeConverters.Create(
                    id: "RanchingEffectExtraMeat", 
                    name: "Ranching Effect Extra Meat", 
                    description: STRINGS.DUPLICANTS.ATTRIBUTES.RANCHING.EFFECTEXTRAMEATMODIFIER, 
                    attribute: Db.Get().Attributes.Ranching, multiplier: Config.Get().EXTRAMEATPERRANCHINGATTRIBUTE, 
                    base_value: 0f, 
                    formatter: formatter, 
                    available_dlcs: DlcManager.AVAILABLE_ALL_VERSIONS);
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        internal static class Localization_Initialize
        {
            private static void Postfix()
            {
                Utils.InitLocalization(typeof(STRINGS));
                Config.Initialize();
            }
        }

        // хаки для того чтобы отобразить заголовок и начинку бокового окна в правильном порядке
        // на длц переопределяем GetSideScreenSortOrder
        // ёбаный холодец. какого хрена крысиного этот патч сдесь крашится на линухе, но если его вынести в отдельный мелкий мод, то не крашится.
        [HarmonyPatch(typeof(SideScreenContent), nameof(SideScreenContent.GetSideScreenSortOrder))]
        internal static class SideScreenContent_GetSideScreenSortOrder
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
        }

        // добавляем тэги для убиваемых животных и дополнительное мясо
        [HarmonyPatch(typeof(EntityTemplates), nameof(EntityTemplates.ExtendEntityToBasicCreature))]
        internal static class EntityTemplates_ExtendEntityToBasicCreature
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
        internal static class BasePacuConfig_CreatePrefab
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
        [HarmonyPatch(typeof(RanchStation.Instance), nameof(RanchStation.Instance.FindRanchable))]
        internal static class RanchStation_Instance_FindRanchable
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
                blablabla {
                    return;
                }
            +++ targetRanchCell = this.GetTargetRanchCell();
            +++ cavityForCell = Game.Instance.roomProber.GetCavityForCell(targetRanchCell);
                if (this.targetRanchable 
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
                var GetCavityForCell = typeof(RanchStation_Instance_FindRanchable).GetMethod(nameof(RanchStation_Instance_FindRanchable.GetCavityForCell), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                var GetOrderedCreatureList = typeof(RanchStation_Instance_FindRanchable).GetMethod(nameof(RanchStation_Instance_FindRanchable.GetOrderedCreatureList), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

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
                        else if (flag3 && (instruction.opcode == OpCodes.Ldfld) && (instructionsList[i+1].opcode == OpCodes.Callvirt) && (instruction.operand is FieldInfo info3) && info3 == creatures)
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
        }

        // хак, чтобы пропустить телодвижения жеготного после "ухаживания", чтобы сразу помирало
        [HarmonyPatch(typeof(RanchedStates), nameof(RanchedStates.InitializeStates))]
        internal static class RanchedStates_InitializeStates
        {
            private static void Postfix(RanchedStates.State ___runaway, RanchedStates.State ___behaviourcomplete)
            {
                ___runaway
                    .TagTransition(GameTags.Creatures.Die, ___behaviourcomplete)
                    .TagTransition(GameTags.Dead, ___behaviourcomplete);
            }
        }

        // фикс для правильного подсчета рыбы в точке доставки
        // todo: добавить проверку наличия другого мода для этой же опции
        [HarmonyPatch(typeof(CreatureDeliveryPoint), "RefreshCreatureCount")]
        internal static class CreatureDeliveryPoint_RefreshCreatureCount
        {
            /*
            --- int cell = Grid.PosToCell(this);
            +++ int cell = Grid.OffsetCell( Grid.PosToCell(this), spawnOffset );
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var PosToCell = typeof(Grid).GetMethod(nameof(Grid.PosToCell), new Type[] { typeof(KMonoBehaviour) });
                var spawnOffset = typeof(CreatureDeliveryPoint).GetField(nameof(CreatureDeliveryPoint.spawnOffset));
                var OffsetCell = typeof(Grid).GetMethod(nameof(Grid.OffsetCell), new Type[] { typeof(int), typeof(CellOffset) });

                bool result = false;
                if (PosToCell != null && spawnOffset != null && OffsetCell != null)
                {
                    for (int i = 0; i < instructionsList.Count; i++)
                    {
                        var instruction = instructionsList[i];
                        if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info) && info == PosToCell)
                        {
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Ldfld, spawnOffset));
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Call, OffsetCell));
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

        // для замены максимума жеготных
        // todo: добавить проверку наличия другого мода для этой же опции
        [HarmonyPatch(typeof(CreatureDeliveryPoint), "IUserControlledCapacity.get_MaxCapacity")]
        internal static class CreatureDeliveryPoint_get_MaxCapacity
        {
            private static bool Prefix(ref float __result)
            {
                __result = Config.Get().MAXCREATURELIMIT;
                return false;
            }
        }
    }
}
