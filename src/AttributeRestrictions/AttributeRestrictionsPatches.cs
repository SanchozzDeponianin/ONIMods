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
            //new POptions().RegisterOptions(this, typeof());
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
                oil.GetComponent<KPrefabID>().prefabSpawnFn +=
                    go => go.GetComponent<AttributeRestriction>().workable = go.GetComponent<OilRefinery.WorkableTarget>();
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
                    var restriction = __instance.target.GetComponent<AttributeRestriction>();
                    if (restriction != null && ReferenceEquals(restriction.workable, __instance.target) && restriction.requiredAttribute != null)
                    {
                        __instance.AddPrecondition(AttributeRestriction.IsSufficientAttributeLevel, restriction);
                    }
                }
            }
        }
    }
}
