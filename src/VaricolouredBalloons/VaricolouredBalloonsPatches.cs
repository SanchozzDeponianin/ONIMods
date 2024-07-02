using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Database;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;

namespace VaricolouredBalloons
{
    public sealed class VaricolouredBalloonsPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(VaricolouredBalloonsPatches));
        }

        private static readonly BalloonArtistFacadeType Varicoloured = (BalloonArtistFacadeType)Hash.SDBMLower(nameof(Varicoloured));
        private static readonly List<BalloonOverrideSymbolIter> BalloonOverrides = new List<BalloonOverrideSymbolIter>();
        public static ReadOnlyCollection<BalloonArtistFacadeResource> MyBalloons { get; private set; }

        private static BalloonArtistFacadeResource MakeBalloon(string id, string animFile, BalloonArtistFacadeType type)
        {
            // для большинства анимаций (кроме групповых) их batchTag совпадает с id их группы
            // для моддовых анимаций игра создаёт отдельные группы с id = name, однако batchTag = KAnimBatchManager.NO_BATCH
            // начиная с U48 batchTag начал использоваться вглубине скинов балонов, изза одинакового batchTag началось мерцание текстур
            // поэтому, сгенерируем уникальный batchTag от name
            var kAnimFile = Assets.GetAnim(animFile);
            Traverse.Create(kAnimFile).Field<HashedString>("_batchTag").Value = kAnimFile.name;
            return new BalloonArtistFacadeResource(id, string.Empty, string.Empty, PermitRarity.Universal, animFile, type, DlcManager.AVAILABLE_ALL_VERSIONS);
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            var myBalloons = new List<BalloonArtistFacadeResource>();
            myBalloons.Add(MakeBalloon("VBalloonOrangeLongSparkles", "varicoloured_balloon_orange_kanim", BalloonArtistFacadeType.ThreeSet));
            myBalloons.Add(MakeBalloon("VBalloonBabyPuftMutant", "varicoloured_balloon_puft_mutant_kanim", Varicoloured));
            MyBalloons = myBalloons.AsReadOnly();
            var unlocked = Db.Get().Permits.BalloonArtistFacades.resources.Where(facade => facade.IsUnlocked()).ToList();
            unlocked.AddRange(myBalloons);
            foreach (var facade in unlocked)
            {
                var iter = facade.GetSymbolIter();
                for (int i = 0; i < facade.balloonOverrideSymbolIDs.Length; i++)
                {
                    // добавляем несколько раз пропорционально количеству вариаций в одном ресурсе, чтобы при выборе рандома все они были равновероятны
                    BalloonOverrides.Add(iter);
                }
            }
        }

        // загрузка самопальных балонов, подхватываем все символы называющиеся на "body"
        [HarmonyPatch(typeof(BalloonArtistFacadeResource), "GetBalloonOverrideSymbolIDs")]
        private static class BalloonArtistFacadeResource_GetBalloonOverrideSymbolIDs
        {
            private static bool Prefix(BalloonArtistFacadeResource __instance, BalloonArtistFacadeType ___balloonFacadeType, ref string[] __result)
            {
                if (___balloonFacadeType == Varicoloured)
                {
                    __result = __instance.AnimFile.GetData().build.symbols
                        .Select(symbol => HashCache.Get().Get(symbol.hash))
                        .Where(name => !string.IsNullOrEmpty(name) && name.StartsWith("body"))
                        .ToArray();
                    return false;
                }
                return true;
            }
        }

        // рандомизируем артиста если у его дефолтный скин
        [HarmonyPatch(typeof(BalloonOverrideSymbolIter), nameof(BalloonOverrideSymbolIter.Next))]
        private static class BalloonOverrideSymbolIter_Next
        {
            private static void Postfix(ref BalloonOverrideSymbol ___current, ref BalloonOverrideSymbol __result)
            {
                if (__result.animFile.IsNone())
                {
                    var index = UnityEngine.Random.Range(0, 1 + BalloonOverrides.Count);
                    if (index < BalloonOverrides.Count)
                        __result = BalloonOverrides[index].Next();
                    else
                        __result = default;
                    ___current = __result;
                }
            }
        }

        // носимый шарег:
        [HarmonyPatch(typeof(EquippableBalloonConfig), nameof(EquippableBalloonConfig.DoPostConfigure))]
        private static class EquippableBalloonConfig_DoPostConfigure
        {
            private static void Postfix(GameObject go)
            {
                go.AddOrGet<ModdedEquippableBalloon>();
            }
        }

        // избегаем записи в сейф ид моддовых анимаций
        [HarmonyPatch(typeof(EquippableBalloon), nameof(EquippableBalloon.SetBalloonOverride))]
        private static class EquippableBalloon_SetBalloonOverride
        {
            private static void Prefix(EquippableBalloon __instance, ref BalloonOverrideSymbol balloonOverride)
            {
                if (balloonOverride.animFile.IsSome()
                    && Assets.ModLoadedKAnims.Contains(balloonOverride.animFile.Unwrap())
                    && __instance.TryGetComponent<ModdedEquippableBalloon>(out var moddedBalloon))
                {
                    moddedBalloon.facadeAnim = balloonOverride.animFileID;
                    moddedBalloon.symbolID = balloonOverride.animFileSymbolID;
                    balloonOverride = default;
                }
            }
        }

        // восстанавливаем ид моддовых анимаций при применении
        [HarmonyPatch(typeof(EquippableBalloon), nameof(EquippableBalloon.ApplyBalloonOverrideToBalloonFx))]
        private static class EquippableBalloon_ApplyBalloonOverrideToBalloonFx
        {
            private static BalloonOverrideSymbol RestoreBalloonOverride(BalloonOverrideSymbol @override, EquippableBalloon balloon)
            {
                if (@override.animFile.IsNone()
                    && balloon.TryGetComponent<ModdedEquippableBalloon>(out var moddedBalloon)
                    && !string.IsNullOrEmpty(moddedBalloon.facadeAnim)
                    && !string.IsNullOrEmpty(moddedBalloon.symbolID))
                {
                    return new BalloonOverrideSymbol(moddedBalloon.facadeAnim, moddedBalloon.symbolID);
                }
                return @override;
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var constructor = typeof(BalloonOverrideSymbol).GetConstructor(new Type[] { typeof(string), typeof(string) });
                var restore = typeof(EquippableBalloon_ApplyBalloonOverrideToBalloonFx).GetMethodSafe(nameof(RestoreBalloonOverride), true, PPatchTools.AnyArguments);
                if (constructor != null && restore != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Is(OpCodes.Newobj, constructor))
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, restore));
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
