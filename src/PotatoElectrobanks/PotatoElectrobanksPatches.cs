using TUNING;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.PatchManager;
using UnityEngine;

namespace PotatoElectrobanks
{
    internal sealed class PotatoElectrobanksPatches : KMod.UserMod2
    {
        public static readonly Tag PotatoPortableBattery = TagManager.Create(nameof(PotatoPortableBattery));
        public static readonly Tag NonPotatoPortableBattery = TagManager.Create(nameof(NonPotatoPortableBattery));

        public override void OnLoad(Harmony harmony)
        {
            if (!DlcManager.IsContentSubscribed(DlcManager.DLC3_ID) || Utils.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(PotatoElectrobanksPatches));
            base.OnLoad(harmony);
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        // хранить в холодосе
        // запретить в разрядниках
        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            STORAGEFILTERS.POWER_BANKS.Remove(GameTags.ChargedPortableBattery);
            STORAGEFILTERS.POWER_BANKS.Add(NonPotatoPortableBattery);
            STORAGEFILTERS.FOOD.Add(PotatoPortableBattery);
        }

        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            foreach (var prefab in Assets.GetPrefabsWithComponent<Electrobank>())
            {
                prefab.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject go)
                {
                    if (go.TryGetComponent(out KPrefabID prefabID) && go.TryGetComponent(out Electrobank electrobank))
                    {
                        Tag categoryTag = (electrobank is PotatoElectrobank) ? PotatoPortableBattery : NonPotatoPortableBattery;
                        prefabID.AddTag(categoryTag);
                        DiscoveredResources.Instance.Discover(prefabID.PrefabTag, categoryTag);
                    }
                };
            }
        }

        // исправляем проверку полной зарядки
        [HarmonyPatch(typeof(Electrobank), nameof(Electrobank.IsFullyCharged), MethodType.Getter)]
        private static class Electrobank_IsFullyCharged
        {
            private static bool Prefix(Electrobank __instance, ref bool __result)
            {
                if (__instance is PotatoElectrobank potato)
                {
                    __result = potato.charge >= potato.maxCapacity;
                    return false;
                }
                return true;
            }
        }

        // исправляем подсчёт разряженных батареек унутри бионикла
        // нахрена тут клеи вообще массу считают ?
        [HarmonyPatch(typeof(BionicBatteryMonitor.Instance), nameof(BionicBatteryMonitor.Instance.DepletedElectrobankCount), MethodType.Getter)]
        private static class BionicBatteryMonitor_Instance_DepletedElectrobankCount
        {
            private static bool Prefix(BionicBatteryMonitor.Instance __instance, ref int __result)
            {
                int num = 0;
                foreach (var go in __instance.storage.items)
                {
                    if (go != null && go.HasTag(GameTags.EmptyPortableBattery))
                        num++;
                }
                __result = num;
                return false;
            }
        }

        // сортировка банок по проценту заряжености
        [HarmonyPatch(typeof(BionicBatteryMonitor.Instance), nameof(BionicBatteryMonitor.Instance.ReorganizeElectrobanks))]
        private static class BionicBatteryMonitor_Instance_ReorganizeElectrobanks
        {
            private static bool Prefix(BionicBatteryMonitor.Instance __instance)
            {
                __instance.storage.items.Sort(CompareTo);
                return false;
            }
            private static int CompareTo(GameObject item1, GameObject item2)
            {
                if (!item1.TryGetComponent(out Electrobank bank1))
                    return -1;
                if (!item2.TryGetComponent(out Electrobank bank2))
                    return 1;
                int percent = PercentFull(bank1).CompareTo(PercentFull(bank2));
                if (percent != 0)
                    return percent;
                else
                    return bank1.Charge.CompareTo(bank2.Charge);
            }
            private static float PercentFull(Electrobank electrobank)
            {
                if (electrobank is PotatoElectrobank potato)
                    return potato.charge / potato.maxCapacity;
                else
                    return electrobank.charge / Electrobank.capacity;
            }
        }

        // для красоты, выплёвывать то что должно быть мусором
        [HarmonyPatch(typeof(ReloadElectrobankChore), nameof(ReloadElectrobankChore.SetOverrideAnimSymbol))]
        private static class ReloadElectrobankChore_SetOverrideAnimSymbol
        {
            private static void Prefix(ref GameObject electrobank)
            {
                if (electrobank.HasTag(GameTags.EmptyPortableBattery)
                    && electrobank.TryGetComponent(out PotatoElectrobank potato) && potato.garbage.IsValid)
                {
                    electrobank = Assets.GetPrefab(potato.garbage);
                }
            }
        }

        // подавим нотификацию для гнили
        [HarmonyPatch(typeof(RotPile), nameof(RotPile.TryCreateNotification))]
        private static class RotPile_TryCreateNotification
        {
            private static bool Prefix(RotPile __instance)
            {
                return __instance.GetProperName() != global::STRINGS.ITEMS.FOOD.ROTPILE.NAME;
            }
        }
    }
}
