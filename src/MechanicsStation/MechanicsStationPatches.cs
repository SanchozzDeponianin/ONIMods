using System.Collections.Generic;
using Harmony;
using Database;
using Klei.AI;
using STRINGS;
using UnityEngine;

using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace MechanicsStation
{
    internal static class MechanicsStationPatches
    {
        public const string MACHINERY_SPEED_MODIFIER_NAME = "MachinerySpeed";
        public const float MACHINERY_SPEED_MODIFIER = 0.5f;
        public const string CRAFTING_SPEED_MODIFIER_NAME = "CraftingSpeed";
        public const float CRAFTING_SPEED_MODIFIER = 1f;
        public const string MACHINE_TINKER_EFFECT_NAME = "Machine_Tinker";
        public const float MACHINE_TINKER_EFFECT_DURATION = 2f;
        public const string REQUIRED_ROLE_PERK = "CanMachineTinker";

        public static SkillPerk CanMachineTinker;
        public static Attribute CraftingSpeed;
        public static Effect MachineTinkerEffect;

        public static void OnLoad()
        {
            PUtil.InitLibrary();
            PUtil.RegisterPatchClass(typeof(MechanicsStationPatches));
            //POptions.RegisterOptions(typeof(MechanicsStationOptions));
        }

        [PLibMethod(RunAt.AfterModsLoad)]
        private static void InitLocalization()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void AddBuilding()
        {
            Utils.AddBuildingToPlanScreen("Equipment", MechanicsStationConfig.ID, PowerControlStationConfig.ID);
            Utils.AddBuildingToTechnology("RefinedObjects", MechanicsStationConfig.ID);
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void InitDb()
        {
            var db = Db.Get();

            // тюнингуем и актифируем комнату
            // подхватывать максимальный размер комнаты из тюнинга
            int maxRoomSize = TuningData<RoomProber.Tuning>.Get().maxRoomSize;
            RoomConstraints.Constraint MAXIMUM_SIZE_MAX = new RoomConstraints.Constraint(
                building_criteria: null, 
                room_criteria: (Room room) => room.cavity.numCells <= maxRoomSize, 
                times_required: 1, 
                name: string.Format(ROOMS.CRITERIA.MAXIMUM_SIZE.NAME, maxRoomSize), 
                description: string.Format(ROOMS.CRITERIA.MAXIMUM_SIZE.DESCRIPTION, maxRoomSize), 
                stomp_in_conflict: null);

            var additional_constraints = db.RoomTypes.MachineShop.additional_constraints;
            for (int i = 0; i < additional_constraints.Length; i++)
            {
                if (additional_constraints[i] == RoomConstraints.MAXIMUM_SIZE_96)
                {
                    additional_constraints[i] = MAXIMUM_SIZE_MAX;
                    break;
                }
            }

            db.RoomTypes.Add(db.RoomTypes.MachineShop);

            // добавляем перк для работы на станции
            CanMachineTinker = db.SkillPerks.Add(new SimpleSkillPerk(REQUIRED_ROLE_PERK, STRINGS.PERK_CAN_MACHINE_TINKER.DESCRIPTION));
            db.Skills.Technicals1.perks.Add(CanMachineTinker);

            // добавляем модификаторы и эффекты 
            string text = DUPLICANTS.MODIFIERS.MACHINETINKER.NAME;
            string description = STRINGS.DUPLICANTS.MODIFIERS.MACHINETINKER.TOOLTIP;

            if (CraftingSpeed == null)
            {
                CraftingSpeed = db.Attributes.Add(new Attribute(CRAFTING_SPEED_MODIFIER_NAME, false, Attribute.Display.General, false, 1f));
                CraftingSpeed.SetFormatter(new PercentAttributeFormatter());
            }

            if (MachineTinkerEffect == null)
            {
                MachineTinkerEffect = db.effects.Add(new Effect(MACHINE_TINKER_EFFECT_NAME, text, description, MACHINE_TINKER_EFFECT_DURATION * Constants.SECONDS_PER_CYCLE, true, true, false));
                MachineTinkerEffect.Add(new AttributeModifier(MACHINERY_SPEED_MODIFIER_NAME, MACHINERY_SPEED_MODIFIER, text));
                MachineTinkerEffect.Add(new AttributeModifier(CRAFTING_SPEED_MODIFIER_NAME, CRAFTING_SPEED_MODIFIER, text));
            }
        }

        // шестеренки
        [HarmonyPatch(typeof(MachinePartsConfig), nameof(MachinePartsConfig.CreatePrefab))]
        internal static class MachinePartsConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                __result.AddOrGet<KPrefabID>().AddTag(GameTags.MiscPickupable);
                __result.AddOrGet<EntitySplitter>();
            }
        }

        // не выбрасывать шестеренки из фабрикаторов
        [HarmonyPatch(typeof(ComplexFabricator), "OnPrefabInit")]
        internal static class ComplexFabricator_OnPrefabInit
        {
            private static void Postfix(ref ComplexFabricator __instance)
            {
                __instance.keepAdditionalTags.SetTag(MachinePartsConfig.TAG);
            }
        }

        // сделать постройку улучшаемой
        private static Tinkerable MakeMachineTinkerable(GameObject prefab)
        {
            var tinkerable = Tinkerable.MakePowerTinkerable(prefab);
            tinkerable.tinkerMaterialTag = MechanicsStationConfig.TINKER_TOOLS;
            tinkerable.tinkerMaterialAmount = 1f;
            tinkerable.addedEffect = MACHINE_TINKER_EFFECT_NAME;
            tinkerable.requiredSkillPerk = REQUIRED_ROLE_PERK;
            tinkerable.SetWorkTime(20f);
            tinkerable.choreTypeTinker = Db.Get().ChoreTypes.MachineTinker.IdHash;
            tinkerable.choreTypeFetch = Db.Get().ChoreTypes.MachineFetch.IdHash;
            prefab.AddOrGet<RoomTracker>().requiredRoomType = Db.Get().RoomTypes.MachineShop.Id;
            return tinkerable;
        }

        // добавление построек для улучшения
        private static List<string> BuildingWithElementConverterStopList = new List<string>()
        {
            ResearchCenterConfig.ID,            // слишком читерно
            AdvancedResearchCenterConfig.ID,
            CosmicResearchCenterConfig.ID,
            OilRefineryConfig.ID,               // ограничение по трубе
        };

        private static List<string> BuildingWithComplexFabricatorWorkableStopList = new List<string>()
        {
            GenericFabricatorConfig.ID,         // странная старая хрень
            EggCrackerConfig.ID,                // было бы нелепо ;-D
        };

        [HarmonyPatch(typeof(Assets), nameof(Assets.AddBuildingDef))]
        internal static class Assets_AddBuildingDef
        {
            private static void Prefix(ref BuildingDef def)
            {
                var go = def.BuildingComplete;
                if (go != null)
                {
                    // перерабатывающие постройки, требующие искричество
                    if (def.RequiresPowerInput && go.GetComponent<ElementConverter>() != null && !BuildingWithElementConverterStopList.Contains(def.PrefabID))
                    {
                        MakeMachineTinkerable(go);

                        // todo: поразмыслить, как это можно сделать чтобы параметры устанавливались перед загрузкой сейва
                        // увеличить всасывание (впервую очередь для скруббера)
                        var elementConsumer = go.GetComponent<PassiveElementConsumer>();
                        if (elementConsumer != null)
                        {
                            elementConsumer.consumptionRate *= 1f + MACHINERY_SPEED_MODIFIER;
                            elementConsumer.capacityKG *= 1f + MACHINERY_SPEED_MODIFIER;
                        }

                        // увеличить ёмкость потребления из трубы
                        foreach (var conduitConsumer in go.GetComponents<ConduitConsumer>())
                        {
                            if (conduitConsumer != null)
                            {
                                conduitConsumer.consumptionRate *= 1f + MACHINERY_SPEED_MODIFIER;
                                conduitConsumer.capacityKG *= 1f + MACHINERY_SPEED_MODIFIER;
                            }
                        }

                        // скважина
                        if (def.PrefabID == OilWellCapConfig.ID)
                        {
                            go.AddOrGet<TinkerableWorkable>();
                        }
                        // удобрятор
                        if (def.PrefabID == FertilizerMakerConfig.ID)
                        {
                            go.AddOrGet<TinkerableFertilizerMaker>();
                            var buildingElementEmitter = go.GetComponent<BuildingElementEmitter>();
                            if (buildingElementEmitter != null)
                            {
                                TinkerableFertilizerMaker.base_methane_production_rate = buildingElementEmitter.emitRate;
                            }
                        }
                    }
                    // фабрикаторы
                    else if (go.GetComponent<ComplexFabricatorWorkable>() != null && !BuildingWithComplexFabricatorWorkableStopList.Contains(def.PrefabID))
                    {
                        MakeMachineTinkerable(go);
                        go.AddOrGet<TinkerableWorkable>();
                    }
                }
            }
        }

        // хак для повышения скорости работы фабрикаторов
        [HarmonyPatch(typeof(Workable), nameof(Workable.GetEfficiencyMultiplier))]
        internal static class Workable_GetEfficiencyMultiplier
        {
            private static void Postfix(Workable __instance, ref float __result)
            {
                var tinkerableWorkable = __instance.GetComponent<TinkerableWorkable>();
                if (tinkerableWorkable != null)
                {
                    __result *= tinkerableWorkable.GetCraftingSpeedMultiplier();
                }
            }
        }

        // хак для повышения скорости выработки газа скважиной
        [HarmonyPatch(typeof(OilWellCap), nameof(OilWellCap.AddGasPressure))]
        internal static class OilWellCap_AddGasPressure
        {
            private static void Prefix(OilWellCap __instance, ref float dt)
            {
                var tinkerableWorkable = __instance.GetComponent<TinkerableWorkable>();
                if (tinkerableWorkable != null)
                {
                    dt *= tinkerableWorkable.GetMachinerySpeedMultiplier();
                }
            }
        }

        // хаки для нефтяной скважины
        // она не является обычной постройкой, она в другом слое присоединяемых построек, поэтому не участвует в подсистеме комнат
        // объявляем растением саму нефтяную дырку, чтобы она получала сообщения о комнатах
        // и перенаправляем сообщения в скважину

        [HarmonyPatch(typeof(OilWellConfig), nameof(OilWellConfig.CreatePrefab))]
        internal static class OilWellConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                __result.AddTag(GameTags.Plant);
            }
        }

        private static void RetriggerOnUpdateRoom(this BuildingAttachPoint buildingAttachPoint, object data)
        {
            for (int i = 0; i < buildingAttachPoint.points.Length; i++)
            {
                buildingAttachPoint.points[i].attachedBuilding?.Trigger((int)GameHashes.UpdateRoom, data);
            }
        }

        [HarmonyPatch(typeof(BuildingAttachPoint), "OnPrefabInit")]
        internal static class BuildingAttachPoint_OnPrefabInit
        {
            private static void Postfix(BuildingAttachPoint __instance)
            {
                __instance.Subscribe((int)GameHashes.UpdateRoom, __instance.RetriggerOnUpdateRoom);
            }
        }

        [HarmonyPatch(typeof(BuildingAttachPoint), "OnCleanUp")]
        internal static class BuildingAttachPoint_OnCleanUp
        {
            private static void Postfix(BuildingAttachPoint __instance)
            {
                __instance.Unsubscribe((int)GameHashes.UpdateRoom, __instance.RetriggerOnUpdateRoom);
            }
        }
    }
}
