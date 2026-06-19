using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using STRINGS;

namespace OrehullHarvester
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            if (!DlcManager.IsContentSubscribed(DlcManager.DLC5_ID)) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new KAnimGroupManager().RegisterAnimsTogether("turtle_build_kanim", "turtle_stomping_kanim");
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
            Utils.LoadEmbeddedAudioSheet("AudioSheets/SFXTags_Creatures.csv");
            harmony.PatchAll();
        }

        // добавить черепахам новое поведение
        [HarmonyPatch(typeof(BaseSeaTurtleConfig), nameof(BaseSeaTurtleConfig.CreatePrefab))]
        private static class BaseSeaTurtleConfig_CreatePrefab
        {
            internal static void Postfix(GameObject __result, bool is_baby)
            {
                if (!is_baby)
                {
                    var def = __result.AddOrGetDef<StompMonitor.Def>();
                    def.Cooldown = 60f;
                    def.radius = 10;
                    var kbac = __result.GetComponent<KBatchedAnimController>();
                    kbac.AnimFiles = kbac.AnimFiles.Append(Assets.GetAnim("turtle_stomping_kanim"));
                }
            }
            /*
            ChoreTable.Builder chore_table = new ChoreTable.Builder().Add(new DeathStates.Def())
                <блаблабла>
    			.Add(new LayEggStates.Def(), !is_baby, -1)
            +++ .Add(new StompStates.Def(), !is_baby, -1)
			    .Add(new EatStates.Def(), true, -1)
                <блаблабла>
            */
            private static ChoreTable.Builder Inject(ChoreTable.Builder builder, bool is_baby)
            {
                return builder.Add(new StompStates.Def(), !is_baby);
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
                var eat = typeof(EatStates.Def).GetConstructors()[0];
                var inject = typeof(BaseSeaTurtleConfig_CreatePrefab).GetMethodSafe(nameof(Inject), true, typeof(ChoreTable.Builder), typeof(bool));

                if (add == null || eat == null || inject == null)
                    return false;

                int i = instructions.FindIndex(inst => inst.Is(OpCodes.Newobj, eat));
                if (i == -1)
                    return false;

                int j = instructions.FindLastIndex(i, inst => inst.Calls(add));
                if (j == -1)
                    return false;

                instructions.Insert(++j, is_baby.GetLoadArgInstruction());
                instructions.Insert(++j, new CodeInstruction(OpCodes.Call, inject));
                return true;
            }
        }

        private static bool IsTurtle(GameObject go)
        {
            return go != null && go.TryGetComponent(out CreatureBrain brain) && brain.species == GameTags.Creatures.Species.SeaTurtleSpecies;
        }

        // доставать до растений у правой стены
        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            foreach (var go in Assets.GetPrefabsWithComponent<CreatureBrain>())
            {
                if (IsTurtle(go))
                {
                    var def = go.GetDef<StompMonitor.Def>();
                    if (def != null)
                    {
                        def.PlantSeeker.SetDynamicOffsetsFn((plant, offcets)
                            => GetObjectCellsOffsetsWithExtraLeftBottomPadding(plant.gameObject, offcets));
                    }
                }
            }
        }

        private static void GetObjectCellsOffsetsWithExtraLeftBottomPadding(GameObject plant, List<CellOffset> offsets)
        {
            var area = plant.GetComponent<OccupyArea>();
            int width = area.GetWidthInCells();
            int height = area.GetHeightInCells();
            int min_x = int.MaxValue;
            int min_y = int.MaxValue;
            for (int i = 0; i < area.OccupiedCellsOffsets.Length; i++)
            {
                var offset = area.OccupiedCellsOffsets[i];
                offsets.Add(offset);
                min_x = Mathf.Min(min_x, offset.x);
                min_y = Mathf.Min(min_y, offset.y);
            }
            for (int j = 0; j < width; j++)
                offsets.Add(new CellOffset(min_x + j, min_y - 1));
            for (int k = 0; k < height; k++)
                offsets.Add(new CellOffset(min_x - 1, min_y + k));
            offsets.Add(new CellOffset(min_x - 1, min_y - 1));
        }

        [HarmonyPatch(typeof(StompStates.Instance), nameof(StompStates.Instance.SetTarget))]
        private static class StompStates_Instance_SetTarget
        {
            private static void Postfix(StompStates.Instance __instance)
            {
                if (__instance.CurrentTarget != null && IsTurtle(__instance.gameObject))
                {
                    var list = ListPool<CellOffset, StompStates.Instance>.Allocate();
                    GetObjectCellsOffsetsWithExtraLeftBottomPadding(__instance.CurrentTarget, list);
                    __instance.TargetOffsets = list.ToArray();
                    list.Recycle();
                }
            }
        }

        // не собирать закрытые устриццы
        [HarmonyPatch(typeof(StompMonitor.Def), nameof(StompMonitor.Def.IsPlantTargetCandidate))]
        private static class StompMonitor_Def_IsPlantTargetCandidate
        {
            private static void Postfix(KPrefabID plant, ref bool __result)
            {
                if (__result && plant.TryGetComponent(out ClamHarvestable clam) && clam.IsClosedAndReadyForHarvesting)
                    __result = false;
            }
        }

        // поправим статуситэмы
        [HarmonyPatch(typeof(StompStates), nameof(StompStates.GetGoingToStompStatusItem))]
        private static class StompStates_GetGoingToStompStatusItem
        {
            private static bool Prefix(StompStates.Instance smi, ref StatusItem __result)
            {
                if (IsTurtle(smi.gameObject))
                {
                    __result = StompStates.GetStatusItem(smi,
                        CREATURES.STATUSITEMS.GOING_TO_HARVEST.NAME, CREATURES.STATUSITEMS.GOING_TO_HARVEST.TOOLTIP);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(StompStates), nameof(StompStates.GetStompingStatusItem))]
        private static class StompStates_GetStompingStatusItem
        {
            private static bool Prefix(StompStates.Instance smi, ref StatusItem __result)
            {
                if (IsTurtle(smi.gameObject))
                {
                    __result = StompStates.GetStatusItem(smi,
                        CREATURES.STATUSITEMS.HARVESTING.NAME, CREATURES.STATUSITEMS.HARVESTING.TOOLTIP);
                    return false;
                }
                return true;
            }
        }

        // косметика, изза несовпадения длины анимации топотуна и черепахла
        private const float STOMP_LOOP_ANIM_DURATION = 1.83333337f;

        private static StompStates.FloatParameter stomp_loop_anim_duration;

        [HarmonyPatch(typeof(StompStates), nameof(StompStates.InitializeStates))]
        private static class StompStates_InitializeStates
        {
            private static void Postfix(StompStates __instance)
            {
                stomp_loop_anim_duration = __instance.AddParameter(nameof(stomp_loop_anim_duration),
                    new StompStates.FloatParameter(STOMP_LOOP_ANIM_DURATION));

                __instance.root.Enter(GetAnimDuration);
            }

            private static void GetAnimDuration(StompStates.Instance smi)
            {
                var kbac = smi.GetComponent<KBatchedAnimController>();
                if (kbac.HasAnimation("stomping_loop"))
                {
                    var anim = kbac.GetAnim("stomping_loop");
                    stomp_loop_anim_duration.Set(anim.numFrames / anim.frameRate, smi);
                }
            }

            // QueueAnim вместо PlayAnim
#if false
            private static StompStates.State QueueAnim(StompStates.State state, string anim, KAnim.PlayMode mode)
            {
                return state.QueueAnim(anim, mode == KAnim.PlayMode.Loop);
            }

            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions, MethodBase original)
            {
                var play = typeof(StompStates.State).GetMethodSafe(nameof(StompStates.State.PlayAnim), false, typeof(string), typeof(KAnim.PlayMode));
                var queue = typeof(StompStates_InitializeStates).GetMethodSafe(nameof(QueueAnim), true, PPatchTools.AnyArguments);
                if (play == null || queue == null)
                    return false;
                instructions = PPatchTools.ReplaceMethodCallSafe(instructions, play, queue).ToList();
                return true;
            }
#endif
        }

        [HarmonyPatch(typeof(StompStates), nameof(StompStates.StompUpdate))]
        private static class StompStates_StompUpdate
        {
            /*
            --- if (smi.StompLoopTimer <= STOMP_LOOP_ANIM_DURATION)
            +++ if (smi.StompLoopTimer <= StompLoopAnimDuration(smi))
            */
            private static float StompLoopAnimDuration(StompStates.Instance smi) => stomp_loop_anim_duration.Get(smi);

            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions, MethodBase original)
            {
                var duration = typeof(StompStates_StompUpdate).GetMethodSafe(nameof(StompLoopAnimDuration), true, typeof(StompStates.Instance));

                int i = instructions.FindIndex(instr => instr.LoadsConstant(STOMP_LOOP_ANIM_DURATION));
                if (i == -1)
                    return false;

                instructions.Insert(i++, new(OpCodes.Ldarg_0));
                instructions[i].opcode = OpCodes.Call;
                instructions[i].operand = duration;
                return true;
            }
        }
    }
}
