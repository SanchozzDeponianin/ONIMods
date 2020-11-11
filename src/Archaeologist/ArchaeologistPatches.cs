using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using TUNING;
using SanchozzONIMods.Lib;

namespace Archaeologist
{
    internal static class ArchaeologistPatches
    {
        private const string Archaeologist = "Archaeologist";

        [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
        internal static class Db_Initialize
        {
            private static void Prefix()
            {
                List<DUPLICANTSTATS.TraitVal> PATCHEDGOODTRAITS = new List<DUPLICANTSTATS.TraitVal>(DUPLICANTSTATS.GOODTRAITS)
                {
                    new DUPLICANTSTATS.TraitVal
                    {
                        id = Archaeologist,
                        statBonus = -DUPLICANTSTATS.MEDIUM_STATPOINT_BONUS,
                        probability = DUPLICANTSTATS.PROBABILITY_MED,
                        mutuallyExclusiveTraits = new List<string>
                        {
                            "CantResearch",
                            "Uncultured"
                        }
                    }
                };
                try
                {
                    Traverse.Create(typeof(DUPLICANTSTATS)).Field("GOODTRAITS").SetValue(PATCHEDGOODTRAITS);
                }
                catch (FieldAccessException thrown)
                {
                    Utils.LogExcWarn(thrown);
                }
                catch (TargetException thrown2)
                {
                    Utils.LogExcWarn(thrown2);
                }
            }

            private static void Postfix(ref Db __instance)
            {
                __instance.traits.Get(Archaeologist).PositiveTrait = true;
            }
        }

        [HarmonyPatch(typeof(Localization), nameof(Localization.Initialize))]
        internal static class Localization_Initialize
        {
            private static void Postfix(Localization.Locale ___sLocale)
            {
                Utils.InitLocalization(typeof(STRINGS), ___sLocale);
                LocString.CreateLocStringKeys(typeof(STRINGS.DUPLICANTS));
            }
        }

        // для получения скриншота на лежанке
        /*
        [HarmonyPatch(typeof(BeachChair), "OnSpawn")]
        internal static class Test2
        {
            private static void Postfix(ref BeachChair __instance)
            {
                UnityEngine.Object.DestroyImmediate(__instance.gameObject.GetComponent<AnimTileable>());
                KBatchedAnimController kBatchedAnimController = __instance.gameObject.GetComponent<KBatchedAnimController>();
                kBatchedAnimController.SetSymbolVisiblity("backdrop", false);
                kBatchedAnimController.SetSymbolVisiblity("cap_left", false);
                kBatchedAnimController.SetSymbolVisiblity("cap_right", false);
            }
        }
        */
    }
}
