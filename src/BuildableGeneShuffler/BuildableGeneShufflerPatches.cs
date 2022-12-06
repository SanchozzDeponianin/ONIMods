using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using static STRINGS.DUPLICANTS.CHORES.GENESHUFFLE;

namespace BuildableGeneShuffler
{
    internal sealed class BuildableGeneShufflerPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(BuildableGeneShufflerPatches));
            new POptions().RegisterOptions(this, typeof(BuildableGeneShufflerOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuilding()
        {
            ModUtil.AddBuildingToPlanScreen("Medical", BuildableGeneShufflerConfig.ID, "wellness");
            Utils.AddBuildingToTechnology("MedicineIV", BuildableGeneShufflerConfig.ID);
            PGameUtils.CopySoundsToAnim(BuildableGeneShufflerConfig.anim, "geneshuffler_kanim");
            // создаём собственный тип поручения, который имитирует "Use Neural Vacillator"
            // но имеет приоритеты как у обычных рабочих поручений
            var db = Db.Get();
            BuildableGeneShuffler.PrepareGeneShuffler = new ChoreType(
                id: nameof(BuildableGeneShuffler.PrepareGeneShuffler),
                parent: db.ChoreTypes,
                chore_groups: new string[] { nameof(db.ChoreGroups.MedicalAid) },
                urge: "",
                name: NAME,
                status_message: STATUS,
                tooltip: TOOLTIP,
                interrupt_exclusion: new List<Tag>(0),
                implicit_priority: db.ChoreTypes.DoctorFetch.priority,
                explicit_priority: db.ChoreTypes.DoctorFetch.explicitPriority)
            { interruptPriority = db.ChoreTypes.DoctorFetch.interruptPriority };
        }

        [HarmonyPatch(typeof(GeneShufflerConfig), nameof(GeneShufflerConfig.CreatePrefab))]
        private static class GeneShufflerConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<BuildedGeneShuffler>();
                __result.AddOrGet<Deconstructable>().allowDeconstruction = false;
            }
        }

        // при разрушении нужно вернуть материалы
        [HarmonyPatch(typeof(Demolishable), "TriggerDestroy")]
        private static class Demolishable_TriggerDestroy
        {
            private static readonly IDetouredField<Demolishable, bool> destroyed =
                PDetours.DetourFieldLazy<Demolishable, bool>("destroyed");

            private static readonly Vector2I dropOffset = new Vector2I(0, 1);
            private static void Prefix(Demolishable __instance)
            {
                if (__instance != null && !destroyed.Get(__instance))
                {
                    var geneShuffler = __instance.GetComponent<GeneShuffler>();
                    if (geneShuffler != null)
                    {
                        // если калибратор не использован - нужно дропнуть зарядник
                        if (!geneShuffler.IsConsumed)
                        {
                            geneShuffler.IsConsumed = true;
                            Scenario.SpawnPrefab(Grid.PosToCell(__instance), dropOffset.x, dropOffset.y, GeneShufflerRechargeConfig.ID, Grid.SceneLayer.Front).SetActive(true);
                            PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Plus, Assets.GetPrefab(GeneShufflerRechargeConfig.ID.ToTag()).GetProperName(), __instance.transform, 1.5f, false);
                        }
                        __instance.GetComponent<BuildedGeneShuffler>()?.SpawnItemsFromConstruction();
                    }
                }
            }
        }

        // патчим массу строительных материалов для возврата при разрушении
        // немного хакновато, и затрагивает все постройки, но ладно
        // просто лень городить собственный спавн всех материалов
        [HarmonyPatch(typeof(Deconstructable), nameof(Deconstructable.SpawnItemsFromConstruction), typeof(float), typeof(byte), typeof(int))]
        private static class Deconstructable_SpawnItemsFromConstruction
        {
            private static float[] InjectMass(float[] mass, Deconstructable deconstructable)
            {
                return deconstructable.GetComponent<BuildedGeneShuffler>()?.constructionMass ?? mass;
            }
            /*
            	else
		    ---     array = new float[] { base.GetComponent<PrimaryElement>().Mass };
	        +++     array = InjectMass(new float[] { base.GetComponent<PrimaryElement>().Mass });
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var injectMass = typeof(Deconstructable_SpawnItemsFromConstruction).GetMethodSafe(nameof(InjectMass), true, PPatchTools.AnyArguments);
                if (injectMass != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].opcode == OpCodes.Stelem_R4)
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, injectMass));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // добавим морба в посылки, чтобы был
        [HarmonyPatch(typeof(Immigration), "ConfigureCarePackages")]
        private static class Immigration_ConfigureCarePackages
        {
            private static void Postfix(ref CarePackageInfo[] ___carePackages)
            {
                ___carePackages = ___carePackages.AddItem(new CarePackageInfo(GlomConfig.ID, 1f, null)).ToArray();
            }
        }

        // фикс бага перезарядки
        [HarmonyPatch(typeof(GeneShuffler.GeneShufflerSM), nameof(GeneShuffler.GeneShufflerSM.InitializeStates))]
        private static class GeneShuffler_GeneShufflerSM_InitializeStates
        {
            private static void Postfix(GeneShuffler.GeneShufflerSM __instance)
            {
                __instance.working.pst
                    .PlayAnim("working_pst")
                    .OnAnimQueueComplete(__instance.consumed);
            }
        }
    }
}
