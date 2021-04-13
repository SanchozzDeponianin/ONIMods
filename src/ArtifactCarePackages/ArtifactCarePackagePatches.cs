using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using STRINGS;
using Harmony;
using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace ArtifactCarePackages
{
    internal static class ArtifactCarePackagePatches
    {
        public static void OnLoad()
        {
            PUtil.InitLibrary();
            PUtil.RegisterPatchClass(typeof(ArtifactCarePackagePatches));
            POptions.RegisterOptions(typeof(ArtifactCarePackageOptions));
        }

        [PLibMethod(RunAt.AfterModsLoad)]
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
    }
}
