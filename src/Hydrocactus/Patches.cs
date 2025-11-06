using System.Collections.Generic;
using ProcGen;
using TUNING;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace Hydrocactus
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            ModOptions.Reload();
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        // тюнингуем урожайность. значение по умолчанию убыточно
        [HarmonyPatch(typeof(FilterPlantConfig), nameof(FilterPlantConfig.CreatePrefab))]
        private static class FilterPlantConfig_CreatePrefab
        {
            private static void Prefix()
            {
                var cropId = SimHashes.Water.ToString();
                for (int i = CROPS.CROP_TYPES.Count - 1; i >= 0; i--)
                {
                    var crop = CROPS.CROP_TYPES[i];
                    if (crop.cropId == cropId)
                    {
                        crop.numProduced = ModOptions.Instance.yield_amount;
                        CROPS.CROP_TYPES[i] = crop;
                        break;
                    }
                }
            }

            private static void Postfix(GameObject __result)
            {
                __result.RemoveTag(GameTags.DeprecatedContent);
            }
        }

        // добавляем семена кактуса в посылку
        [HarmonyPatch(typeof(Immigration), "ConfigureCarePackages")]
        private static class Immigration_ConfigureCarePackages
        {
            private static void Postfix(List<CarePackageInfo> ___carePackages)
            {
                var seed = new CarePackageInfo(FilterPlantConfig.SEED_ID, ModOptions.Instance.carepackage_seeds_amount, null);
                ___carePackages.Add(seed);
            }
        }

        // добавляем кактусы и семена в ворлдген
        [HarmonyPatch(typeof(SettingsCache), nameof(SettingsCache.LoadFiles), typeof(List<Klei.YamlIO.Error>))]
        private static class SettingsCache_LoadFiles
        {
            // id из worldgen/mobs.yaml
            private const string plant_id = "med_FilterPlant";
            private const string seed_id = "med_FilterPlantSeed";
            private static readonly Dictionary<string, List<string>> AffectedBiomes;
            static SettingsCache_LoadFiles()
            {
                var plant_and_seed = new List<string>() { plant_id, seed_id };
                var seed_only = new List<string>() { seed_id };
                AffectedBiomes = new Dictionary<string, List<string>>()
                {
                    { "biomes/Sedimentary/Basic", plant_and_seed },
                    { "biomes/Sedimentary/Basic_CO2", seed_only },
                    { "biomes/Sedimentary/Metal_CO2", seed_only },
                    { "biomes/Sedimentary/Desert", plant_and_seed },
                    { "biomes/Sedimentary/Snowy", plant_and_seed },
                    { "biomes/Forest/Basic", plant_and_seed },
                    { "biomes/Forest/BasicOxy", plant_and_seed },
                    { "biomes/Forest/Metal", plant_and_seed },
                    { "biomes/Forest/Snowy", plant_and_seed },
                    { "expansion1::biomes/Sedimentary/Basic_Dense", plant_and_seed },
                    { "expansion1::biomes/Sedimentary/Basic_CO2_Dense", seed_only },
                    { "expansion1::biomes/Sedimentary/Metal_CO2_Dense", seed_only },
                    { "expansion1::biomes/Forest/Chasm", plant_and_seed },
                    { "expansion1::biomes/Forest/Core", plant_and_seed },
                    { "expansion1::biomes/Wasteland/Basic", plant_and_seed },
                    { "expansion1::biomes/Wasteland/Sulfur", plant_and_seed },
                    { "expansion1::biomes/Swamp/Basic", seed_only },
                    { "expansion1::biomes/Swamp/Water_CO2", plant_and_seed },
                };
            }
            private static void Postfix(bool __result)
            {
                if (__result && SettingsCache.subworlds != null)
                {
                    foreach (var subworld in SettingsCache.subworlds)
                    {
                        if (subworld.Value?.biomes != null)
                        {
                            foreach (var biome in subworld.Value.biomes)
                            {
                                if (biome != null && biome.tags != null && AffectedBiomes.TryGetValue(biome.name, out var tags_to_inject))
                                {
                                    foreach (var tag in tags_to_inject)
                                    {
                                        if (!biome.tags.Contains(tag))
                                        {
                                            biome.tags.Add(tag);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
