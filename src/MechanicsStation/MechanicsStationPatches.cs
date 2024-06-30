using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using static MechanicsStation.MechanicsStationAssets;

namespace MechanicsStation
{
    internal sealed class MechanicsStationPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(MechanicsStationPatches));
            new POptions().RegisterOptions(this, typeof(MechanicsStationOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuildingAndModifiers()
        {
            ModUtil.AddBuildingToPlanScreen(BUILD_CATEGORY.Equipment, MechanicsStationConfig.ID, BUILD_SUBCATEGORY.industrialstation, PowerControlStationConfig.ID);
            Utils.AddBuildingToTechnology("RefinedObjects", MechanicsStationConfig.ID);
            Init();
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            LoadOptions();
        }

        // шестеренки
        [HarmonyPatch(typeof(MachinePartsConfig), nameof(MachinePartsConfig.CreatePrefab))]
        private static class MachinePartsConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                __result.AddOrGet<KPrefabID>().AddTag(GameTags.MiscPickupable);
                __result.AddOrGet<EntitySplitter>();
            }
        }

        // не выбрасывать шестеренки из фабрикаторов
        [HarmonyPatch(typeof(ComplexFabricator), "DropExcessIngredients")]
        private static class ComplexFabricator_DropExcessIngredients
        {
            private static HashSet<Tag> Inject(HashSet<Tag> tags)
            {
                tags.Add(MachinePartsConfig.ID);
                return tags;
            }
            /*
        	    HashSet<Tag> hashSet = new HashSet<Tag>();
            +++ hashSet.Add(MachinePartsConfig.ID);
	            if (this.keepAdditionalTag != Tag.Invalid)
		            hashSet.Add(this.keepAdditionalTag);
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var newobj = typeof(HashSet<Tag>).GetConstructor(System.Type.EmptyTypes);
                var inject = typeof(ComplexFabricator_DropExcessIngredients).GetMethodSafe(nameof(Inject), true, PPatchTools.AnyArguments);
                if (newobj != null && inject != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Is(OpCodes.Newobj, newobj))
                        {
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, inject));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // добавление построек для улучшения
        // todo: добавлять постройки из длц по мере необходимости
        private static readonly List<string> BuildingWithElementConverterStopList = new List<string>()
        {
            ResearchCenterConfig.ID,            // слишком читерно
            AdvancedResearchCenterConfig.ID,
            CosmicResearchCenterConfig.ID,
            DLC1CosmicResearchCenterConfig.ID,
            OilRefineryConfig.ID,               // ограничение по трубе
            AirFilterConfig.ID,                 // бесполезно и бессмысленно
            AtmoicGardenConfig.ID,              // вообще недоделанная хрень
            RadiationLightConfig.ID,            // бесполезно. todo: в будующем сделать повышение радиации
        };

        private static readonly List<string> BuildingWithComplexFabricatorWorkableStopList = new List<string>()
        {
            GenericFabricatorConfig.ID,         // странная старая хрень
            EggCrackerConfig.ID,                // было бы нелепо ;-D
            OrbitalResearchCenterConfig.ID,     // слишком читерно
            AdvancedApothecaryConfig.ID,        // новая так и недоделанная хрень
        };

        [HarmonyPatch(typeof(Assets), nameof(Assets.AddBuildingDef))]
        private static class Assets_AddBuildingDef
        {
            private static void Prefix(ref BuildingDef def)
            {
                var go = def.BuildingComplete;
                if (go != null)
                {
                    // перерабатывающие постройки, требующие искричество
                    if (def.RequiresPowerInput && go.TryGetComponent<ElementConverter>(out _) && !BuildingWithElementConverterStopList.Contains(def.PrefabID))
                    {
                        MakeMachineTinkerableSave(go);
                        // увеличить всасывание (впервую очередь для скруббера)
                        float multiplier = BASE_SPEED_VALUE + (MechanicsStationOptions.Instance.machinery_speed_modifier / 100);
                        var kPrefabID = go.GetComponent<KPrefabID>();
                        if (go.TryGetComponent<PassiveElementConsumer>(out _))
                        {
                            kPrefabID.prefabInitFn += delegate (GameObject prefab)
                            {
                                if (prefab.TryGetComponent<PassiveElementConsumer>(out var elementConsumer))
                                {
                                    elementConsumer.consumptionRate *= multiplier;
                                    elementConsumer.capacityKG *= multiplier;
                                }
                            };
                        }
                        // увеличить ёмкость потребления из трубы
                        if (go.TryGetComponent<ConduitConsumer>(out _))
                        {
                            kPrefabID.prefabInitFn += delegate (GameObject prefab)
                            {
                                foreach (var conduitConsumer in prefab.GetComponents<ConduitConsumer>())
                                {
                                    if (conduitConsumer != null)
                                    {
                                        conduitConsumer.consumptionRate *= multiplier;
                                        conduitConsumer.capacityKG *= multiplier;
                                    }
                                }
                            };
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
                            if (go.TryGetComponent<BuildingElementEmitter>(out var buildingElementEmitter))
                            {
                                TinkerableFertilizerMaker.base_methane_production_rate = buildingElementEmitter.emitRate;
                            }
                        }
                    }
                    // фабрикаторы
                    else if (go.TryGetComponent<ComplexFabricatorWorkable>(out _) && !BuildingWithComplexFabricatorWorkableStopList.Contains(def.PrefabID))
                    {
                        MakeMachineTinkerableSave(go);
                        go.AddOrGet<TinkerableWorkable>();
                    }
                    // специи
                    else if (def.PrefabID == SpiceGrinderConfig.ID)
                    {
                        MakeMachineTinkerableSave(go);
                        go.AddOrGet<TinkerableWorkable>();
                    }
                }
            }
        }

        // повышение скорости работы фабрикаторов
        [HarmonyPatch(typeof(Workable), nameof(Workable.GetEfficiencyMultiplier))]
        private static class Workable_GetEfficiencyMultiplier
        {
            private static void Postfix(Workable __instance, ref float __result)
            {
                if (__instance.TryGetComponent<TinkerableWorkable>(out var tinkerableWorkable))
                {
                    __result *= tinkerableWorkable.GetCraftingSpeedMultiplier();
                }
            }
        }

        // повышение скорости выработки газа скважиной
        [HarmonyPatch(typeof(OilWellCap), nameof(OilWellCap.AddGasPressure))]
        private static class OilWellCap_AddGasPressure
        {
            private static void Prefix(OilWellCap __instance, ref float dt)
            {
                if (__instance.TryGetComponent<TinkerableWorkable>(out var tinkerableWorkable))
                {
                    dt *= tinkerableWorkable.GetMachinerySpeedMultiplier();
                }
            }
        }

        // нефтяная скважина
        // она не является обычной постройкой, она в другом слое присоединяемых построек, поэтому не участвует в подсистеме комнат
        // объявляем растением саму нефтяную дырку, чтобы она получала сообщения о комнатах
        // и перенаправляем сообщения в скважину

        [HarmonyPatch(typeof(OilWellConfig), nameof(OilWellConfig.CreatePrefab))]
        private static class OilWellConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                __result.AddTag(GameTags.Plant);
            }
        }

        private static readonly EventSystem.IntraObjectHandler<BuildingAttachPoint> OnUpdateRoomDelegate =
            new EventSystem.IntraObjectHandler<BuildingAttachPoint>((BuildingAttachPoint component, object data)
                => RetriggerOnUpdateRoom(component, data));

        private static void RetriggerOnUpdateRoom(BuildingAttachPoint buildingAttachPoint, object data)
        {
            for (int i = 0; i < buildingAttachPoint.points.Length; i++)
            {
                buildingAttachPoint.points[i].attachedBuilding?.Trigger((int)GameHashes.UpdateRoom, data);
            }
        }

        [HarmonyPatch(typeof(BuildingAttachPoint), "OnPrefabInit")]
        private static class BuildingAttachPoint_OnPrefabInit
        {
            private static void Postfix(BuildingAttachPoint __instance)
            {
                __instance.Subscribe((int)GameHashes.UpdateRoom, OnUpdateRoomDelegate);
            }
        }

        [HarmonyPatch(typeof(BuildingAttachPoint), "OnCleanUp")]
        private static class BuildingAttachPoint_OnCleanUp
        {
            private static void Prefix(BuildingAttachPoint __instance)
            {
                __instance.Unsubscribe((int)GameHashes.UpdateRoom, OnUpdateRoomDelegate);
            }
        }

        // древний окаменелостъ. до окончания раскопок никаких шестеренок!
        [HarmonyPatch(typeof(FossilMine), nameof(FossilMine.SetActiveState))]
        private static class FossilMine_SetActiveState
        {
            private static void Postfix(FossilMine __instance, bool active)
            {
                if (__instance.TryGetComponent<TinkerableWorkable>(out var tw))
                {
                    tw.disabled = !active;
                    tw.Trigger((int)GameHashes.EffectRemoved);
                }
            }
        }

        [HarmonyPatch(typeof(Tinkerable), "HasEffect")]
        private static class Tinkerable_HasEffect
        {
            private static void Postfix(Tinkerable __instance, ref bool __result)
            {
                if (__result) return;
                if (__instance.tinkerMaterialTag == MechanicsStationConfig.TINKER_TOOLS
                    && __instance.TryGetComponent<TinkerableWorkable>(out var tw))
                {
                    __result = tw.disabled;
                }
            }
        }
    }
}
