using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace WhereMyLoot
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            ModOptions.Reload();
        }

        // отключить разрушение
        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            // мозг и его пижамы
            foreach (var id in new[] { MegaBrainTankConfig.ID, GravitasContainerConfig.ID })
            {
                Assets.GetBuildingDef(id).BuildingComplete.GetComponent<KPrefabID>().prefabSpawnFn += go =>
                {
                    if (go.TryGetComponent(out Demolishable demolishable))
                        demolishable.allowDemolition = !ModOptions.Instance.protect.megabraintank;
                };
            }

            // мутатор жеготных
            Assets.GetBuildingDef(GravitasCreatureManipulatorConfig.ID).BuildingComplete.GetComponent<KPrefabID>().prefabSpawnFn += go =>
            {
                if (go.TryGetComponent(out Demolishable demolishable))
                    demolishable.allowDemolition = !ModOptions.Instance.protect.creaturemanipulator;
            };

            // завод биоботов
            Assets.GetBuildingDef(MorbRoverMakerConfig.ID).BuildingComplete.GetComponent<KPrefabID>().prefabSpawnFn += go =>
            {
                if (go.TryGetComponent(out Demolishable demolishable))
                    demolishable.allowDemolition = !ModOptions.Instance.protect.morbrovermaker;
            };

            // принтерсептор и его сортир
            foreach (var id in new[] { HijackedHeadquartersConfig.ID, GravitasBathroomStallConfig.ID })
            {
                Assets.GetBuildingDef(id).BuildingComplete.GetComponent<KPrefabID>().prefabSpawnFn += go =>
                {
                    if (go.TryGetComponent(out Demolishable demolishable))
                        demolishable.allowDemolition = !ModOptions.Instance.protect.hijackedheadquarters;
                };
            }

            // калибратор
            Assets.GetPrefab("GeneShuffler").GetComponent<KPrefabID>().prefabSpawnFn += go =>
            {
                if (go.TryGetComponent(out Demolishable demolishable))
                    demolishable.allowDemolition = !ModOptions.Instance.protect.geneshuffler;
            };

            // аетн
            Assets.GetBuildingDef(MassiveHeatSinkConfig.ID).BuildingComplete.GetComponent<KPrefabID>().prefabSpawnFn += go =>
            {
                if (go.TryGetComponent(out Demolishable demolishable))
                    demolishable.allowDemolition = !ModOptions.Instance.protect.massiveheatsink;
            };

            // спутники
            foreach (var id in new[] { PropSurfaceSatellite1Config.ID, PropSurfaceSatellite2Config.ID, PropSurfaceSatellite3Config.ID })
            {
                Assets.GetPrefab(id).GetComponent<KPrefabID>().prefabSpawnFn += go =>
                {
                    if (go.TryGetComponent(out Demolishable demolishable))
                        demolishable.allowDemolition = !ModOptions.Instance.protect.satellites;
                };
            }
        }

        // сортир после разблокировки пароля
        [HarmonyPatch(typeof(GravitasBathroomStall), nameof(GravitasBathroomStall.InitializeStates))]
        private static class GravitasBathroomStall_InitializeStates
        {
            private static void Postfix(GravitasBathroomStall __instance)
            {
                __instance.root
                    .Enter(smi => GravitasBathroomSortirInstance = smi.gameObject)
                    .Exit(smi => GravitasBathroomSortirInstance = null);
                __instance.complete.Enter(smi =>
                {
                    if (smi.gameObject.TryGetComponent(out Demolishable demolishable))
                        demolishable.allowDemolition = true;
                });
            }
        }

        private static GameObject GravitasBathroomSortirInstance;

        // если сортир уже был уничтожен, разблокируем пароль
        [HarmonyPatch(typeof(HijackedHeadquarters), nameof(HijackedHeadquarters.InitializeStates))]
        private static class HijackedHeadquarters_InitializeStates
        {
            private static void Postfix(HijackedHeadquarters __instance)
            {
                __instance.operational.passcode.idle_locked.ScheduleAction("", 5f, TestSortirDemolished);
            }
            private static void TestSortirDemolished(HijackedHeadquarters.Instance smi)
            {
                if (smi.IsNullOrStopped() || !smi.IsInsideState(smi.sm.operational.passcode.idle_locked))
                    return;
                if (GravitasBathroomSortirInstance != null)
                    return;
                foreach (var container in ClusterManager.Instance.WorldContainers)
                {
                    if (SaveGame.Instance.worldGenSpawner.GetSpawnablesWithTag(GravitasBathroomStallConfig.ID, container.id).Count > 0)
                        return;
                }
                smi.UnlockPrinterceptor();
            }
        }

        // ансиент спесимен
        [HarmonyPatch(typeof(MajorFossilDigSite), nameof(MajorFossilDigSite.MakeItDemolishable))]
        private static class MajorFossilDigSite_MakeItDemolishable
        {
            private static void Postfix(MajorFossilDigSite.Instance smi)
            {
                if (smi.gameObject.TryGetComponent(out Demolishable demolishable))
                    demolishable.allowDemolition = !ModOptions.Instance.protect.fossildig;
            }
        }

        // долутываем при разрушении
        [HarmonyPatch(typeof(Demolishable), nameof(Demolishable.TriggerDestroy))]
        internal static class Demolishable_TriggerDestroy
        {
            private static void Prefix(Demolishable __instance)
            {
                if (__instance != null && !__instance.destroyed)
                {
                    // лутаемые объекты типа шкафчиков
                    if (__instance.TryGetComponent(out SetLocker setLocker) && !setLocker.used)
                        setLocker.CompleteChore();

                    // шкафчик c семенами возле биоботов
                    var seedLocker = __instance.GetSMI<GravitasLocker.Instance>();
                    if (!seedLocker.IsNullOrStopped() && seedLocker.HasContents)
                        seedLocker.SpawnLoot();

                    // объекты которые можно "осмотреть", запись в кодекс
                    if (__instance.TryGetComponent(out LoreBearer loreBearer) && !loreBearer.BeenClicked)
                        loreBearer.OnClickRead();

                    // калибратор. если не использован - нужно дропнуть зарядник
                    if (__instance.TryGetComponent(out GeneShuffler geneShuffler) && !geneShuffler.IsConsumed)
                    {
                        geneShuffler.IsConsumed = true;
                        Scenario.SpawnPrefab(Grid.PosToCell(__instance), 0, 1, GeneShufflerRechargeConfig.ID, Grid.SceneLayer.Front).SetActive(true);
                        PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Plus,
                            Assets.GetPrefab(GeneShufflerRechargeConfig.ID.ToTag()).GetProperName(), __instance.transform);
                    }

                    // штука для разблокировки исследований
                    var smi = __instance.GetSMI<POITechItemUnlocks.Instance>();
                    if (!smi.IsNullOrStopped() && smi.IsInsideState(smi.sm.locked))
                    {
                        smi.sm.seenNotification.Set(true, smi);
                        smi.UnlockTechItems();
                        smi.sm.pendingChore.Set(false, smi);
                        if (!string.IsNullOrEmpty(smi.def.loreUnlockId))
                            Game.Instance.unlocks.Unlock(smi.def.loreUnlockId, true);
                    }
                }
            }
        }

        // подавляем надоедливый диалог об обнаружении новых записей лора
        [HarmonyPatch(typeof(LoreBearer), nameof(LoreBearer.OnClickRead))]
        internal static class LoreBearer_OnClickRead
        {
            private static void DeactivateScreenIsMarkedForDemolition(LoreBearer loreBearer, KScreen screen)
            {
                if (screen != null && loreBearer != null
                    && loreBearer.TryGetComponent<Demolishable>(out var demolishable) && demolishable.isMarkedForDemolition)
                {
                    screen.Deactivate();
                }
            }
            /*
            --- var infoDialogScreen = LoreBearer.ShowPopupDialog().blabla.AddDefaultOK();
                blablablabla;
                // и на версиях 531669 и ранее вот это много раз:
                if (чтототам)
                {
                    блаблабла;
            ---     return;
            +++     goto end:
                }
                blablablabla;
                // и в конце
            --- return;
            +++ goto end:
            +++ end:
            +++ DeactivateScreenIsMarkedForDemolition(this, infoDialogScreen);
                return;
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator IL)
            {
                return instructions.Transpile(original, IL, transpiler);
            }
            private static bool transpiler(ref List<CodeInstruction> instructions, ILGenerator IL)
            {
                var addDefaultOK = typeof(InfoDialogScreen).GetMethodSafe(nameof(InfoDialogScreen.AddDefaultOK), false, typeof(bool));
                var deactivateScreen = typeof(LoreBearer_OnClickRead).GetMethodSafe(nameof(DeactivateScreenIsMarkedForDemolition), true, PPatchTools.AnyArguments);
                bool result = false;
                if (addDefaultOK != null && deactivateScreen != null)
                {
                    var label = IL.DefineLabel();
                    CodeInstruction ldloc_Screen = null;
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(addDefaultOK) && instructions[i + 1].IsStloc())
                        {
                            ldloc_Screen = TranspilerUtils.GetMatchingLoadInstruction(instructions[i + 1]);
                            continue;
                        }
                        if (instructions[i].opcode == OpCodes.Ret)
                        {
                            instructions[i].opcode = OpCodes.Br_S;
                            instructions[i].operand = label;
                        }
                    }
                    var end = new CodeInstruction(OpCodes.Nop);
                    end.labels.Add(label);
                    instructions.Add(end);
                    if (ldloc_Screen != null)
                    {
                        instructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                        instructions.Add(ldloc_Screen);
                        instructions.Add(new CodeInstruction(OpCodes.Call, deactivateScreen));
                        result = true;
                    }
                    instructions.Add(new CodeInstruction(OpCodes.Ret));
                }
                return result;
            }
        }
    }
}
