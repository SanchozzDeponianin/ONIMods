using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Database;
using TUNING;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using static ReBuildableAETN.MassiveHeatSinkCoreConfig;

namespace ReBuildableAETN
{
    public sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            ModOptions.Reload();
            new PPatchManager(harmony).RegisterPatchClass(GetType());
            new POptions().RegisterOptions(this, typeof(ModOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
            LoadSprite();
            harmony.PatchAll();
        }

        private static void LoadSprite()
        {
            const string name = "ui_buildingneutroniumcore";
            var sprite = PUIUtils.LoadSprite($"sprites/{name}.png");
            sprite.name = name;
            Assets.Sprites.Add(name, sprite);
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            Utils.AddBuildingToPlanScreen(BUILD_CATEGORY.Utilities, MassiveHeatSinkConfig.ID, BUILD_SUBCATEGORY.temperature);
            Utils.AddBuildingToTechnology("Catalytics", MassiveHeatSinkConfig.ID);
            GameTags.MaterialBuildingElements.Add(MaterialBuildingTag);
            // добавляем ядра -выдры- в космос в ванилле
            var chance = ModOptions.Instance.VanillaPlanetChance;
            if (DlcManager.IsPureVanilla() && chance.Enabled)
            {
                var sdp = Db.Get().SpaceDestinationTypes;
                CloneArtifactDropRateTable(sdp.IcyDwarf, TIER_CORE, chance.IcyDwarfChance / 100f);
                CloneArtifactDropRateTable(sdp.IceGiant, TIER_CORE, chance.IceGiantChance / 100f);
            }
        }

        private static void CloneArtifactDropRateTable(SpaceDestinationType destination, ArtifactTier tier, float weight_percent)
        {
            var result = new ArtifactDropRate();
            float weight = destination.artifactDropTable.totalWeight * weight_percent;
            foreach (var rate in destination.artifactDropTable.rates)
            {
                if (rate.first == DECOR.SPACEARTIFACT.TIER_NONE)
                    result.AddItem(rate.first, rate.second - weight);
                else
                    result.AddItem(rate.first, rate.second);
            }
            result.AddItem(tier, weight);
            destination.artifactDropTable = result;
        }

        // добавляем ядра для постройки аэтна
        [HarmonyPatch(typeof(MassiveHeatSinkConfig), nameof(MassiveHeatSinkConfig.CreateBuildingDef))]
        private static class MassiveHeatSinkConfig_CreateBuildingDef
        {
            private static void Postfix(BuildingDef __result)
            {
                __result.ShowInBuildMenu = true;
                __result.ViewMode = OverlayModes.GasConduits.ID;
                __result.MaterialCategory = MATERIALS.REFINED_METALS.Append(MaterialBuildingTag.Name);
                __result.Mass = __result.Mass.Append(2);
                if (ModOptions.Instance.AddLogicPort)
                    __result.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(1, 0));
            }
        }

        [HarmonyPatch(typeof(MassiveHeatSinkConfig), nameof(MassiveHeatSinkConfig.DoPostConfigureComplete))]
        private static class MassiveHeatSinkConfig_DoPostConfigureComplete
        {
            private static void Postfix(GameObject go)
            {
                // поправим массу
                var def = go.GetComponent<Building>().Def;
                go.GetComponent<PrimaryElement>().MassPerUnit = def.Mass[0];
                def.BuildingUnderConstruction.GetComponent<PrimaryElement>().MassPerUnit = def.Mass[0];
                // требование навыка для постройки
                var constructable = def.BuildingUnderConstruction.GetComponent<Constructable>();
                constructable.requiredSkillPerk = Db.Get().SkillPerks.CanDemolish.Id;
                // требование навыка для разрушения
                var deconstructable = go.GetComponent<Deconstructable>();
                deconstructable.requiredSkillPerk = Db.Get().SkillPerks.CanDemolish.Id;
                deconstructable.allowDeconstruction = false;
                go.AddOrGet<MassiveHeatSinkRebuildable>();
                if (ModOptions.Instance.AddLogicPort)
                    go.AddOrGet<LogicOperationalController>();
                go.UpdateComponentRequirement<Demolishable>(false);
            }
        }

        // скрываем требование навыка пока разрушение не назначено
        [HarmonyPatch(typeof(Deconstructable), "OnSpawn")]
        private static class Deconstructable_OnSpawn
        {
            private static void Prefix(ref bool ___shouldShowSkillPerkStatusItem)
            {
                ___shouldShowSkillPerkStatusItem = false;
            }
        }

