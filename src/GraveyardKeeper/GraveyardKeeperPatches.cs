using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace GraveyardKeeper
{
    internal sealed class GraveyardKeeperPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(GraveyardKeeperPatches));
            new POptions().RegisterOptions(this, typeof(GraveyardKeeperOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        // составляем списки всех растений
        private static List<Tag> RegularPlants = new List<Tag>();
        private static List<Tag> SingleHarvestPlants = new List<Tag>();
        private static List<Tag> NonYieldingPlants = new List<Tag>();
        private static List<Tag> PlantsToSpawn = new List<Tag>();

        [HarmonyPatch(typeof(Db), nameof(Db.PostProcess))]
        private static class Db_PostProcess
        {
            private static void Postfix()
            {
                foreach (var go in Assets.GetPrefabsWithComponent<UprootedMonitor>())
                {
                    if (go.GetComponent<BasicForagePlantPlanted>() != null)
                    {
                        SingleHarvestPlants.Add(go.PrefabID());
                    }
                    else if (go.GetComponent<Crop>() != null)
                    {
                        RegularPlants.Add(go.PrefabID());
                    }
                    else if (go.GetComponent<SeedProducer>() != null)
                    {
                        NonYieldingPlants.Add(go.PrefabID());
                    }
                }
            }
        }

        // ухх.. есть аж три места для проверки допустимости посадки семечка белкой.
        // и все разные. и два приватные. и все нужны чтобы всё правильно работало
        // первое: в запросе патхфиндера
        private static PlantableCellQuery modifiedPlantableCellQuery; // модифицированый запрос. будем использовать только для проверки

        private static readonly IDetouredField<PlantableCellQuery, int> PlantableCellQuery_plantDetectionRadius =
            PDetours.DetourField<PlantableCellQuery, int>("plantDetectionRadius");
        private static readonly IDetouredField<PlantableCellQuery, int> PlantableCellQuery_maxPlantsInRadius =
            PDetours.DetourField<PlantableCellQuery, int>("maxPlantsInRadius");

        delegate bool PlantableCellQuery_CheckValidPlotCell(PlantableCellQuery query, PlantableSeed seed, int plant_cell);
        private static readonly PlantableCellQuery_CheckValidPlotCell plantableCellQuery_CheckValidPlotCell =
            PDetours.Detour<PlantableCellQuery_CheckValidPlotCell>(typeof(PlantableCellQuery), "CheckValidPlotCell");

        // второе: у самой белки в задаче посадки
        delegate bool SeedPlantingStates_CheckValidPlotCell(SeedPlantingStates.Instance smi, PlantableSeed seed, int cell, out PlantablePlot plot);
        private static readonly SeedPlantingStates_CheckValidPlotCell seedPlantingStates_CheckValidPlotCell =
            PDetours.Detour<SeedPlantingStates_CheckValidPlotCell>(typeof(SeedPlantingStates), "CheckValidPlotCell");

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            GraveyardKeeperOptions.Reload();
            PlantsToSpawn.Clear();
            if (GraveyardKeeperOptions.Instance.non_yielding_plants)
                PlantsToSpawn.AddRange(NonYieldingPlants);
            if (GraveyardKeeperOptions.Instance.single_harvest_plants)
                PlantsToSpawn.AddRange(SingleHarvestPlants);
            if (GraveyardKeeperOptions.Instance.regular_plants)
                PlantsToSpawn.AddRange(RegularPlants);
            if (PlantsToSpawn.Count == 0)
                PlantsToSpawn.Add(EvilFlowerConfig.ID);
            modifiedPlantableCellQuery = new PlantableCellQuery();
            PlantableCellQuery_plantDetectionRadius.Set(modifiedPlantableCellQuery, 1);
            PlantableCellQuery_maxPlantsInRadius.Set(modifiedPlantableCellQuery, 5);
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
                });
            }
        }

        // игре очень не нравиться когда тело дупликанта пытается перенести кто-то кто сам не дупликант
        // поэтому уничтожаем труп и заменяем его семечкой когда белка пытается всять
        [HarmonyPatch(typeof(SeedPlantingStates), "PickupComplete")]
        private static class SeedPlantingStates_PickupComplete
        {
            private static Pickupable SplitCorpse(Pickupable pickupable, float amount, GameObject prefab)
            {
                if (pickupable != null && pickupable.HasTag(GameTags.Corpse))
                {
                    var position = pickupable.transform.GetPosition();
                    var seed = GameUtil.KInstantiate(Assets.GetPrefab(EvilFlowerConfig.SEED_ID), position, Grid.SceneLayer.Ore);
                    seed.SetActive(true);
                    seed.AddTag(GameTags.Corpse);
                    var fx = FXHelpers.CreateEffect("collapse_buildings_kanim", position);
                    fx.Play("idle", KAnim.PlayMode.Once);
                    fx.destroyOnAnimComplete = true;
                    CreatureHelpers.DeselectCreature(pickupable.gameObject);
                    Util.KDestroyGameObject(pickupable.gameObject);
                    return seed.GetComponent<Pickupable>();
                }
                else
                    return EntitySplitter.Split(pickupable, amount, prefab);
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var EntitySplitter_Split = typeof(EntitySplitter).GetMethodSafe(nameof(EntitySplitter.Split), true, typeof(Pickupable), typeof(float), typeof(GameObject));
                var splitCorpse = typeof(SeedPlantingStates_PickupComplete).GetMethodSafe(nameof(SplitCorpse), true, typeof(Pickupable), typeof(float), typeof(GameObject));

                if (EntitySplitter_Split != null && splitCorpse != null)
                    return PPatchTools.ReplaceMethodCallSafe(instructions, EntitySplitter_Split, splitCorpse);
                else
                {
                    string methodName = method.DeclaringType.FullName + "." + method.Name;
                    Debug.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                    return instructions;
                }
            }
        }

        // выращиваем несколько случайных растений поблизости от места выбранного белкой
        [HarmonyPatch(typeof(SeedPlantingStates), "PlantComplete")]
        private static class SeedPlantingStates_PlantComplete
        {
            private static readonly CellOffset[] cellOffsets;
            static SeedPlantingStates_PlantComplete()
            {
                var offsets = new List<CellOffset>() { CellOffset.none };
                foreach (var x in new int[] { -1, 1, -2, 2, -3, 3 })
                    foreach (var y in new int[] { 0, 1, -1, 2, -2 })
                        offsets.Add(new CellOffset(x, y));
                cellOffsets = offsets.ToArray();
            }

            private static void Prefix(SeedPlantingStates.Instance smi)
            {
                if (!smi.IsNullOrStopped() && smi.targetSeed != null && smi.targetSeed.HasTag(GameTags.Corpse) && smi.targetSeed.TryGetComponent<PlantableSeed>(out var target_seed))
                {
                    var planted_cells = ListPool<int, SeedPlantingStates>.Allocate();
                    PlantsToSpawn.Shuffle();
                    int squirrel_cell = Grid.PosToCell(smi);
                    // сначала проверяем возможность посадить растение, используя семечку что принесла белка
                    // подменяя у неё ид растения и направление роста
                    for (int i = 0; i < PlantsToSpawn.Count; i++)
                    {
                        var plant_prefab = Assets.GetPrefab(PlantsToSpawn[i]);
                        if (plant_prefab != null)
                        {
                            target_seed.PlantID = PlantsToSpawn[i];
                            target_seed.direction = SingleEntityReceptacle.ReceptacleDirection.Top;
                            Tag seed_prefab_tag = EvilFlowerConfig.SEED_ID;

                            if (plant_prefab.TryGetComponent<SeedProducer>(out var seed_producer))
                            {
                                var seed_prefab = Assets.GetPrefab(seed_producer.seedInfo.seedId);
                                if (seed_prefab != null)
                                {
                                    seed_prefab_tag = seed_prefab.PrefabID();
                                    if (seed_prefab.TryGetComponent<PlantableSeed>(out var plantableSeed))
                                    {
                                        target_seed.direction = plantableSeed.direction;
                                    }
                                }
                            }

                            for (int j = 0; j < cellOffsets.Length; j++)
                            {
                                int target_cell = Grid.OffsetCell(squirrel_cell, cellOffsets[j]);
                                if (planted_cells.Contains(target_cell))
                                    continue;

                                if (plantableCellQuery_CheckValidPlotCell(modifiedPlantableCellQuery, target_seed, target_cell)
                                    && seedPlantingStates_CheckValidPlotCell(smi, target_seed, target_cell, out var plot))
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
                            if (planted_cells.Count >= GraveyardKeeperOptions.Instance.max_plants_spawn)
                                break;
                        }
                    }
                    if (planted_cells.Count > 0)
                    {
                        // добавляем красявости
                        for (int k = 0; k < planted_cells.Count; k++)
                        {
                            GameUtil.KInstantiate(Assets.GetPrefab(PlantSparkleFXConfig.ID), Grid.CellToPos(planted_cells[k]), Grid.SceneLayer.FXFront)
                                .SetActive(true);
                        }
                        // и удаляем трупное семечко
                        smi.GetComponent<Storage>().Drop(target_seed.gameObject);
                        Util.KDestroyGameObject(target_seed.gameObject);
                        smi.targetSeed = null;
                    }
                    else
                    {
                        // либо снимаем с него метку и возвращаем как было
                        target_seed.PlantID = EvilFlowerConfig.ID;
                        target_seed.direction = SingleEntityReceptacle.ReceptacleDirection.Top;
                        target_seed.RemoveTag(GameTags.Corpse);
                    }
                    planted_cells.Recycle();
                }
            }
        }

        // снимаем трупную метку с семечка если белка отвлеклась или не смогла посадить
        [HarmonyPatch(typeof(SeedPlantingStates), "DropAll")]
        private static class SeedPlantingStates_DropAll
        {
            private static void Prefix(SeedPlantingStates.Instance smi)
            {
                var seed = smi.GetComponent<Storage>().FindFirst(GameTags.Corpse);
                if (seed != null)
                    seed.RemoveTag(GameTags.Corpse);
            }
        }

        // предотвратить попытки утащить в могилу зарезервированых белкой трупов
        [HarmonyPatch(typeof(Grave.StatesInstance), nameof(Grave.StatesInstance.CreateFetchTask))]
        private static class Grave_StatesInstance_CreateFetchTask
        {
            private static void Postfix(FetchChore ___chore)
            {
                ___chore.forbiddenTags = ___chore.forbiddenTags.AddItem(GameTags.Creatures.ReservedByCreature).ToArray();
                ___chore.forbidHash = FetchChore.ComputeHashCodeForTags(___chore.forbiddenTags);
            }
        }
    }
}
