using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using Klei.AI;
using STRINGS;
using UnityEngine;
using SanchozzONIMods.Lib;
using System.Reflection;
using System.Reflection.Emit;

namespace ButcherStation
{
    class ButcherStationPatches
    {
        /*
                public static class Mod_OnLoad
                {
                    public static void OnLoad()
                    {
                    }
                }
        */

        public static AttributeConverter RanchingEffectExtraMeat;

        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public class GeneratedBuildings_LoadGeneratedBuildings
        {
            public static void Prefix()
            {
                Utils.AddBuildingToPlanScreen("Equipment", FishingStationConfig.ID, "ShearingStation");
                Utils.AddBuildingToPlanScreen("Equipment", ButcherStationConfig.ID, "ShearingStation");
            }
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        public class Db_Initialize
        {
            public static void Prefix()
            {
                Utils.AddBuildingToTechnology("AnimalControl", ButcherStationConfig.ID);
                Utils.AddBuildingToTechnology("AnimalControl", FishingStationConfig.ID);
            }

            public static void Postfix(Db __instance)
            {
                ToPercentAttributeFormatter formatter = new ToPercentAttributeFormatter(1f, GameUtil.TimeSlice.None);
                RanchingEffectExtraMeat = __instance.AttributeConverters.Create("RanchingEffectExtraMeat", "Ranching Effect Extra Meat", STRINGS.DUPLICANTS.ATTRIBUTES.RANCHING.EFFECTEXTRAMEATMODIFIER, Db.Get().Attributes.Ranching, Config.Get().EXTRAMEATPERRANCHINGATTRIBUTE, 0f, formatter);
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        public static class Localization_Initialize
        {
            public static void Postfix()
            {
                Utils.InitLocalization(typeof(STRINGS));
                LocString.CreateLocStringKeys(typeof(STRINGS.BUILDING));
                LocString.CreateLocStringKeys(typeof(STRINGS.BUILDINGS));
                LocString.CreateLocStringKeys(typeof(STRINGS.UI));
                Strings.Add($"STRINGS.MISC.TAGS.{ButcherStation.ButcherableCreature.ToString().ToUpperInvariant()}", MISC.TAGS.BAGABLECREATURE);
                Strings.Add($"STRINGS.MISC.TAGS.{ButcherStation.FisherableCreature.ToString().ToUpperInvariant()}", MISC.TAGS.SWIMMINGCREATURE);

                Config.Initialize();
            }
        }

        // хак для того чтобы отобразить заголовок окна
        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        public static class DetailsScreen_OnPrefabInit
        {
            public static void Prefix(List<DetailsScreen.SideScreenRef> ___sideScreens)
            {
                DetailsScreen.SideScreenRef sideScreen;
                for (int i = 0; i < ___sideScreens.Count; i++)
                {
                    if (___sideScreens[i].name == "IntSliderSideScreen")
                    {
                        sideScreen = ___sideScreens[i];
                        ___sideScreens.RemoveAt(i);
                        for (int j = 0; j < ___sideScreens.Count; j++)
                        {
                            if (___sideScreens[j].name == "SingleCheckboxSideScreen")
                            {
                                ___sideScreens.Insert(j + 1, sideScreen);
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }

        // добавляем тэги для убиваемых животных и дополнительное мясо
        [HarmonyPatch(typeof(EntityTemplates), "ExtendEntityToBasicCreature")]
        public class EntityTemplates_ExtendEntityToBasicCreature
        {
            public static void Postfix(GameObject __result, string onDeathDropID, int onDeathDropCount)
            {
                if (onDeathDropCount > 0 && (onDeathDropID == "Meat" || onDeathDropID == "FishMeat"))
                {
                    ExtraMeatSpawner extraMeatSpawner = __result.AddOrGet<ExtraMeatSpawner>();
                    extraMeatSpawner.onDeathDropID = onDeathDropID;
                    extraMeatSpawner.onDeathDropCount = onDeathDropCount;
                }
                __result.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject inst)
                {
                    if (inst.GetDef<RanchableMonitor.Def>() != null)
                    {
                        Tag creatureEligibleTag;
                        KPrefabID creature_prefab_id = inst.GetComponent<KPrefabID>();
                        if (creature_prefab_id.HasTag(GameTags.SwimmingCreature))
                        {
                            creatureEligibleTag = ButcherStation.FisherableCreature;
                        }
                        else
                        {
                            creatureEligibleTag = ButcherStation.ButcherableCreature;
                        }
                        creature_prefab_id.AddTag(creatureEligibleTag, false);
#if VANILLA
                        WorldInventory.Instance.Discover(creature_prefab_id.PrefabTag, creatureEligibleTag);
#endif
                    }
                };
            }
        }

        [HarmonyPatch(typeof(EntityTemplates), "ExtendEntityToFertileCreature")]
        static class EntityTemplates_ExtendEntityToFertileCreature
        {
            public static void Postfix(GameObject __result, bool add_fish_overcrowding_monitor)
            {
                if (add_fish_overcrowding_monitor)
                {
                    __result.AddOrGetDef<RanchableMonitor.Def>();
                }
            }
        }

        // хак чтобы сделать рыб приручаемыми - чтобы ловились на рыбалке
        /*
         ChoreTable.Builder chore_table = new ChoreTable.Builder().Add
         blablabla
         .PushInterruptGroup()
    +++  .Add(new RanchedStates.Def(), true)
         .Add(new FixedCaptureStates.Def(), true)
         blablabla
         */
        [HarmonyPatch(typeof(BasePacuConfig), "CreatePrefab")]
        static class BasePacuConfig_CreatePrefab
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction instruction = instructionsList[i];
                    yield return instruction;
                    if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == typeof(ChoreTable.Builder).GetMethod("PushInterruptGroup", new Type[] { }))
                    {
                        Debug.Log("BasePacuConfig CreatePrefab Transpiler injected");
                        yield return new CodeInstruction(OpCodes.Newobj, typeof(RanchedStates.Def).GetConstructors()[0]);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return new CodeInstruction(OpCodes.Callvirt, typeof(ChoreTable.Builder).GetMethod("Add", new Type[] { typeof(StateMachine.BaseDef), typeof(bool) }));
                    }
                }
            }
        }

        // хак, чтобы ранчо-станции проверяли допустимость комнаты по главной клетке постройки
        // а не по конечной клетке для приручения жеготного. иначе рыбалка не работает
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
        */
        [HarmonyPatch(typeof(RanchStation.Instance), "FindRanchable")]
        static class RanchStation_Instance_FindRanchable
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool flag1 = true;
                bool flag2 = true;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction instruction = instructionsList[i];
                    if (flag1 && instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == typeof(RanchStation.Instance).GetMethod("GetTargetRanchCell"))
                    {
                        Debug.Log("RanchStation.Instance FindRanchable Transpiler #1 injected");
                        yield return new CodeInstruction(OpCodes.Call, typeof(Grid).GetMethod("PosToCell", new Type[] { typeof(StateMachine.Instance) }));
                        flag1 = false;
                    }
                    else if (flag2 && instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == typeof(RanchStation.Instance).GetMethod("get_targetRanchable"))
                    {
                        Debug.Log("RanchStation.Instance FindRanchable Transpiler #2 injected");
                        yield return new CodeInstruction(OpCodes.Call, typeof(RanchStation.Instance).GetMethod("GetTargetRanchCell", new Type[] { }));
                        yield return new CodeInstruction(OpCodes.Stloc_0);
                        yield return new CodeInstruction(OpCodes.Call, typeof(Game).GetMethod("get_Instance", new Type[] { }));
                        yield return new CodeInstruction(OpCodes.Ldfld, typeof(Game).GetField("roomProber"));
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return new CodeInstruction(OpCodes.Callvirt, typeof(RoomProber).GetMethod("GetCavityForCell", new Type[] { typeof(int) }));
                        yield return new CodeInstruction(OpCodes.Stloc_1);
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return instruction;
                        flag2 = false;
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }
        }

        // фикс для правильного подсчета рыбы в точке доставки
#if VANILLA
        /*
    --- int cell = Grid.PosToCell(this);
    +++ int cell = Grid.OffsetCell( Grid.PosToCell(this), spawnOffset );
        */
        [HarmonyPatch(typeof(CreatureDeliveryPoint), "RefreshCreatureCount")]
        static class CreatureDeliveryPoint_RefreshCreatureCount
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction instruction = instructionsList[i];
                    yield return instruction;
                    if (instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == typeof(Grid).GetMethod("PosToCell", new Type[] { typeof(KMonoBehaviour) }) )
                    {
                        //Start of injection
                        Debug.Log("CreatureDeliveryPoint RefreshCreatureCount Transpiler injected");
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld, typeof(CreatureDeliveryPoint).GetField("spawnOffset"));
                        yield return new CodeInstruction(OpCodes.Call, typeof(Grid).GetMethod("OffsetCell", new Type[] { typeof(int), typeof(CellOffset) }) );
                    }
                }
            }
        }
#endif

        // для замены максимума жеготных
        // todo: добавить проверку наличия другого мода для этой же опции
        [HarmonyPatch(typeof(CreatureDeliveryPoint), "IUserControlledCapacity.get_MaxCapacity")]
        static class CreatureDeliveryPoint_get_MaxCapacity
        {
            static bool Prefix(ref float __result)
            {
                __result = Config.Get().MAXCREATURELIMIT;
                return false;
            }
        }
    }
}