        [HarmonyPatch]
        private static class Deconstructable_Queue_Cancel_Deconstruction
        {
            private static readonly DetouredMethod<Action<Workable, object>> UpdateStatusItem =
                typeof(Workable).DetourLazy<Action<Workable, object>>("UpdateStatusItem");

            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new MethodBase[] {
                    typeof(Deconstructable).GetMethodSafe("QueueDeconstruction", false),
                    typeof(Deconstructable).GetMethodSafe("CancelDeconstruction", false),
                };
            }

            private static void Postfix(Deconstructable __instance, ref bool ___shouldShowSkillPerkStatusItem, bool ___isMarkedForDeconstruction)
            {
                ___shouldShowSkillPerkStatusItem = ___isMarkedForDeconstruction;
                UpdateStatusItem.Invoke(__instance, null);
            }
        }

        // исправляем требование к массе. изза неконсистентности после У56:
        // строительное мюню считает в юнитах, а доставка для стройки в килограммах.
        // чтобы нельзя было строить аетн из замурованной версии едра,
        // придется влезть во все постройки и запретить тэг замурованного артифакта.
        [HarmonyPatch(typeof(Constructable), "OnSpawn")]
        private static class Constructable_OnSpawn
        {
            private static void InjectForbiddenTag(FetchList2 fetchList, Tag tag, Tag[] forbidden_tags, float amount, Operational.State operationalRequirement)
            {
                if (tag == TAG)
                {
                    forbidden_tags = (forbidden_tags ?? new Tag[0]).Append(GameTags.CharmedArtifact);
                    amount *= MASS;
                }
                fetchList.Add(tag, forbidden_tags, amount, operationalRequirement);
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var fetchList_Add = typeof(FetchList2).GetMethodSafe(nameof(FetchList2.Add), false, typeof(Tag), typeof(Tag[]), typeof(float), typeof(Operational.State));
                var injectForbiddenTag = typeof(Constructable_OnSpawn).GetMethodSafe(nameof(InjectForbiddenTag), true, PPatchTools.AnyArguments);
                if (fetchList_Add != null && injectForbiddenTag != null)
                {
                    instructions = PPatchTools.ReplaceMethodCallSafe(instructions, fetchList_Add, injectForbiddenTag).ToList();
                    return true;
                }
                return false;
            }
        }

        // косметический патч, отображаем в кодексе ядра в юнитах
        [HarmonyPatch(typeof(CodexEntryGenerator), "GenerateBuildingDescriptionContainers")]
        private static class CodexEntryGenerator_GenerateBuildingDescriptionContainers
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL)
            {
                return instructions.Transpile(original, IL, transpiler);
            }

            private static string GetUnits(float amount)
            {
                return GameUtil.GetFormattedUnits(amount);
            }
            /*
            --- GameUtil.GetFormattedMass(блабла)
            +++ def.MaterialCategory[i] == MaterialBuildingTag ? GameUtil.GetFormattedUnits(блабла) : GameUtil.GetFormattedMass(блабла)
            */
            private static bool transpiler(List<CodeInstruction> instructions, MethodBase method, ILGenerator IL)
            {
                var def = method.GetParameters().FirstOrDefault(p => p.ParameterType == typeof(BuildingDef));
                var material = typeof(BuildingDef).GetFieldSafe(nameof(BuildingDef.MaterialCategory), false);
                var mass = typeof(BuildingDef).GetFieldSafe(nameof(BuildingDef.Mass), false);
                var get_mass = typeof(GameUtil).GetMethodSafe(nameof(GameUtil.GetFormattedMass), true, PPatchTools.AnyArguments);
                var get_units = typeof(CodexEntryGenerator_GenerateBuildingDescriptionContainers).GetMethodSafe(nameof(GetUnits), true, typeof(float));
                var equals = typeof(string).GetMethodSafe(nameof(string.Equals), true, typeof(string), typeof(string));

                if (def == null || material == null || mass == null || get_mass == null || get_units == null || equals == null)
                    return false;
                int i = instructions.FindIndex(inst => inst.Calls(get_mass));
                if (i == -1) return false;
                int j = instructions.FindLastIndex(i, inst => inst.LoadsField(mass));
                if (j == -1 || !instructions[j + 1].IsLdloc()) return false;
                var index = new CodeInstruction(instructions[j + 1]);

                j--;
                var @else = IL.DefineLabel();
                instructions[j].labels.Add(@else);
                var endif = IL.DefineLabel();
                instructions[i + 1].labels.Add(endif);

                instructions.Insert(j++, def.GetLoadArgInstruction());
                instructions.Insert(j++, new CodeInstruction(OpCodes.Ldfld, material));
                instructions.Insert(j++, index);
                instructions.Insert(j++, new CodeInstruction(OpCodes.Ldelem_Ref));
                instructions.Insert(j++, new CodeInstruction(OpCodes.Ldstr, MaterialBuildingTag.Name));
                instructions.Insert(j++, new CodeInstruction(OpCodes.Call, equals));
                instructions.Insert(j++, new CodeInstruction(OpCodes.Brfalse_S, @else));
                instructions.Insert(j++, def.GetLoadArgInstruction());
                instructions.Insert(j++, new CodeInstruction(OpCodes.Ldfld, mass));
                instructions.Insert(j++, index);
                instructions.Insert(j++, new CodeInstruction(OpCodes.Ldelem_R4));
                instructions.Insert(j++, new CodeInstruction(OpCodes.Call, get_units));
                instructions.Insert(j++, new CodeInstruction(OpCodes.Br_S, endif));
                return true;
            }
        }

        // добавляем ядра в посылку
        [HarmonyPatch(typeof(Immigration), "ConfigureCarePackages")]
        private static class Immigration_ConfigureCarePackages
        {
            private static bool Prepare() => ModOptions.Instance.CarePackage.Enabled;

            private static bool Condition(Tag tag)
            {
                return (GameClock.Instance.GetCycle() >= ModOptions.Instance.CarePackage.MinCycle)
                    && (!ModOptions.Instance.CarePackage.RequireDiscovered
                        || DiscoveredResources.Instance.IsDiscovered(tag));
            }

            private static void Postfix(List<CarePackageInfo> ___carePackages)
            {
                var core = new CarePackageInfo(ID, 1, () => Condition(ID));
                ___carePackages.Add(core);
            }
        }

        // добавляем ядра во всякий хлам:
        // стол директора, добавляем возможность обыскать
        // сетлокер и сырая воркабле должны быть добавлены в гамеобъект раньше любой другой воркабле, 
        // иначе хрень получается, поэтому транспилером
        [HarmonyPatch(typeof(PropFacilityDeskConfig), nameof(PropFacilityDeskConfig.CreatePrefab))]
        private static class PropFacilityDeskConfig_CreatePrefab
        {
            private static Demolishable InjectSetLocker(GameObject go)
            {
                var workable = go.AddOrGet<Workable>();
                workable.synchronizeAnims = false;
                workable.resetProgressOnStop = true;
                var setLocker = go.AddOrGet<SetLocker>();
                setLocker.machineSound = "VendingMachine_LP";
                setLocker.overrideAnim = "anim_break_kanim";
                setLocker.dropOffset = new Vector2I(1, 1);
                go.AddOrGet<LoopingSounds>();
                go.AddOrGet<MassiveHeatSinkCoreSpawner>().chance =
                    ModOptions.Instance.GravitasPOIChance.RarePOIChance / 100f;
                return go.AddOrGet<Demolishable>();
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var AddOrGetDemolishable = typeof(EntityTemplateExtensions).GetMethodSafe(nameof(EntityTemplateExtensions.AddOrGet), true, typeof(GameObject))?.MakeGenericMethod(typeof(Demolishable));
                var injectSetLocker = typeof(PropFacilityDeskConfig_CreatePrefab).GetMethodSafe(nameof(InjectSetLocker), true, PPatchTools.AnyArguments);
                if (AddOrGetDemolishable != null && injectSetLocker != null)
                {
                    instructions = PPatchTools.ReplaceMethodCallSafe(instructions, AddOrGetDemolishable, injectSetLocker).ToList();
                    return true;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(PropFacilityDeskConfig), nameof(PropFacilityDeskConfig.OnPrefabInit))]
        private static class PropFacilityDeskConfig_OnPrefabInit
        {
            private static void Postfix(GameObject inst)
            {
                var databank = DatabankHelper.ID;
                var loot = new string[] { FieldRationConfig.ID };
                for (int i = 0; i < 25; i++)
                {
                    loot = loot.Append(databank);
                }
                var component = inst.GetComponent<SetLocker>();
                component.possible_contents_ids = new string[][] { loot };
                component.ChooseContents();
            }
        }

        // спутники
        [HarmonyPatch]
        private static class PropSurfaceSatellite3Config_CreatePrefab
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new List<MethodBase>()
                {
                    typeof(PropSurfaceSatellite1Config).GetMethodSafe(nameof(PropSurfaceSatellite1Config.CreatePrefab), false),
                    typeof(PropSurfaceSatellite2Config).GetMethodSafe(nameof(PropSurfaceSatellite2Config.CreatePrefab), false),
                    typeof(PropSurfaceSatellite3Config).GetMethodSafe(nameof(PropSurfaceSatellite3Config.CreatePrefab), false),
                };
            }

            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<MassiveHeatSinkCoreSpawner>().chance =
                    ModOptions.Instance.GravitasPOIChance.RarePOIChance / 100f;
            }
        }

        // шкафчик и торг-о-мат
        [HarmonyPatch]
        private static class SetLockerConfig_VendingMachineConfig_CreatePrefab
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new MethodBase[] {
                    typeof(SetLockerConfig).GetMethodSafe(nameof(SetLockerConfig.CreatePrefab), false, PPatchTools.AnyArguments),
                    typeof(VendingMachineConfig).GetMethodSafe(nameof(VendingMachineConfig.CreatePrefab), false, PPatchTools.AnyArguments),
                };
            }

            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<MassiveHeatSinkCoreSpawner>().chance =
                    ModOptions.Instance.GravitasPOIChance.LockerPOIChance / 100f;
            }
        }

        // добавляем ядра в космос в длц:
        // нужно избежать записи id ядра в сейф, иначе при отключении мода будет плохо

        // выборы следующего для сбора артифакта в космических пои.
        // пусть будет условное значение "" означает ядро (некрасиво, но что поделать)
        [HarmonyPatch(typeof(ArtifactPOIStates.Instance), nameof(ArtifactPOIStates.Instance.PickNewArtifactToHarvest))]
        private static class ArtifactPOIStates_Instance_PickNewArtifactToHarvest
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active();

            private static bool Prefix(ArtifactPOIStates.Instance __instance, int ___numHarvests)
            {
                // пропускаем пои с явно прописанным стартовым артифактом
                if (___numHarvests <= 0 && !string.IsNullOrEmpty(__instance.configuration.GetArtifactID()))
                {
                    return true;
                }
                var chance = ModOptions.Instance.SpaceOutPOIChance;
                if (__instance.CanHarvestArtifact() && chance.Enabled && UnityEngine.Random.Range(0f, 100f) < chance.SpacePOIChance)
                {
                    __instance.artifactToHarvest = string.Empty;
                    return false;
                }
                return true;
            }
