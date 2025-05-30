using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace SquirrelGenerator
{
    internal sealed class SquirrelGeneratorPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(SquirrelGeneratorPatches));
            new POptions().RegisterOptions(this, typeof(SquirrelGeneratorOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            Utils.AddBuildingToPlanScreen(BUILD_CATEGORY.Power, SquirrelGeneratorConfig.ID, BUILD_SUBCATEGORY.generators, ManualGeneratorConfig.ID);
            Utils.AddBuildingToTechnology("Ranching", SquirrelGeneratorConfig.ID);
            GameTags.MaterialBuildingElements.Add(GameTags.Seed);
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
            /*
            ChoreTable.Builder chore_table = new ChoreTable.Builder().Add(new DeathStates.Def()).Add(new AnimInterruptStates.Def())
                <блаблабла>
                .Add(new CallAdultStates.Def())
                .Add(new SeedPlantingStates.Def(блабла))
        +++     .PushInterruptGroup()
        +++     .Add(new WheelRunningStates.Def())
        +++     .PopInterruptGroup()
                .PopInterruptGroup()
                .Add(new IdleStates.Def());
            */
            private static ChoreTable.Builder Inject(ChoreTable.Builder builder)
            {
                return builder.PushInterruptGroup().Add(new WheelRunningStates.Def()).PopInterruptGroup();
            }

            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Transpile(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var pop = typeof(ChoreTable.Builder).GetMethodSafe(nameof(ChoreTable.Builder.PopInterruptGroup), false, PPatchTools.AnyArguments);
                var inject = typeof(BaseSquirrelConfig_BaseSquirrel).GetMethodSafe(nameof(Inject), true, PPatchTools.AnyArguments);
                if (pop != null && inject != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(pop))
                        {
                            instructions.Insert(i++, new CodeInstruction(OpCodes.Call, inject));
                            return true;
                        }
                    }
                }
                return false;
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
                return TranspilerUtils.Transpile(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
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
