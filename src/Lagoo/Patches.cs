using System;
using System.Collections.Generic;
using Klei.AI;
using TUNING;
using ProcGen;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace Lagoo
{
    using static LagooConfig;

    internal sealed class Patches : KMod.UserMod2
    {
        public const string squirrel_kanim = "squirrel_kanim";
        public const string baby_squirrel_kanim = "baby_squirrel_kanim";
        public const string egg_squirrel_kanim = "egg_squirrel_kanim";

        public const string lagoo_kanim = "lagoo_kanim";
        public const string baby_lagoo_kanim = "baby_lagoo_kanim";
        public const string egg_lagoo_kanim = "egg_lagoo_kanim";

        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            var kagm = new KAnimGroupManager();
            kagm.RegisterAnims(squirrel_kanim, lagoo_kanim);
            kagm.RegisterAnims(baby_squirrel_kanim, baby_lagoo_kanim);
            kagm.RegisterAnims(egg_squirrel_kanim, egg_lagoo_kanim);
        }

        // несмотря на наличие подсистемы SymbolOverride
        // многое гвоздями прибито к KBatchedAnimController.AnimFiles[0]
        // чтобы не городить 100500 патчей, поманипулируем анимацией
        // загрузим свою анимацию в общую группу с клеевской и продуплируем символы
        // а заодно и зе-ордер поправим. штатным белкам всеравно, а тут глаза должны быть поверх мордочки.

        private static Dictionary<KAnimFile, KAnimFile> AnimFileMapping = new();

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
            var squirrel = Assets.GetAnim(squirrel_kanim);
            var lagoo = Assets.GetAnim(lagoo_kanim);
            MergeSymbols(squirrel, lagoo);
            AdjustZOrder(squirrel, "sq_mouth", "sq_eye");
            AdjustZOrder(lagoo, "sq_mouth", "sq_eye");
            var baby_squirrel = Assets.GetAnim(baby_squirrel_kanim);
            var baby_lagoo = Assets.GetAnim(baby_lagoo_kanim);
            MergeSymbols(baby_squirrel, baby_lagoo);
            AdjustZOrder(baby_squirrel, "sq_mouth_baby", "sq_eye_baby");
            AdjustZOrder(baby_lagoo, "sq_mouth_baby", "sq_eye_baby");
            MergeSymbols(Assets.GetAnim(egg_squirrel_kanim), Assets.GetAnim(egg_lagoo_kanim));
        }

        private static void MergeSymbols(KAnimFile to, KAnimFile from)
        {
            if (to != null && to.IsBuildLoaded && from != null && from.IsBuildLoaded)
            {
                AnimFileMapping[to] = from;
                var to_data = to.GetData();
                var from_data = from.GetData();
                if (to_data.batchTag == from_data.batchTag)
                {
                    var to_build = to_data.build;
                    var from_build = from_data.build;
                    if (to_build != null && to_build.symbols != null && from_build != null && from_build.symbols != null)
                    {
                        to_build.symbols = to_build.symbols.Append(from_build.symbols);
                    }
                }
            }
        }

        private static void AdjustZOrder(KAnimFile file, KAnimHashedString find_symbol, KAnimHashedString move_after)
        {
            if (file != null && file.IsAnimLoaded)
            {
                var file_data = file.GetData();
                var group_data = KAnimBatchManager.Instance().GetBatchGroupData(file_data.animBatchTag);
                var frameElements = group_data.GetAnimFrameElements();
                for (int i = 0; i < file_data.animCount; i++)
                {
                    var anim = file_data.GetAnim(i);
                    for (int j = 0; j < anim.numFrames; j++)
                    {
                        if (group_data.TryGetFrame(j + anim.firstFrameIdx, out var frame))
                        {
                            var a = frameElements.FindIndex(frame.firstElementIdx, frame.numElements,
                                elem => elem.symbol == find_symbol);
                            var b = frameElements.FindLastIndex(frame.firstElementIdx + frame.numElements - 1, frame.numElements,
                                elem => elem.symbol == move_after);
                            if (a != -1 && b != -1 && a < b)
                            {
                                // использовать тут RemoveAt и Insert было бы крайне хреновой идеей, наверно
                                var element = frameElements[a];
                                for (int n = a; n < b; n++)
                                    frameElements[n] = frameElements[n + 1];
                                frameElements[b] = element;
                            }
                        }
                    }
                }
            }
        }

        // однако пара патчей все равно понадобится, так как продуплировать анимы == всё сложно
        // UI сприты
        [HarmonyPatch(typeof(Def), nameof(Def.GetUISpriteFromMultiObjectAnim))]
        private static class Def_GetUISpriteFromMultiObjectAnim
        {
            private static void Prefix(ref KAnimFile animFile, string animName, string symbolName)
            {
                if (((!string.IsNullOrEmpty(animName) && animName.StartsWith(ANIM_PREFIX))
                    || (!string.IsNullOrEmpty(symbolName) && symbolName.StartsWith(ANIM_PREFIX)))
                    && animFile != null && AnimFileMapping.ContainsKey(animFile))
                {
                    animFile = AnimFileMapping[animFile];
                }
            }
        }

        [HarmonyPatch(typeof(Def), nameof(Def.GetAnimFileFromPrefabWithTag))]
        [HarmonyPatch(new Type[] { typeof(GameObject), typeof(string), typeof(string) },
            new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
        private static class Def_GetAnimFileFromPrefabWithTag
        {
            private static void Postfix(ref KAnimFile __result, string animName)
            {
                if (!string.IsNullOrEmpty(animName) && animName.StartsWith(ANIM_PREFIX)
                    && __result != null && AnimFileMapping.ContainsKey(__result))
                {
                    __result = AnimFileMapping[__result];
                }
            }
        }

        // модификаторы шансов яйц
        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            // от диеты
            var rate = 0.025f / SquirrelTuning.STANDARD_CALORIES_PER_CYCLE;
            var food_tags = new HashSet<Tag> { ColdWheatConfig.ID };
            if (DlcManager.IsContentSubscribed(DlcManager.DLC2_ID))
            {
                food_tags.Add(SpaceTreeConfig.ID);
                food_tags.Add(CarrotPlantConfig.ID);
                food_tags.Add(HardSkinBerryPlantConfig.ID);
            }
            CREATURES.EGG_CHANCE_MODIFIERS.CreateDietaryModifier(ID, EGG_ID, food_tags, rate)();
            // и от температуры тоже
            CREATURES.EGG_CHANCE_MODIFIERS.CreateTemperatureModifier(ID + "LowTemp", EGG_ID,
                -70f + Constants.CELSIUS2KELVIN, -20f + Constants.CELSIUS2KELVIN, 8.333333E-05f, false)();
        }

        // обнимашки согревают
        [HarmonyPatch(typeof(HugMinionReactable), nameof(HugMinionReactable.ApplyEffects))]
        private static class HugMinionReactable_ApplyEffects
        {
            private static readonly HashedString WarmTouch = "WarmTouch";
            private static void Prefix(GameObject ___gameObject, GameObject ___reactor)
            {
                if (___gameObject.PrefabID() == ID && ___reactor.TryGetComponent(out Effects effects))
                {
                    var effect = effects.Get(WarmTouch);
                    if (effect == null)
                        effect = effects.Add(WarmTouch, true);
                    if (effect != null)
                        effect.timeRemaining = Mathf.Max(effect.timeRemaining, ModOptions.Instance.warm_touch_duration * Constants.SECONDS_PER_CYCLE);
                }
            }
        }

        // серу в кормушку
        [HarmonyPatch(typeof(CreatureFeederConfig), nameof(CreatureFeederConfig.ConfigurePost))]
        public static class CreatureFeederConfig_ConfigurePost
        {
            public static void Postfix(BuildingDef def)
            {
                var storageFilters = def.BuildingComplete.GetComponent<Storage>().storageFilters;
                foreach (var diet in DietManager.CollectDiets(new Tag[] { GameTags.Creatures.Species.SquirrelSpecies }))
                {
                    if (!storageFilters.Contains(diet.Key))
                        storageFilters.Add(diet.Key);
                }
            }
        }

        // на корм динозаверу
        [HarmonyPatch(typeof(BaseRaptorConfig), nameof(BaseRaptorConfig.StandardDiets))]
        private static class BaseRaptorConfig_StandardDiets
        {
            private static bool Prepare() => DlcManager.IsContentSubscribed(DlcManager.DLC4_ID);
            private static void Postfix(List<Diet.Info> __result)
            {
                foreach (var diet in __result)
                {
                    if (diet.foodType == Diet.Info.FoodType.EatButcheredPrey && diet.consumedTags.Contains(SquirrelConfig.ID))
                    {
                        diet.consumedTags.Add(ID);
                        diet.consumedTags.Add(BABY_ID);
                    }
                }
            }
        }

        // добавляем в посылку
        [HarmonyPatch(typeof(Immigration), nameof(Immigration.ConfigureCarePackages))]
        private static class Immigration_ConfigureCarePackages
        {
            private static void Postfix(List<CarePackageInfo> ___carePackages)
            {
                ___carePackages.Add(new CarePackageInfo(BABY_ID, 1f, null));
                ___carePackages.Add(new CarePackageInfo(EGG_ID, 2f, null));
            }
        }

        // добавляем в ворлдген
        [HarmonyPatch(typeof(SettingsCache), nameof(SettingsCache.LoadFiles), typeof(List<Klei.YamlIO.Error>))]
        private static class SettingsCache_LoadFiles
        {
            // id из worldgen/mobs.yaml
            private const string med = "med_SquirrelLagoo";
            private const string low = "low_SquirrelLagoo";
            private static readonly Dictionary<string, string> AffectedBiomes = new()
            {
                { "biomes/Forest/Snowy", med },
                { "biomes/Frozen/Dry", med },
                { "biomes/Frozen/Solid", med },
                { "biomes/Frozen/Wet", med },
                { "expansion1::biomes/Frozen/SaltySlush", med },
                { "dlc2::biomes/CarrotQuarry/Basic", low },
                { "dlc2::biomes/CarrotQuarry/Slush", low },
                { "dlc2::biomes/IceCaves/Basic", low },
                { "dlc2::biomes/IceCaves/Metal", low },
                { "dlc2::biomes/IceCaves/Oxy", low },
                { "dlc2::biomes/IceCaves/Snowy", low },
                { "dlc2::biomes/SugarWoods/Basic", low },
            };

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
                                if (biome != null && biome.tags != null && AffectedBiomes.TryGetValue(biome.name, out string tag_to_inject))
                                {
                                    if (!biome.tags.Contains(tag_to_inject))
                                    {
                                        biome.tags.Add(tag_to_inject);
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