#if DEBUG
            private static void Postfix(ArtifactPOIStates.Instance __instance)
            {
                Debug.Log("PickNewArtifactToHarvest: " + __instance.artifactToHarvest);
            }
#endif
        }

        // передача ранее выбранного артифакта
        [HarmonyPatch(typeof(ArtifactPOIStates.Instance), nameof(ArtifactPOIStates.Instance.GetArtifactToHarvest))]
        private static class ArtifactPOIStates_Instance_GetArtifactToHarvest
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active();

            private static bool Prefix(ArtifactPOIStates.Instance __instance, ref string __result)
            {
                if (__instance.CanHarvestArtifact() && ModOptions.Instance.SpaceOutPOIChance.Enabled && __instance.artifactToHarvest == string.Empty)
                {
                    __result = ID;
                    return false;
                }
                return true;
            }
#if DEBUG
            private static void Postfix(string __result)
            {
                Debug.Log("GetArtifactToHarvest: " + __result);
            }
#endif
        }

        private static bool IsNotGameArtifactID(string id) => string.IsNullOrEmpty(id) || id == ID;

        // запись о проанализированном артифакте на станции анализа
        // тут тоже нужно избежать записи id в сейф
        [HarmonyPatch(typeof(ArtifactSelector), nameof(ArtifactSelector.RecordArtifactAnalyzed))]
        private static class ArtifactSelector_RecordArtifactAnalyzed
        {
            private static bool Prefix(string id, ref bool __result)
            {
                if (IsNotGameArtifactID(id))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        // и еще одна предосторожность
        [HarmonyPatch(typeof(ArtifactSelector), "OnSpawn")]
        private static class ArtifactSelector_OnSpawn
        {
            private static void Prefix(Dictionary<ArtifactType, List<string>> ___placedArtifacts, List<string> ___analyzedArtifatIDs)
            {
                foreach (var x in ___placedArtifacts.Keys)
                {
                    ___placedArtifacts[x].RemoveAll(IsNotGameArtifactID);
                }
                ___analyzedArtifatIDs.RemoveAll(IsNotGameArtifactID);
            }
        }
    }
}

