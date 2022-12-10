using System;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;

namespace AttributeRestrictions
{
    internal sealed class AttributeRestrictionsPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(AttributeRestrictionsPatches));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        // добавление сидескреена
        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        private static class DetailsScreen_OnPrefabInit
        {
            private static void Postfix()
            {
                PUIUtils.AddSideScreenContent<AttributeRestrictionSideScreen>();
            }
        }

        // добавляем компонент после инициализации всех построек
        [HarmonyPatch(typeof(BuildingConfigManager), nameof(BuildingConfigManager.ConfigurePost))]
        private static class BuildingConfigManager_ConfigurePost
        {
            private static void Postfix()
            {
                // фабрикаторы
                foreach (var go in Assets.GetPrefabsWithComponent<ComplexFabricator>())
                {
                    var fabricator = go.GetComponent<ComplexFabricator>();
                    if (fabricator != null && fabricator.duplicantOperated)
                        go.AddOrGet<AttributeRestriction>().workable = go.GetComponent<ComplexFabricatorWorkable>();
                }

                // ранч-станции
                foreach (var go in Assets.GetPrefabsWithTag(RoomConstraints.ConstraintTags.RanchStationType))
                {
                    if (go.GetDef<RanchStation.Def>() != null)
                        go.AddOrGet<AttributeRestriction>();
                }

                // вертилятор
                var fan = Assets.GetBuildingDef(IceCooledFanConfig.ID).BuildingComplete;
                var fan_re = fan.AddOrGet<AttributeRestriction>();
                fan_re.workable = fan.GetComponent<IceCooledFanWorkable>();
                fan_re.isBelow = true;

                // дуплячячье колесо
                var gen = Assets.GetBuildingDef(ManualGeneratorConfig.ID).BuildingComplete;
                var gen_re = gen.AddOrGet<AttributeRestriction>();
                gen_re.workable = gen.GetComponent<ManualGenerator>();
                gen_re.isBelow = true;

                // самогонный аппарат
                var oil = Assets.GetBuildingDef(OilRefineryConfig.ID).BuildingComplete;
                var oil_re = oil.AddOrGet<AttributeRestriction>();
                oil_re.overrideAttribute = Db.Get().Attributes.Machinery.Id;
                oil_re.isBelow = true;
                oil.GetComponent<KPrefabID>().prefabSpawnFn += (gmo) =>
                {
                    if (gmo.TryGetComponent<AttributeRestriction>(out var restriction)
                        && gmo.TryGetComponent<OilRefinery.WorkableTarget>(out var workable))
                    {
                        restriction.workable = workable;
                    }
                };
            }
        }

        // Арргхх!!! хармону через жёппу патчит методы шаблонных классов.
        // хотел сделать через WorkChore<Workable>.GetConstructors()
        // а придется вот так, внедрять свою прекондицию при добавлении прекондиции рабочего расписания
        [HarmonyPatch(typeof(Chore), nameof(Chore.AddPrecondition))]
        private static class Chore_AddPrecondition
        {
            private static void Postfix(Chore __instance, Chore.Precondition precondition, object data)
            {
                if (precondition.id == ChorePreconditions.instance.IsScheduledTime.id && data == Db.Get().ScheduleBlockTypes.Work)
                {
                    if (__instance.target.gameObject.TryGetComponent<AttributeRestriction>(out var restriction)
                        && ReferenceEquals(restriction.workable, __instance.target) && restriction.requiredAttribute != null)
                    {
                        __instance.AddPrecondition(AttributeRestriction.IsSufficientAttributeLevel, restriction);
                    }
                }
            }
        }

        // ранчостанция добавляет себе Workable в конструкторе
        // придется патчить конструктор чтобы связать ограничение с Workable достаточно рано перед созданием Chore
        [HarmonyPatch(typeof(RanchStation.Instance), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(IStateMachineTarget), typeof(RanchStation.Def) })]
        private static class RanchStation_Instance_Constructor
        {
            private static void Postfix(RanchStation.Instance __instance)
            {
                if (__instance.gameObject.TryGetComponent<AttributeRestriction>(out var restriction)
                    && __instance.gameObject.TryGetComponent<RancherChore.RancherWorkable>(out var workable))
                {
                    restriction.workable = workable;
                }
            }
        }

        // для ранчостанции внедряем прекондицию отдельным патчем, так как она создаёт Chore с target != Workable
        [HarmonyPatch(typeof(RanchStation.Instance), nameof(RanchStation.Instance.CreateChore))]
        private static class RanchStation_Instance_CreateChore
        {
            private static void Postfix(RanchStation.Instance __instance, Chore __result)
            {
                if (__instance.gameObject.TryGetComponent<AttributeRestriction>(out var restriction)
                    && restriction.requiredAttribute != null)
                {
                    __result.AddPrecondition(AttributeRestriction.IsSufficientAttributeLevel, restriction);
                }
            }
        }
    }
}
