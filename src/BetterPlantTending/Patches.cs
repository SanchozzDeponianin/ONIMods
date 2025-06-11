using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace BetterPlantTending
{
    using static ModAssets;

    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            ModOptions.Reload();
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            Init();
            if (DlcManager.IsContentSubscribed(DlcManager.DLC2_ID))
                TreesPatches.SpaceTree_ResolveTooltipCallback_Patch();
        }

        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            // помечаем растения в декоративных горшках тем же тегом что растения в ящике
            // чтобы жучинкусы могли достать до них с пола
            // плюс с проверкой если сторонний мод добавил декоратифные семена в фермерские блоки
            foreach (var go in Assets.GetPrefabsWithComponent<PlantablePlot>())
            {
                if (go.TryGetComponent(out PlantablePlot plot) && plot.HasDepositTag(GameTags.DecorSeed)
                    && !go.TryGetComponent(out SimCellOccupier _))
                {
                    plot.tagOnPlanted = GameTags.PlantedOnFloorVessel;
                }
            }
            // жучинкусы могут убобрять растения совместно, а также с мимикой
            if (ModOptions.Instance.allow_tending_together)
            {
                foreach (var critter in Assets.GetPrefabsWithComponent<ChoreConsumer>())
                {
                    if (critter.TryGetComponent(out ChoreConsumer consumer) && consumer.choreTable != null)
                    {
                        var entry = consumer.choreTable.GetEntry<CropTendingStates.Def>();
                        if (entry.stateMachineDef is CropTendingStates.Def def)
                            def.ignoreEffectGroup = new HashedString[] { def.effectId };
                    }
                }
            }
        }

        // подавим нотификацию когда собирается гнилой мутантный урожай
        [HarmonyPatch(typeof(RotPile), nameof(RotPile.TryCreateNotification))]
        private static class RotPile_TryCreateNotification
        {
            private static bool Prepare() => DlcManager.FeaturePlantMutationsEnabled();
            private static bool Prefix(RotPile __instance)
            {
                return __instance.GetProperName() != global::STRINGS.ITEMS.FOOD.ROTPILE.NAME;
            }
        }

        // дополнительные семена безурожайных растений
        [HarmonyPatch]
        private static class EntityTemplates_CreateAndRegisterSeedForPlant
        {
            private static MethodBase TargetMethod()
            {
                return typeof(EntityTemplates).GetOverloadWithMostArguments(nameof(EntityTemplates.CreateAndRegisterSeedForPlant),
                    true, typeof(GameObject), typeof(IHasDlcRestrictions));
            }

            private static void Postfix(GameObject plant, List<Tag> additionalTags)
            {
                if (plant.GetComponent<Crop>() == null)
                {
                    if (additionalTags != null && additionalTags.Contains(GameTags.DecorSeed))
                    {
                        plant.AddOrGet<ExtraSeedProducer>().isDecorative = true;
                        Tinkerable.MakeFarmTinkerable(plant);
                        plant.GetComponent<KPrefabID>().prefabInitFn += go =>
                        {
                            if (go.TryGetComponent(out Tinkerable tinkerable))
                                tinkerable.userMenuAllowed = ModOptions.Instance.allow_tinker_decorative;
                        };
                    }
                    else if (plant.PrefabID() == OxyfernConfig.ID || plant.PrefabID() == ColdBreatherConfig.ID)
                    {
                        plant.AddOrGet<ExtraSeedProducer>().isDecorative = false;
                        Tinkerable.MakeFarmTinkerable(plant);
                    }
                }
            }
        }

        #region Tinkerable
        // заспавним доп семя при убобрении фермерами
        [HarmonyPatch(typeof(Tinkerable), nameof(Tinkerable.OnCompleteWork))]
        private static class Tinkerable_OnCompleteWork
        {
            private static void Postfix(Tinkerable __instance, WorkerBase worker)
            {
                if (__instance.TryGetComponent<ExtraSeedProducer>(out var producer))
                    producer.CreateExtraSeed(worker);
            }
        }

        // предотвращаем убобрение фермерами 
        // если растение засохло или полностью выросло
        // или декоротивное доп семя заспавнилось
        // при изменении состояния растения нужно перепроверить задачу
        // заодно чиним что качается механика заместо фермерства
        [HarmonyPatch(typeof(Tinkerable), nameof(Tinkerable.OnPrefabInit))]
        private static class Tinkerable_OnPrefabInit
        {
            private static void Postfix(Tinkerable __instance)
            {
                if (__instance.tinkerMaterialTag == FarmStationConfig.TINKER_TOOLS)
                {
                    __instance.attributeConverter = Db.Get().AttributeConverters.PlantTendSpeed;
                    __instance.skillExperienceSkillGroup = Db.Get().SkillGroups.Farming.Id;
                    // чтобы обновить чору после того как белка извлекла семя
                    __instance.Subscribe((int)GameHashes.SeedProduced, Tinkerable.OnEffectRemovedDelegate);
                    // чтобы обновить чору когда растение засыхает/растёт/выросло
                    if (ModOptions.Instance.prevent_tending_grown_or_wilting)
                    {
                        __instance.Subscribe((int)GameHashes.Wilt, Tinkerable.OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.WiltRecover, Tinkerable.OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.Grow, Tinkerable.OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.CropSleep, Tinkerable.OnEffectRemovedDelegate);
                        __instance.Subscribe((int)GameHashes.CropWakeUp, Tinkerable.OnEffectRemovedDelegate);
                        if (ModOptions.Instance.space_tree_adjust_productivity)
                            __instance.Subscribe((int)GameHashes.TagsChanged, OnTagsChanged);
                    }
                }
            }

            private static readonly EventSystem.IntraObjectHandler<Tinkerable> OnTagsChanged = new((tinkerable, data) =>
            {
                if (((TagChangedEventData)data).tag == SpaceTreePlant.SpaceTreeReadyForHarvest)
                    tinkerable.QueueUpdateChore();
            });
        }

        // если убобрение не нужно - эмулируем как будто оно уже есть
        [HarmonyPatch(typeof(Tinkerable), nameof(Tinkerable.HasEffect))]
        private static class Tinkerable_HasEffect
        {
            private static void Postfix(Tinkerable __instance, ref bool __result)
            {
                if (__result)
                    return;
                if (__instance.tinkerMaterialTag == FarmStationConfig.TINKER_TOOLS)
                {
                    if (ModOptions.Instance.prevent_tending_grown_or_wilting)
                    {
                        if (__instance.HasTag(GameTags.Wilting) || __instance.HasTag(GameTags.FullyGrown)) // засохло или полностью выросло
                        {
                            __result = true;
                            return;
                        }
                        // полностью выросло или не растёт
                        if (__instance.TryGetComponent<Growing>(out var growing) && (growing.ReachedNextHarvest() || !growing.IsGrowing()))
                        {
                            __result = true;
                            return;
                        }
                        // ветка сиропового дерева:
                        if (ModOptions.Instance.space_tree_adjust_productivity)
                        {
                            // ускорение сиропа включено => дерево заполнено сиропом и ожидает сбора
                            if (__instance.HasTag(SpaceTreePlant.SpaceTreeReadyForHarvest))
                            {
                                __result = true;
                                return;
                            }
                        }
                        else
                        {
                            // ускорение сиропа выключено => ветка полностью выросла
                            var stbi = __instance.GetSMI<SpaceTreeBranch.Instance>();
                            if (!stbi.IsNullOrStopped() && stbi.IsBranchFullyGrown)
                            {
                                __result = true;
                                return;
                            }
                        }
                    }
                    if (__instance.TryGetComponent<ExtraSeedProducer>(out var producer) && !producer.ShouldFarmTinkerTending)
                    {
                        __result = true;
                        return;
                    }
                }
            }
        }

        // ишшо одно место чиним что качается механика заместо фермерства
        [HarmonyPatch(typeof(TinkerStation), nameof(TinkerStation.OnPrefabInit))]
        private static class TinkerStation_OnPrefabInit
        {
            private static void Postfix(TinkerStation __instance)
            {
                if (__instance.outputPrefab == FarmStationConfig.TINKER_TOOLS)
                {
                    __instance.attributeConverter = Db.Get().AttributeConverters.HarvestSpeed;
                    __instance.skillExperienceSkillGroup = Db.Get().SkillGroups.Farming.Id;
                }
            }
        }
        #endregion

        // исправление для отображения шанса доп семян в интерфейсе ферм и кодексе.
        // заодно начал отображаться и декор.
        // а то обычно для неурожайных растений "эффекты" вообще не отображаются.
        // грязновато. но ладно.
        [HarmonyPatch(typeof(GameUtil), nameof(GameUtil.GetPlantEffectDescriptors))]
        private static class GameUtil_GetPlantEffectDescriptors
        {
            private static Component GetTwoComponents(GameObject go)
            {
                if (go != null)
                {
                    if (go.TryGetComponent<Growing>(out var growing))
                        return growing;
                    if (go.TryGetComponent<ExtraSeedProducer>(out var producer))
                        return producer;
                }
                return null;
            }
            /*
        --- Growing growing = go.GetComponent<Growing>();
        +++ Growing growing = go.GetComponent<Growing>() ?? go.GetComponent<ExtraSeedProducer>();
            bool flag = growing == null;
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var getComponent = typeof(GameObject).GetMethod(nameof(GameObject.GetComponent), Type.EmptyTypes).MakeGenericMethod(typeof(Growing));
                var getTwoComponents = typeof(GameUtil_GetPlantEffectDescriptors).GetMethodSafe(nameof(GetTwoComponents), true, PPatchTools.AnyArguments);
                if (getComponent != null && getTwoComponents != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(getComponent))
                        {
                            instructions[i] = new CodeInstruction(OpCodes.Call, getTwoComponents);
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        #region Fertilization
        // вопервых исправление неконсистентности поглощения твердых удобрений засохшими растениями после загрузки сейфа
        // патчим FertilizationMonitor чтобы был больше похож на IrrigationMonitor
        // вовторых останавливаем поглощения воды/удобрений при других причинах отсутствии роста,
        // для ентого внедряем собственный компонент
        private static bool ShouldAbsorb(StateMachine.Instance smi)
        {
            bool absorb = !smi.gameObject.HasTag(GameTags.Wilting);
            if (absorb && ModOptions.Instance.prevent_fertilization_irrigation_not_growning
                && smi.gameObject.TryGetComponent<ExtendedFertilizationIrrigationMonitor>(out var monitor))
                absorb = absorb && monitor.ShouldAbsorb;
            return absorb;
        }

        [HarmonyPatch(typeof(FertilizationMonitor.Instance), nameof(FertilizationMonitor.Instance.StartAbsorbing))]
        private static class FertilizationMonitor_Instance_StartAbsorbing
        {
            private static bool Prefix(FertilizationMonitor.Instance __instance)
            {
                bool absorb = ShouldAbsorb(__instance);
                if (!absorb)
                    __instance.StopAbsorbing();
                return absorb;
            }
        }

        [HarmonyPatch(typeof(FertilizationMonitor), nameof(FertilizationMonitor.InitializeStates))]
        private static class FertilizationMonitor_InitializeStates
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return PPatchTools.ReplaceConstant(instructions, (int)GameHashes.WiltRecover, (int)GameHashes.TagsChanged, true);
            }

            private static void Postfix(FertilizationMonitor __instance)
            {
                __instance.replanted.fertilized.absorbing
                    .Enter(ExtendedFertilizationIrrigationMonitor.Subscribe)
                    .Exit(ExtendedFertilizationIrrigationMonitor.Unsubscribe);
            }
        }

        [HarmonyPatch(typeof(IrrigationMonitor.Instance), nameof(IrrigationMonitor.Instance.UpdateAbsorbing))]
        private static class IrrigationMonitor_Instance_UpdateIrrigation
        {
            private static void Prefix(IrrigationMonitor.Instance __instance, ref bool allow)
            {
                allow = allow && ShouldAbsorb(__instance);
            }
        }

        [HarmonyPatch(typeof(IrrigationMonitor), nameof(IrrigationMonitor.InitializeStates))]
        private static class IrrigationMonitor_InitializeStates
        {
            private static void Postfix(IrrigationMonitor __instance)
            {
                __instance.replanted.irrigated.absorbing
                    .Enter(ExtendedFertilizationIrrigationMonitor.Subscribe)
                    .Exit(ExtendedFertilizationIrrigationMonitor.Unsubscribe);
            }
        }

        [HarmonyPatch]
        private static class EntityTemplates_ExtendPlantToFertilizableIrrigated
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new List<MethodBase>()
                {
                    typeof(EntityTemplates).GetMethodSafe(nameof(EntityTemplates.ExtendPlantToFertilizable), true, PPatchTools.AnyArguments),
                    typeof(EntityTemplates).GetMethodSafe(nameof(EntityTemplates.ExtendPlantToIrrigated), true, typeof(GameObject), typeof(PlantElementAbsorber.ConsumeInfo[])),
                };
            }

            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<ExtendedFertilizationIrrigationMonitor>();
                Debug.LogFormat("ExtendPlantToFertilizableIrrigated {0}", __result.name);
            }
        }

        // чтобы пересчитать необходимость поглощения воды/удобрений:
        // когда меняется состояние ветки дерева
        [HarmonyPatch(typeof(PlantBranch), nameof(PlantBranch.InitializeStates))]
        private static class PlantBranch_InitializeStates
        {
            private static void Postfix(PlantBranch __instance)
            {
                __instance.root.EventHandler(GameHashes.Grow, (smi, data) =>
                {
                    if (smi.HasTrunk)
                        ExtendedFertilizationIrrigationMonitor.QueueUpdateAbsorbing(smi.trunk);
                });
            }
        }

        [HarmonyPatch(typeof(SpaceTreePlant.Instance), nameof(SpaceTreePlant.Instance.OnBranchWiltStateChanged))]
        private static class SpaceTreePlant_Instance_OnBranchWiltStateChanged
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC2_ID);

            private static void Postfix(SpaceTreePlant.Instance __instance)
            {
                ExtendedFertilizationIrrigationMonitor.QueueUpdateAbsorbing(__instance);
            }
        }

        // когда сироповое дерево не/может производить сироп
        [HarmonyPatch(typeof(SpaceTreePlant), nameof(SpaceTreePlant.InitializeStates))]
        private static class SpaceTreePlant_InitializeStates
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC2_ID);

            private static void Postfix(SpaceTreePlant __instance)
            {
                __instance.production.producing
                    .Enter(ExtendedFertilizationIrrigationMonitor.QueueUpdateAbsorbing)
                    .Exit(ExtendedFertilizationIrrigationMonitor.QueueUpdateAbsorbing);
            }
        }

        // когда меняется состояние ветки лозы
        [HarmonyPatch(typeof(VineBranch), nameof(VineBranch.InitializeStates))]
        private static class VineBranch_InitializeStates
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC4_ID);

            private static void QueueUpdate(VineBranch.Instance smi)
            {
                ExtendedFertilizationIrrigationMonitor.QueueUpdateAbsorbing(smi.MotherSMI);
            }
            private static void Postfix(VineBranch __instance)
            {
                __instance.undevelopedBranch.growing
                    .Enter(QueueUpdate)
                    .Exit(QueueUpdate);
                __instance.mature.healthy.growing
                    .Enter(QueueUpdate)
                    .Exit(QueueUpdate);
            }
        }
        #endregion
    }
}
