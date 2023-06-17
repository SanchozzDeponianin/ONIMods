using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using STRINGS;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace ArtifactCarePackages
{
    internal sealed class ArtifactCarePackagePatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary();
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(ArtifactCarePackagePatches));
            new POptions().RegisterOptions(this, typeof(ArtifactCarePackageOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            ArtifactCarePackageOptions.Reload();
            new ArtifactImmigration();
        }

        [PLibMethod(RunAt.OnEndGame)]
        private static void OnEndGame()
        {
            ArtifactImmigration.DestroyInstance();
        }

        // внедряем в список возможных пакетов - пакеты с артифактами
        [HarmonyPatch(typeof(Immigration), nameof(Immigration.RandomCarePackage))]
        internal static class Immigration_RandomCarePackage
        {
            /*
            foreach (CarePackageInfo package in this.carePackages)
            {
                bool flag = package.requirement == null || package.requirement();
                if (flag)
                {
                    possiblePackages.Add(package);
                }
            }
        +++ ArtifactImmigration.InjectRandomCarePackages(possiblePackages);
            return possiblePackages[UnityEngine.Random.Range(0, possiblePackages.Count)];
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            { 
                var list_count = typeof(List<CarePackageInfo>).GetProperty(nameof(List<CarePackageInfo>.Count)).GetGetMethod();
                var inject = typeof(ArtifactImmigration).GetMethodSafe(nameof(ArtifactImmigration.InjectRandomCarePackages), true, PPatchTools.AnyArguments);
                if (list_count != null && inject != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(list_count))
                        {
                            instructions.Insert(i, new CodeInstruction(OpCodes.Call, inject));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // подправляем текст в экране выбора, чтобы показать параметры декора
        [HarmonyPatch(typeof(CarePackageContainer), "GetCurrentQuantity")]
        internal static class CarePackageContainer_GetCurrentQuantity
        {
            private static void Postfix(CarePackageInfo ___info, ref string __result)
            {
                var a = Assets.GetPrefab(___info.id.ToTag())?.GetComponent<SpaceArtifact>()?.GetArtifactTier();
                if (a != null)
                {
                    var value = a.decorValues.amount;
                    string decorString = GameUtil.AddPositiveSign(value.ToString(), value > 0f);
                    __result = string.Concat(__result, "\n",
                        string.Format(UI.BUILDINGEFFECTS.DECORPROVIDED, "", decorString, a.decorValues.radius));
                }
            }
        }

        // в длц - часть полученных артифактов помечаем как "наземные артифакты"
        // по эмпирической формуле, в зависимости - сколько еще нужно для ачивки
        [HarmonyPatch(typeof(CarePackage), "SpawnContents")]
        internal static class CarePackage_SpawnContents
        {
            private const int REQUIRED_ARTIFACT_COUNT = 10;
            private const float GAP = 0.0001f;
            private static bool Prepare() => DlcManager.IsExpansion1Active();

            private static void TryMakeTerrestrialArtifact(GameObject go)
            {
                var artifact = go.GetComponent<SpaceArtifact>();
                if (artifact != null)
                {
                    bool isTerrestrial;
                    switch (artifact.artifactType)
                    {
                        case ArtifactType.Space:
                            isTerrestrial = false;
                            break;
                        case ArtifactType.Terrestrial:
                            isTerrestrial = true;
                            break;
                        case ArtifactType.Any:
                        default:
                            int needTerrestrialArtifact = Mathf.Max(0, REQUIRED_ARTIFACT_COUNT - ArtifactSelector.Instance.AnalyzedArtifactCount);
                            int needSpaceArtifact = Mathf.Max(0, REQUIRED_ARTIFACT_COUNT - ArtifactSelector.Instance.AnalyzedSpaceArtifactCount);
                            float chanceTerrestrialArtifact = (needTerrestrialArtifact + GAP) / (needTerrestrialArtifact + needSpaceArtifact + 2 * GAP);
                            isTerrestrial = Random.value < chanceTerrestrialArtifact;
                            break;
                    }
                    if (isTerrestrial)
                        go.GetComponent<KPrefabID>().AddTag(GameTags.TerrestrialArtifact, true);
                }
            }

            /*
                gameObject.SetActive(true);
            +++ TryMakeTerrestrialArtifact(gameObject);
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            { 
                var setActive = typeof(GameObject).GetMethodSafe(nameof(GameObject.SetActive), false, typeof(bool));
                var tryMakeTerrestrialArtifact = typeof(CarePackage_SpawnContents).GetMethodSafe(nameof(CarePackage_SpawnContents.TryMakeTerrestrialArtifact), true, PPatchTools.AnyArguments);
                if (setActive != null && tryMakeTerrestrialArtifact != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(setActive))
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Ldloc_0));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, tryMakeTerrestrialArtifact));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // в длц - поправляем анимацию посылки чтобы было "замуровано"
        [HarmonyPatch(typeof(CarePackage), "SetAnimToInfo")]
        internal static class CarePackage_SetAnimToInfo
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active();

            private static string GetProperAnim(KBatchedAnimController kbac)
            {
                string result = kbac.initialAnim;
                if (kbac.HasTag(GameTags.Artifact))
                {
                    result = result.Replace("idle_", "entombed_");
                }
                return result;
            }

            /*
            --- KBatchedAnimController4.initialAnim = KBatchedAnimController2.initialAnim;
            +++ KBatchedAnimController4.initialAnim = GetProperAnim(KBatchedAnimController2);
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            { 
                var initialAnim = typeof(KAnimControllerBase).GetFieldSafe(nameof(KAnimControllerBase.initialAnim), false);
                var getProperAnim = typeof(CarePackage_SetAnimToInfo).GetMethodSafe(nameof(GetProperAnim), true, PPatchTools.AnyArguments);
                if (initialAnim != null && getProperAnim != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].LoadsField(initialAnim))
                        {
                            instructions[i] = new CodeInstruction(OpCodes.Call, getProperAnim);
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
