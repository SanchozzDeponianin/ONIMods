using System.Collections.Generic;
using System.Linq;
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
            base.OnLoad(harmony);
            PUtil.InitLibrary();
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
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var list_count = typeof(List<CarePackageInfo>).GetProperty(nameof(List<CarePackageInfo>.Count)).GetGetMethod();
                var inject = typeof(ArtifactImmigration).GetMethodSafe(nameof(ArtifactImmigration.InjectRandomCarePackages), true, PPatchTools.AnyArguments);

                bool result = false;
                if (list_count != null && inject != null)
                {
                    for (int i = 0; i < instructionsList.Count; i++)
                    {
                        var instruction = instructionsList[i];
                        if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info) && info == list_count)
                        {
                            instructionsList.Insert(i, new CodeInstruction(OpCodes.Call, inject));
                            result = true;
#if DEBUG
                            PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                            break;
                        }
                    }
                }
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
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
                if (go.HasTag(GameTags.Artifact))
                {
                    int needTerrestrialArtifact = Mathf.Max(0, REQUIRED_ARTIFACT_COUNT - ArtifactSelector.Instance.AnalyzedArtifactCount);
                    int needSpaceArtifact = Mathf.Max(0, REQUIRED_ARTIFACT_COUNT - ArtifactSelector.Instance.AnalyzedSpaceArtifactCount);
                    float chanceTerrestrialArtifact = (needTerrestrialArtifact + GAP) / (needTerrestrialArtifact + needSpaceArtifact + 2 * GAP);
                    if (Random.value < chanceTerrestrialArtifact)
                    {
                        go.GetComponent<KPrefabID>().AddTag(GameTags.TerrestrialArtifact, true);
                    }
                }
            }

            /*
                gameObject.SetActive(true);
            +++ TryMakeTerrestrialArtifact(gameObject);
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var setActive = typeof(GameObject).GetMethodSafe(nameof(GameObject.SetActive), false, PPatchTools.AnyArguments);
                var tryMakeTerrestrialArtifact = typeof(CarePackage_SpawnContents).GetMethodSafe(nameof(CarePackage_SpawnContents.TryMakeTerrestrialArtifact), true, PPatchTools.AnyArguments);

                bool result = false;
                if (setActive != null && tryMakeTerrestrialArtifact != null)
                {

                    for (int i = 0; i < instructionsList.Count; i++)
                    {
                        var instruction = instructionsList[i];
                        if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
                            && (instruction.operand is MethodInfo info) && info == setActive)
                        {
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Ldloc_0));
                            instructionsList.Insert(++i, new CodeInstruction(OpCodes.Call, tryMakeTerrestrialArtifact));
                            result = true;
                            break;
#if DEBUG
                        PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                        }
                    }
                }
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
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
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var initialAnim = typeof(KAnimControllerBase).GetFieldSafe(nameof(KAnimControllerBase.initialAnim), false);
                var getProperAnim = typeof(CarePackage_SetAnimToInfo).GetMethodSafe(nameof(GetProperAnim), true, PPatchTools.AnyArguments);

                bool result = false;
                if (initialAnim != null && getProperAnim != null)
                {

                    for (int i = 0; i < instructionsList.Count; i++)
                    {
                        var instruction = instructionsList[i];
                        if (instruction.opcode == OpCodes.Ldfld && (instruction.operand is FieldInfo info) && info == initialAnim)
                        {
                            instructionsList[i] = new CodeInstruction(OpCodes.Call, getProperAnim);
                            result = true;
                            break;
#if DEBUG
                        PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                        }
                    }
                }
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }
        }
    }
}
