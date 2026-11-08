using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace GraveyardKeeper
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

        // составляем списки всех растений
        private static readonly HashSet<Tag> RegularPlants = new();
        private static readonly HashSet<Tag> SingleHarvestPlants = new();
        private static readonly HashSet<Tag> NonYieldingPlants = new();
        private static readonly HashSet<Tag> AquaticPlants = new();
        private static readonly HashSet<Tag> TerrestrePlants = new();
        private static readonly List<Tag> PlantsToSpawnAquatic = new();
        private static readonly List<Tag> PlantsToSpawnTerrestre = new();

        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            foreach (var go in Assets.GetPrefabsWithComponent<UprootedMonitor>())
            {
                var kpid = go.GetComponent<KPrefabID>();
                if (kpid.HasTag(GameTags.DeprecatedContent) || kpid.HasTag(GameTags.PlantBranch))
                    continue;
                if (go.TryGetComponent(out BasicForagePlantPlanted _))
                {
                    SingleHarvestPlants.Add(kpid.PrefabTag);
                }
                else if (go.TryGetComponent(out Crop _) || kpid.PrefabTag == VineMotherConfig.ID || kpid.PrefabTag == SeaTreeRootConfig.ID)
                {
                    RegularPlants.Add(kpid.PrefabTag);
                }
                else if (go.TryGetComponent(out SeedProducer _))
                {
                    NonYieldingPlants.Add(kpid.PrefabTag);
                }
                if (kpid.HasTag(GameTags.PlantIsAquatic))
                {
                    AquaticPlants.Add(kpid.PrefabTag);
                    // водное, но может быть частично погруженным
                    if (go.TryGetComponent(out PressureVulnerable vulnerable) && !vulnerable.allCellsMustBeSafe
                        && go.TryGetComponent(out OccupyArea occupy) && (occupy.GetWidthInCells() > 1 || occupy.GetHeightInCells() > 1))
                    {
                        TerrestrePlants.Add(kpid.PrefabTag);
                    }
                    // водное, но не из ДЛЦ 5, разрешим посадку как наземное
                    if (kpid.requiredDlcIds == null || !kpid.requiredDlcIds.Contains(DlcManager.DLC5_ID))
                    {
                        TerrestrePlants.Add(kpid.PrefabTag);
                    }
                }
                else
                {
                    TerrestrePlants.Add(kpid.PrefabTag);
                    // наземное, но не может утонуть
                    if (!go.TryGetComponent(out DrowningMonitor drowning) || drowning.livesUnderWater)
                    {
                        AquaticPlants.Add(kpid.PrefabTag);
                    }
                }
            }
#if DEBUG
            Debug.LogFormat("SingleHarvestPlants:\n{0}", SingleHarvestPlants.Join("\n"));
            Debug.LogFormat("RegularPlants:\n{0}", RegularPlants.Join("\n"));
            Debug.LogFormat("NonYieldingPlants:\n{0}", NonYieldingPlants.Join("\n"));
            Debug.LogFormat("AquaticPlants:\n{0}", AquaticPlants.Join("\n"));
            Debug.LogFormat("TerrestrePlants:\n{0}", TerrestrePlants.Join("\n"));
#endif
        }

        // модифицированый запрос патхфиндера. будем использовать только для проверки
        private static PlantableCellQuery modifiedPlantableCellQuery;

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            ModOptions.Reload();
            PlantsToSpawnAquatic.Clear();
            PlantsToSpawnTerrestre.Clear();

            if (ModOptions.Instance.non_yielding_plants)
                PlantsToSpawnTerrestre.AddRange(NonYieldingPlants);
            if (ModOptions.Instance.single_harvest_plants)
                PlantsToSpawnTerrestre.AddRange(SingleHarvestPlants);
            if (ModOptions.Instance.regular_plants)
                PlantsToSpawnTerrestre.AddRange(RegularPlants);

            PlantsToSpawnTerrestre.RemoveAll(tag => !Game.IsCorrectDlcActiveForCurrentSave(Assets.GetPrefab(tag).GetComponent<KPrefabID>()));
            PlantsToSpawnAquatic.AddRange(PlantsToSpawnTerrestre);

            PlantsToSpawnTerrestre.RemoveAll(tag => !TerrestrePlants.Contains(tag));
            if (PlantsToSpawnTerrestre.Count == 0)
                PlantsToSpawnTerrestre.Add(EvilFlowerConfig.ID);

            PlantsToSpawnAquatic.RemoveAll(tag => !AquaticPlants.Contains(tag));
            if (PlantsToSpawnAquatic.Count == 0)
                PlantsToSpawnAquatic.Add(BasicFabricMaterialPlantConfig.ID);
