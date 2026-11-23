using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using SanchozzONIMods.Shared;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace SquirrelGenerator
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            CustomLighting.Register();
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
            harmony.PatchAll();
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            Utils.AddBuildingToPlanScreen(BUILD_CATEGORY.Power, SquirrelGeneratorConfig.ID, BUILD_SUBCATEGORY.generators, ManualGeneratorConfig.ID);
            Utils.AddBuildingToTechnology("Ranching", SquirrelGeneratorConfig.ID);
            GameTags.MaterialBuildingElements.Add(GameTags.Seed);
        }

        [HarmonyPatch(typeof(Blueprints), MethodType.Constructor)]
        private static class Blueprints_Constructor
        {
            private static void Postfix(Blueprints __instance)
            {
                __instance.skinsReleaseProviders = __instance.skinsReleaseProviders.Append(new Blueprints_SquirrelWheel());
            }
        }

        // добавить белкам новое поведение
        [HarmonyPatch(typeof(BaseSquirrelConfig), nameof(BaseSquirrelConfig.BaseSquirrel))]
        private static class BaseSquirrelConfig_BaseSquirrel
        {
            internal static void Postfix(GameObject __result, bool is_baby)
            {
                if (!is_baby)
                {
                    __result.AddOrGet<WheelRunningMonitor>();
                    var choreType = __result.GetComponent<ChoreConsumer>().choreTable.GetEntry<WheelRunningStates.Def>().choreType;
                    if (choreType != null)
                        choreType.interruptPriority -= 50;
                }
#if DEBUG
                Debug.Log($"{__result.PrefabID()} ChoreTable !!!");
                Debug.Log("id\tpriority\tinterrupt_priority");
                var entries = Traverse.Create(__result.GetComponent<ChoreConsumer>().choreTable).Field<ChoreTable.Entry[]>("entries").Value;
                foreach (var entry in entries)
                    Debug.Log($"{entry.choreType.Id}\t{entry.choreType.priority}\t{entry.choreType.interruptPriority}");
#endif
            }
            // внедряем новое поведение так чтобы оно могло быть прервано другими типичными беличими делами
            // кроме проигрывания эмоцый
            /*
            ChoreTable.Builder chore_table = new ChoreTable.Builder()
                <блаблабла>
                .Add(new SeedPlantingStates.Def(блабла))
                .Add(new CritterCondoStates.Def(), блабла)
        +++     .Add(new WheelRunningStates.Def(), !is_baby)
                .Add(new CritterEmoteStates.Def(anim), блабла)
                .PopInterruptGroup()
                .Add(new IdleStates.Def());
            */
            private static ChoreTable.Builder Inject(ChoreTable.Builder builder, bool is_baby)
            {
                return builder.Add(new WheelRunningStates.Def(), !is_baby);
            }

            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions, MethodBase original)
            {
                var is_baby = original.GetParameters().FindFirst(p => p.ParameterType == typeof(bool) && p.Name == "is_baby");
                var add = typeof(ChoreTable.Builder).GetMethodSafe(nameof(ChoreTable.Builder.Add), false,
                    typeof(StateMachine.BaseDef), typeof(bool), typeof(int));
                var emote = typeof(CritterEmoteStates.Def).GetConstructors()[0];
                var inject = typeof(BaseSquirrelConfig_BaseSquirrel).GetMethodSafe(nameof(Inject), true, PPatchTools.AnyArguments);
                if (is_baby == null || add == null || emote == null || inject == null)
                    return false;
                int i = instructions.FindIndex(inst => inst.opcode == OpCodes.Newobj && inst.operand is ConstructorInfo info && info == emote);
                if (i == -1) return false;
                int j = instructions.FindLastIndex(i, inst => inst.Calls(add));
                if (j == -1) return false;
                instructions.Insert(++j, is_baby.GetLoadArgInstruction());
                instructions.Insert(++j, new CodeInstruction(OpCodes.Call, inject));
                return true;
            }
        }

        // задача поиска и посадки семян в любом случае, даже если нет подходящих семян
        // начинает выполняться и хочет прервать бег в колесе
        // добавляем проверку - нет семян -> белка хочет бегать сразу, без паузы
        // математически это не совсем правильно, но задача поиска семян достаточно редкая, так что сойдёт
        [HarmonyPatch(typeof(SeedPlantingStates), nameof(SeedPlantingStates.InitializeStates))]
        private static class SeedPlantingStates_InitializeStates
        {
            private static void Postfix(SeedPlantingStates __instance)
            {
                __instance.findSeed.Exit(CheckSeed);
            }
            private static void CheckSeed(SeedPlantingStates.Instance smi)
            {
                if (smi.targetSeed == null)
                {
                    smi.GetSMI<WheelRunningMonitor.StatesInstance>()?.SetSearchTimeImmediately();
                    WheelRunningStates.PrioritizeUpdateBrain(smi);
                }
            }
        }

        // чтобы нельзя было строить из мутантовых семян
        [HarmonyPatch(typeof(Constructable), "OnSpawn")]
        private static class Constructable_OnSpawn
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();

            private static readonly IDetouredField<FetchOrder2, Tag[]> FORBIDDEN_TAGS
                = PDetours.DetourField<FetchOrder2, Tag[]>(nameof(FetchOrder2.ForbiddenTags));

            private static FetchList2 InjectForbiddenTag(FetchList2 fetchList, Constructable constructable)
            {
                if (constructable.PrefabID() == BuildingConfigManager.GetUnderConstructionName(SquirrelGeneratorConfig.ID))
                {
                    Tag[] forbidden_tags;
                    foreach (var fetchOrder in fetchList.FetchOrders)
                    {
                        if (fetchOrder.ForbiddenTags == null)
                            forbidden_tags = new Tag[1] { GameTags.MutatedSeed };
                        else
                            forbidden_tags = fetchOrder.ForbiddenTags.Append(GameTags.MutatedSeed);
                        FORBIDDEN_TAGS.Set(fetchOrder, forbidden_tags);
                    }
                }
                return fetchList;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var fetchList = typeof(Constructable).GetFieldSafe("fetchList", false);
                var submit = typeof(FetchList2).GetMethodSafe(nameof(FetchList2.Submit), false, typeof(System.Action), typeof(bool));
                var injectForbiddenTag = typeof(Constructable_OnSpawn).GetMethodSafe(nameof(InjectForbiddenTag), true, PPatchTools.AnyArguments);
                if (fetchList != null && submit != null && injectForbiddenTag != null)
                {
                    int j = instructions.FindIndex(inst => inst.Calls(submit));
                    if (j != -1)
                    {
                        int i = instructions.FindLastIndex(j, inst => inst.LoadsField(fetchList));
                        if (i != -1)
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, injectForbiddenTag));
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
