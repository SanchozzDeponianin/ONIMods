using System;
using System.Collections.Generic;
using Harmony;
using Klei.AI;
using UnityEngine;

namespace DeathReimagined
{
    // набор патчей для новых болезней

    internal static class DiseasesPatches
    {
        // атрибут чуйствительности к инфаркту
        [HarmonyPatch(typeof(Database.Attributes), MethodType.Constructor, new Type[] { typeof(ResourceSet) })]
        public static class Database_Attributes_Constructor
        {
            public static void Postfix(Database.Attributes __instance)
            {
                Klei.AI.Attribute attribute = new Klei.AI.Attribute(HeartAttackMonitor.ATTRIBUTE_ID, false, Klei.AI.Attribute.Display.Details, false, 0f, null, null);
                attribute.SetFormatter(new ToPercentAttributeFormatter(1, GameUtil.TimeSlice.None));
                __instance.Add(attribute);
            }
        }

        // новая болезнь инфаркт
        [HarmonyPatch(typeof(Database.Sicknesses), MethodType.Constructor, new Type[] { typeof(ResourceSet) })]
        internal static class Database_Sicknesses_Constructor
        {
            private static void Postfix(Database.Sicknesses __instance)
            {
                __instance.Add(new HeartAttackSickness());
            }
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        internal static class Db_Initialize
        {
            private static void Postfix(Db __instance)
            {
                // добавляем модификаторы инфаркта к трейтам:
                // Торопыжка
                Trait traitTwinkletoes = __instance.traits.Get("Twinkletoes");
                traitTwinkletoes.Add(new AttributeModifier(HeartAttackMonitor.ATTRIBUTE_ID, -0.05f, traitTwinkletoes.Name));

                // Силач
                Trait traitStrongArm = __instance.traits.Get("StrongArm");
                traitStrongArm.Add(new AttributeModifier(HeartAttackMonitor.ATTRIBUTE_ID, -0.05f, traitStrongArm.Name));

                // Анемия
                Trait traitAnemic = __instance.traits.Get("Anemic");
                traitAnemic.Add(new AttributeModifier(HeartAttackMonitor.ATTRIBUTE_ID, +0.1f, traitAnemic.Name));

                // Пацифист
                Trait traitScaredyCat = __instance.traits.Get("ScaredyCat");
                traitScaredyCat.Add(new AttributeModifier(HeartAttackMonitor.ATTRIBUTE_ID, +0.1f, traitScaredyCat.Name));

                // Лапшерукий
                Trait traitNoodleArms = __instance.traits.Get("NoodleArms");
                traitNoodleArms.Add(new AttributeModifier(HeartAttackMonitor.ATTRIBUTE_ID, +0.05f, traitNoodleArms.Name));
            }
        }

        // добавляем новые модификаторы для увеличения и уменьшения шанса инфаркта:
        [HarmonyPatch(typeof(ModifierSet), "LoadEffects")]
        internal static class ModifierSet_LoadEffects
        {
            private static void Postfix(ModifierSet __instance)
            {
                // плохой сон - свет
                Effect effectBadSleep = __instance.effects.Get("BadSleep");
                effectBadSleep.Add(new AttributeModifier(HeartAttackMonitor.ATTRIBUTE_ID, +0.05f, effectBadSleep.Name));

                // плохой сон - храп
                Effect effectTerribleSleep = __instance.effects.Get("TerribleSleep");
                effectTerribleSleep.Add(new AttributeModifier(HeartAttackMonitor.ATTRIBUTE_ID, +0.1f, effectTerribleSleep.Name));

                // лечение на койке
                Effect effectMedicalCot = __instance.effects.Get("MedicalCot");
                effectMedicalCot.Add(new AttributeModifier(HeartAttackMonitor.ATTRIBUTE_ID, -0.1f, effectMedicalCot.Name));

                // лечение на койке с доктором
                Effect effectMedicalCotDoctored = __instance.effects.Get("MedicalCotDoctored");
                effectMedicalCotDoctored.Add(new AttributeModifier(HeartAttackMonitor.ATTRIBUTE_ID, -0.2f, effectMedicalCotDoctored.Name));
                effectMedicalCotDoctored.Add(new AttributeModifier(HeartAttackSickness.ID + "CureSpeed", 0.25f, effectMedicalCotDoctored.Name));

                // красная тревога
                Effect effectRedAlert = __instance.effects.Get("RedAlert");
                effectRedAlert.Add(new AttributeModifier(HeartAttackMonitor.ATTRIBUTE_ID, +0.25f, effectRedAlert.Name));
            }
        }

        // визуализация шансов на инфаркт в уй
        [HarmonyPatch(typeof(DiseaseInfoScreen), "CreateImmuneInfo")]
        public static class DiseaseInfoScreen_CreateImmuneInfo_Patch
        {
            internal static void Postfix(GameObject ___selectedTarget, CollapsibleDetailContentPanel ___immuneSystemPanel, bool __result)
            {
                if (__result)
                {
                    AttributeInstance susceptibility = Db.Get().Attributes.Get(HeartAttackMonitor.ATTRIBUTE_ID).Lookup(___selectedTarget);
                    if (susceptibility != null)
                    {
                        ___immuneSystemPanel.SetLabel(HeartAttackMonitor.ATTRIBUTE_ID, susceptibility.modifier.Name + ": " + GameUtil.GetFormattedPercent(100f * Mathf.Clamp01(susceptibility.GetTotalValue())), susceptibility.GetAttributeValueTooltip());
                    }
                }
            }
        }

        // патчи для мед.койки
        // есть критичная болезня ?
        private static bool HasCriticalDisease(Sicknesses sicknesses)
        {
            if (sicknesses != null)
            {
                using (IEnumerator<SicknessInstance> enumerator = sicknesses.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.modifier.severity >= Sickness.Severity.Critical)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // возвращаем возможность лежать на койке при наличии болезней.
        [HarmonyPatch(typeof(Clinic), "CanManuallyAssignTo")]
        internal static class Clinic_CanManuallyAssignTo
        {
            private static void Postfix(Clinic __instance, MinionAssignablesProxy worker, ref bool __result)
            {
                MinionIdentity minionIdentity = worker.target as MinionIdentity;
                if (minionIdentity != null)
                {
                    __result = __result || minionIdentity.HasTag(GameTags.Incapacitated) || (!string.IsNullOrEmpty(__instance.diseaseEffect) && HasCriticalDisease(minionIdentity.GetSicknesses()));
                }
            }
        }

        // если есть критичная болезнь, то в первую очередь использовать на койке анимацию болезни а не раны.
        [HarmonyPatch(typeof(Clinic), "GetAppropriateOverrideAnims")]
        internal static class Clinic_GetAppropriateOverrideAnims
        {
            private static bool Prefix(Clinic __instance, Worker worker, ref KAnimFile[] __result)
            {
                if (__instance.workerDiseasedAnims != null && !string.IsNullOrEmpty(__instance.diseaseEffect) && HasCriticalDisease(worker.GetSicknesses()))
                {
                    __result = __instance.workerDiseasedAnims;
                    return false;
                }
                return true;
            }
        }

    }
}