#if DEBUG
            Debug.LogFormat("PlantsToSpawnTerrestre:\n{0}", PlantsToSpawnTerrestre.Join("\n"));
            Debug.LogFormat("PlantsToSpawnAquatic:\n{0}", PlantsToSpawnAquatic.Join("\n"));
#endif
            modifiedPlantableCellQuery = new PlantableCellQuery()
            {
                plantDetectionRadius = 1,
                maxPlantsInRadius = 5
            };
        }

        // грязноватый хак. делаем труп семечкой
        // todo: если когда либо появится воскрешающий мод, надо добавить обратную операцию
        [HarmonyPatch(typeof(RationalAi), nameof(RationalAi.InitializeStates))]
        private static class RationalAi_InitializeStates
        {
            private static void Postfix(RationalAi __instance)
            {
                __instance.dead
                    .ToggleTag(GameTags.CropSeed)
                    .Enter(smi =>
                {
                    var plantableSeed = smi.gameObject.AddOrGet<PlantableSeed>();
                    plantableSeed.PlantID = EvilFlowerConfig.ID;
                    plantableSeed.direction = SingleEntityReceptacle.ReceptacleDirection.Top;
                    plantableSeed.requireBackWallPlantable = false;
                });
            }
        }

        // оказалось что белки пытаются резервировать даже семена лежащие на складах и переносимые дупликами
        // запретим это
        [HarmonyPatch(typeof(SeedPlantingStates), nameof(SeedPlantingStates.FindSeed))]
        private static class SeedPlantingStates_FindSeed
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }
            /*
            --- if (блабла && !seed.HasTag(GameTags.Creatures.ReservedByCreature) && блабла)
            +++ if (блабла && !seed.HasTag(GameTags.Creatures.ReservedByCreature) && !seed.HasTag(GameTags.Stored) &&  блабла)
            */
            private static bool HasStoredTag(Component cmp, Tag tag)
            {
                return cmp.HasTag(tag) || cmp.HasTag(GameTags.Stored);
            }
            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var reserved = typeof(GameTags.Creatures).GetFieldSafe(nameof(GameTags.Creatures.ReservedByCreature), true);
                var hastag = typeof(KPrefabIDExtensions).GetMethodSafe(nameof(KPrefabIDExtensions.HasTag), true, typeof(Component), typeof(Tag));
                var hasstored = typeof(SeedPlantingStates_FindSeed).GetMethodSafe(nameof(HasStoredTag), true, typeof(Component), typeof(Tag));
                if (reserved != null && hastag != null && hasstored != null)
                {
                    int i = instructions.FindIndex(inst => inst.LoadsField(reserved));
                    if (i != -1 && instructions[i + 1].Calls(hastag))
                    {
                        instructions[i + 1].operand = hasstored;
                        return true;
                    }
                }
                return false;
            }
        }

        // ранее игре очень не нравилось когда тело дупликанта пытается перенести кто-то кто сам не дупликант
        // теперь больше не крашится. но нужно быть начеку. и если что, то опять вернуть трюк
        // (уничтожаем труп и заменяем его семечкой когда белка пытается всять)
        [HarmonyPatch(typeof(SeedPlantingStates), nameof(SeedPlantingStates.PickupComplete))]
        private static class SeedPlantingStates_PickupComplete
        {
            private static Pickupable SplitCorpse(Pickupable pickupable, float amount, GameObject prefab)
            {
                if (pickupable != null && pickupable.HasTag(GameTags.Corpse))
                {
                    var position = pickupable.transform.GetPosition();
                    var fx = FXHelpers.CreateEffect("collapse_buildings_kanim", position);
                    fx.Play("idle", KAnim.PlayMode.Once);
                    fx.destroyOnAnimComplete = true;
                    CreatureHelpers.DeselectCreature(pickupable.gameObject);
                    return pickupable;
                }
                else
                    return EntitySplitter.Split(pickupable, amount, prefab);
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var EntitySplitter_Split = typeof(EntitySplitter).GetMethodSafe(nameof(EntitySplitter.Split), true, typeof(Pickupable), typeof(float), typeof(GameObject));
                var splitCorpse = typeof(SeedPlantingStates_PickupComplete).GetMethodSafe(nameof(SplitCorpse), true, typeof(Pickupable), typeof(float), typeof(GameObject));

                if (EntitySplitter_Split != null && splitCorpse != null)
                {
                    instructions = PPatchTools.ReplaceMethodCallSafe(instructions, EntitySplitter_Split, splitCorpse).ToList();
                    return true;
                }
                return false;
            }
        }

        private static bool ConfigureFakeSeed(PlantableSeed fake_seed, Tag as_plant, out Tag real_seed_prefab)
        {
            real_seed_prefab = EvilFlowerConfig.SEED_ID;
            fake_seed.direction = SingleEntityReceptacle.ReceptacleDirection.Top;
            fake_seed.requireBackWallPlantable = false;
            var plant_prefab = Assets.GetPrefab(as_plant);
            if (plant_prefab != null)
            {
                fake_seed.PlantID = as_plant;
                if (plant_prefab.TryGetComponent<SeedProducer>(out var seed_producer))
                {
                    var seed_prefab = Assets.GetPrefab(seed_producer.seedInfo.seedId);
                    if (seed_prefab != null && seed_prefab.TryGetComponent<PlantableSeed>(out var plantableSeed))
                    {
                        real_seed_prefab = seed_prefab.PrefabID();
                        fake_seed.direction = plantableSeed.direction;
                        fake_seed.requireBackWallPlantable = plantableSeed.requireBackWallPlantable;
                    }
                }
                return true;
            }
            return false;
        }

        // перед поиском белкой места для посадки, подменяем характеристики "трупной семечки" на случайновыбранное растение
        [HarmonyPatch(typeof(SeedPlantingStates), nameof(SeedPlantingStates.FindDirtPlot))]
        private static class SeedPlantingStates_FindDirtPlot
        {
            private static void Prefix(SeedPlantingStates.Instance smi)
            {
                if (!smi.IsNullOrStopped() && smi.targetSeed != null && smi.targetSeed.HasTag(GameTags.Corpse)
                    && smi.targetSeed.TryGetComponent<PlantableSeed>(out var target_seed))
                {
                    List<Tag> plants_to_spawn;
                    if (smi.HasTag(GameTags.SwimmingCreature))
                    {
                        target_seed.PlantID = BasicFabricMaterialPlantConfig.ID;
                        plants_to_spawn = PlantsToSpawnAquatic;
                    }
                    else
                    {
                        target_seed.PlantID = EvilFlowerConfig.ID;
                        plants_to_spawn = PlantsToSpawnTerrestre;
                    }
                    plants_to_spawn.Shuffle();
                    ConfigureFakeSeed(target_seed, plants_to_spawn[0], out _);
                }
            }
        }

        // выращиваем несколько случайных растений поблизости от места выбранного белкой
        [HarmonyPatch(typeof(SeedPlantingStates), nameof(SeedPlantingStates.PlantComplete))]
        private static class SeedPlantingStates_PlantComplete
        {
            private static readonly CellOffset[] cellOffsets;
            static SeedPlantingStates_PlantComplete()
            {
                var offsets = new List<CellOffset>() { CellOffset.none };
                foreach (var x in new int[] { -1, 1, -2, 2, -3, 3 })
                    foreach (var y in new int[] { 0, 1, -1, 2, -2, 3 })
                        offsets.Add(new CellOffset(x, y));
                cellOffsets = offsets.ToArray();
            }

            private static bool Prefix(SeedPlantingStates.Instance smi)
            {
                if (!smi.IsNullOrStopped() && smi.targetSeed != null && smi.targetSeed.HasTag(GameTags.Corpse)
                    && smi.targetSeed.TryGetComponent<PlantableSeed>(out var target_seed))
                {
                    var planted_cells = ListPool<int, SeedPlantingStates>.Allocate();
                    int squirrel_cell = Grid.PosToCell(smi);
                    modifiedPlantableCellQuery.canUseBackwall = smi.def.canPlantBackWall;
                    var plants_to_spawn = smi.HasTag(GameTags.SwimmingCreature) ? PlantsToSpawnAquatic : PlantsToSpawnTerrestre;
                    // сначала проверяем возможность посадить растение, используя семечку что принесла белка
                    // подменяя у неё ид растения и направление роста
                    for (int i = 0; i < plants_to_spawn.Count; i++)
                    {
                        if (ConfigureFakeSeed(target_seed, plants_to_spawn[i], out Tag seed_prefab_tag))
                        {
                            for (int j = 0; j < cellOffsets.Length; j++)
                            {
                                int target_cell = Grid.OffsetCell(squirrel_cell, cellOffsets[j]);
                                if (planted_cells.Contains(target_cell))
                                    continue;

                                if ((modifiedPlantableCellQuery.CheckValidPlotCell(target_seed, target_cell)
                                    || modifiedPlantableCellQuery.CheckValidBackWallPlotCell(target_seed, target_cell))
                                    && SeedPlantingStates.CheckValidPlotCell(smi, target_seed, target_cell, out var plot))
                                {
                                    // потом садим, с новой семечкой
                                    var new_seed_go = GameUtil.KInstantiate(Assets.GetPrefab(seed_prefab_tag), Grid.CellToPos(target_cell), Grid.SceneLayer.Ore);
                                    new_seed_go.SetActive(true);
                                    var new_seed = new_seed_go.AddOrGet<PlantableSeed>();
                                    new_seed.PlantID = target_seed.PlantID;
                                    if (plot != null && plot.Occupant == null)
                                    {
                                        plot.ForceDeposit(new_seed_go);
                                    }
                                    else
                                    {
                                        new_seed.TryPlant(true);
                                    }
                                    planted_cells.Add(target_cell);
                                    break;
                                }
                            }
                            if (planted_cells.Count >= ModOptions.Instance.max_plants_spawn)
                                break;
                        }
                    }
                    smi.GetComponent<Storage>().Drop(target_seed.gameObject);
                    if (planted_cells.Count > 0)
                    {
                        // добавляем красявости
                        for (int k = 0; k < planted_cells.Count; k++)
                        {
                            GameUtil.KInstantiate(Assets.GetPrefab(PlantSparkleFXConfig.ID), Grid.CellToPos(planted_cells[k]), Grid.SceneLayer.FXFront)
                                .SetActive(true);
                        }
                        // и удаляем труп
                        Util.KDestroyGameObject(target_seed.gameObject);
                    }
                    else
                    {
                        // либо возвращаем как было
                        target_seed.PlantID = EvilFlowerConfig.ID;
                        target_seed.direction = SingleEntityReceptacle.ReceptacleDirection.Top;
                        target_seed.requireBackWallPlantable = false;
                        // и роняем труп
                        if (!GameComps.Fallers.Has(target_seed.gameObject))
                            GameComps.Fallers.Add(target_seed.gameObject, Vector2.zero);
                    }
                    planted_cells.Recycle();
                    smi.targetSeed = null;
                    smi.seed_cell = Grid.InvalidCell;
                    smi.targetPlot = null;
                    return false;
                }
                return true;
            }
        }

        // предотвратить попытки утащить в могилу зарезервированых белкой трупов
        [HarmonyPatch(typeof(Grave.StatesInstance), nameof(Grave.StatesInstance.CreateFetchTask))]
        private static class Grave_StatesInstance_CreateFetchTask
        {
            private static void Postfix(FetchChore ___chore)
            {
                var forbiddenTags = ___chore.forbiddenTags.Append(GameTags.Creatures.ReservedByCreature);
                Traverse.Create(___chore).Field<Tag[]>(nameof(FetchChore.forbiddenTags)).Value = forbiddenTags;
                ___chore.forbidHash = FetchChore.ComputeHashCodeForTags(___chore.forbiddenTags);
            }
        }
    }
}
