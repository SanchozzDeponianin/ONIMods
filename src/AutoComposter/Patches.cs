using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using STRINGS;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using SanchozzONIMods.Shared;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace AutoComposter
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            base.OnLoad(harmony);
            RotPileSilentNotification.Patch(harmony);
            // гипотетически могут пофиксить путём удаления, поэтому завернём
            try
            {
                harmony.Patch(typeof(Compostable), nameof(Compostable.MarkForCompost),
                    prefix: new HarmonyMethod(typeof(Compostable_MarkForCompost), nameof(Compostable_MarkForCompost.Prefix)));
            }
            catch (Exception e)
            {
                Utils.LogExcWarn(e);
            }
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
            // издеваемся над категорией
            if (ModOptions.Instance.hide_from_filters)
            {
                Filterable.filterableCategories.Remove(GameTags.Compostable);
                TUNING.STORAGEFILTERS.NOT_EDIBLE_SOLIDS.Remove(GameTags.Compostable);
                TUNING.STORAGEFILTERS.STORAGE_LOCKERS_STANDARD.Remove(GameTags.Compostable);
                TUNING.STORAGEFILTERS.STORAGE_SOLID_CARGO_BAY.Remove(GameTags.Compostable);
            }
            if (ModOptions.Instance.make_special)
                TUNING.STORAGEFILTERS.SPECIAL_STORAGE.Add(GameTags.Compostable);
            if (ModOptions.Instance.hide_from_resources)
                GameTags.UnitCategories.Remove(GameTags.Compostable);
        }

        internal static readonly HashSet<Tag> DirectlyCompostables = new();
        internal static readonly HashSet<Tag> CanBeMarkedCompostables = new();
        internal static readonly Dictionary<Tag, Tag> FreshToCompostable = new();

        private static readonly HashSet<Tag> StorageCompostCategories = new();
        private static readonly Dictionary<Tag, Tag> PrefabToCompostCategory = new();

        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            foreach (var go in Assets.GetPrefabsWithTag(GameTags.Compostable))
            {
                go.TryGetComponent(out KPrefabID compost);
                if (go.TryGetComponent(out Compostable compostable))
                {
                    if (compostable.isMarkedForCompost)
                    {
                        CanBeMarkedCompostables.Add(compost.PrefabTag);
                        compostable.originalPrefab.TryGetComponent(out KPrefabID fresh);
                        FreshToCompostable[fresh.PrefabTag] = compost.PrefabTag;
                        MakeCompostCategory(fresh);
                    }
                }
                else
                {
                    DirectlyCompostables.Add(compost.PrefabTag);
                    MakeCompostCategory(compost);
                }
            }
            ConfigureCompost(Assets.GetBuildingDef(CompostConfig.ID).BuildingComplete);
        }

        // дополнительные теги-категории для штук которые могут быть скомпостированы, для использования в storageFilters
        private static void MakeCompostCategory(KPrefabID kprefab)
        {
            var category = DiscoveredResources.GetCategoryForEntity(kprefab);
            var compost = new Tag("Compost" + category.Name);
            if (!StorageCompostCategories.Contains(compost))
            {
                var proper = category.ProperNameStripLink();
                if (string.IsNullOrEmpty(proper))
                    proper = UI.StripLinkFormatting(Strings.Get("STRINGS.MISC.TAGS." + category.Name.ToUpper()));
                compost = TagManager.Create(compost.Name, proper);
                StorageCompostCategories.Add(compost);
            }
            PrefabToCompostCategory[kprefab.PrefabTag] = compost;
        }

        private static void ConfigureCompost(GameObject go)
        {
            var storage = go.AddOrGet<Storage>();
            storage.storageFilters = new(StorageCompostCategories);
            if (!ModOptions.Instance.hide_from_filters)
                storage.storageFilters.Add(GameTags.Compostable);
            storage.storageFullMargin = TUNING.STORAGE.STORAGE_LOCKER_FILLED_MARGIN;
            storage.allowSettingOnlyFetchMarkedItems = false;
            storage.storageID = GameTags.Compostable;

            var garbage = go.AddComponent<Storage>();
            garbage.capacityKg = storage.capacityKg;
            garbage.storageID = GameTags.Garbage;

            var filterable = go.AddOrGet<TreeFilterable>();
            filterable.autoSelectStoredOnLoad = false;
            filterable.copySettingsEnabled = false;
            filterable.dropIncorrectOnFilterChange = false;
            filterable.filterByStorageCategoriesOnSpawn = false;
            filterable.preventAutoAddOnDiscovery = true;
            filterable.tintOnNoFiltersSet = false;

            go.AddOrGet<AutoComposter>();
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            Game.Instance.Subscribe((int)GameHashes.AddedFetchable, OnAddedFetchable);
        }

        private static void OnAddedFetchable(object data)
        {
            var go = data as GameObject;
            if (go != null && go.TryGetComponent(out KPrefabID kprefab) && DiscoveredResources.Instance != null
                && PrefabToCompostCategory.TryGetValue(kprefab.PrefabTag, out var category))
            {
                DiscoveredResources.Instance.DiscoverCategory(category, kprefab.PrefabTag);
            }
        }

        // унпинываем компостируемое если категория скрыта
        [HarmonyPatch(typeof(WorldInventory), nameof(WorldInventory.OnSpawn))]
        private static class WorldInventory_OnSpawn
        {
            private static bool Prepare() => ModOptions.Instance.hide_from_resources;
            private static void Prefix(WorldInventory __instance)
            {
                __instance.pinnedResources.RemoveAll(tag => CanBeMarkedCompostables.Contains(tag));
                __instance.notifyResources.RemoveAll(tag => CanBeMarkedCompostables.Contains(tag));
            }
        }

        // корректируем прибитый гвоздями IsFunctional
        [HarmonyPatch(typeof(FilteredStorage), nameof(FilteredStorage.IsFunctional))]
        private static class FilteredStorage_IsFunctional
        {
            private static bool Prefix(FilteredStorage __instance, ref bool __result)
            {
                if (__instance.root is AutoComposter composter && !composter.IsNullOrDestroyed())
                {
                    __result = composter.IsOperational;
                    return false;
                }
                return true;
            }
        }

        // предотвращаем вываливание компостируемых штук при загрузке сейфа и при автокомпостировании
        //[HarmonyPatch(typeof(Compostable), nameof(Compostable.MarkForCompost))]
        private static class Compostable_MarkForCompost
        {
            internal static bool Prefix(Compostable __instance)
            {
                if (__instance != null && __instance.TryGetComponent(out Pickupable pickupable) && pickupable.storage != null)
                {
                    DiscoveredResources.Instance.Discover(pickupable.KPrefabID.PrefabTag);
                    return false;
                }
                return true;
            }
        }

        // предотвращаем вываливание гнилья когда еда сгнивает
        private static void StoreRotPile(GameObject rotten, object source)
        {
            if (rotten != null && !source.IsNullOrDestroyed())
            {
                Pickupable pickupable = null;
                if (source is Rottable.Instance smi)
                    pickupable = smi.GetComponent<Pickupable>();
                else if (source is RotPile pile)
                    pile.TryGetComponent(out pickupable);
                if (pickupable != null && pickupable.storage != null && pickupable.storage.storageID == GameTags.Compostable)
                {
                    pickupable.storage.Store(rotten, true);
                    if (rotten.PrefabID() == RotPileConfig.ID && rotten.TryGetComponent(out KSelectable selectable))
                        selectable.SetName(ITEMS.FOOD.ROTPILE.NAME);
                }
            }
        }

        [HarmonyPatch]
        private static class Rottable_InitializeStates_Spoiled
        {
            private static List<MethodBase> targets;
            private static IEnumerable<MethodBase> TargetMethods()
            {
                if (targets == null)
                {
                    targets = new();
                    // их тут два таких. придется маслать оба
                    foreach (var type in typeof(Rottable).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (type.IsDefined(typeof(CompilerGeneratedAttribute)))
                        {
                            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                            {
                                var parameters = method.GetParameters();
                                if (method.ReturnType == typeof(void) && parameters.Length == 1
                                    && parameters[0].ParameterType == typeof(Rottable.Instance))
                                {
                                    targets.Add(method);
                                }
                            }
                        }
                    }
                }
                return targets;
            }
            private static bool Prepare() => TargetMethods() != null;

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var spawn = typeof(Scenario).GetMethodSafe(nameof(Scenario.SpawnPrefab), true, PPatchTools.AnyArguments);
                var set_active = typeof(GameObject).GetMethod(nameof(GameObject.SetActive));
                var store = typeof(Patches).GetMethodSafe(nameof(StoreRotPile), true, PPatchTools.AnyArguments);
                if (spawn == null || set_active == null || store == null)
                    return false;

                int i = instructions.FindIndex(inst => inst.Calls(spawn));
                if (i == -1 || !instructions[i + 1].IsStloc())
                    return true;
                var ld_go = instructions[i + 1].GetMatchingLoadInstruction();

                i = instructions.FindIndex(inst => inst.Calls(set_active));
                if (i == -1)
                    return false;

                instructions.Insert(++i, ld_go);
                instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_1));
                instructions.Insert(++i, new CodeInstruction(OpCodes.Call, store));
                return true;
            }
        }

        [HarmonyPatch(typeof(RotPile), nameof(RotPile.ConvertToElement))]
        private static class RotPile_ConvertToElement
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static bool transpiler(ref List<CodeInstruction> instructions)
            {
                var spawn = typeof(Substance).GetMethodSafe(nameof(Substance.SpawnResource), false, PPatchTools.AnyArguments);
                var store = typeof(Patches).GetMethodSafe(nameof(StoreRotPile), true, PPatchTools.AnyArguments);
                if (spawn == null || store == null)
                    return false;

                int i = instructions.FindIndex(inst => inst.Calls(spawn));
                if (i == -1 || !instructions[i + 1].IsStloc())
                    return false;
                var ld_go = instructions[i + 1].GetMatchingLoadInstruction();
                i++;
                instructions.Insert(++i, ld_go);
                instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                instructions.Insert(++i, new CodeInstruction(OpCodes.Call, store));
                return true;
            }
        }
    }
}
