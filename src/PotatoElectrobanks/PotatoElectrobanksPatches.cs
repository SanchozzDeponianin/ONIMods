using Klei.AI;
using TUNING;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.PatchManager;

namespace PotatoElectrobanks
{
    using BionicModifierParameter = StateMachinesExtensions.NonSerializedObjectParameter
        <BionicBatteryMonitor, BionicBatteryMonitor.Instance, IStateMachineTarget, BionicBatteryMonitor.Def, AttributeModifier>;

    using FlydoModifierParameter = StateMachinesExtensions.NonSerializedObjectParameter
        <RobotElectroBankMonitor, RobotElectroBankMonitor.Instance, IStateMachineTarget, RobotElectroBankMonitor.Def, AttributeModifier>;

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

        // также в основном для красоты
        // отображаемая максимальная величина заряда биониклов и флудов
        // гвоздями прибита к величине кратной 120 кДж
        // внесем коррективы в зависимости от установленной батарейки
        // чтобы не отображалось как "380 кДж / 120 кДж"
        private static BionicModifierParameter bionic_battery_max_capacity_fix;

        [HarmonyPatch(typeof(BionicBatteryMonitor), nameof(BionicBatteryMonitor.InitializeStates))]
        private static class BionicBatteryMonitor_InitializeStates
        {
            private static void Postfix(BionicBatteryMonitor __instance)
            {
                bionic_battery_max_capacity_fix = __instance.AddParameter(nameof(bionic_battery_max_capacity_fix),
                    new BionicModifierParameter());
            }
        }

        [HarmonyPatch(typeof(BionicBatteryMonitor.Instance), nameof(BionicBatteryMonitor.Instance.UpdateCapacityAmount))]
        private static class BionicBatteryMonitor_Instance_UpdateCapacityAmount
        {
            private static void Postfix(BionicBatteryMonitor.Instance __instance)
            {
                // оригинальный код стирает все модификаторы, так что просто создадим новый
                var modifier = new AttributeModifier(Db.Get().Amounts.BionicInternalBattery.maxAttribute.Id, 0f, "fix", false, false, false);
                bionic_battery_max_capacity_fix.Set(modifier, __instance);
                __instance.BionicBattery.maxAttribute.Add(modifier);
            }
        }

        [HarmonyPatch(typeof(BionicBatteryMonitor.Instance), nameof(BionicBatteryMonitor.Instance.RefreshCharge))]
        private static class BionicBatteryMonitor_Instance_RefreshCharge
        {
            private static void Prefix(BionicBatteryMonitor.Instance __instance)
            {
                var modifier = bionic_battery_max_capacity_fix.Get(__instance);
                if (modifier != null)
                {
                    float delta = 0f;
                    for (int i = 0; i < __instance.storage.Count; i++)
                    {
                        var go = __instance.storage.items[i];
                        if (go != null && go.TryGetComponent(out PotatoElectrobank potato))
                            delta += potato.maxCapacity - Electrobank.capacity;
                    }
                    modifier.SetValue(delta);
                }
            }
        }

        // тожесамое для флудо
        private static FlydoModifierParameter robot_battery_max_capacity_fix;

        [HarmonyPatch(typeof(RobotElectroBankMonitor), nameof(RobotElectroBankMonitor.InitializeStates))]
        private static class RobotElectroBankMonitor_InitializeStates
        {
            private static void Postfix(RobotElectroBankMonitor __instance)
            {
                robot_battery_max_capacity_fix = __instance.AddParameter(nameof(robot_battery_max_capacity_fix),
                    new FlydoModifierParameter());
            }
        }

        [HarmonyPatch(typeof(RobotElectroBankMonitor.Instance), nameof(RobotElectroBankMonitor.Instance.ElectroBankStorageChange))]
        private static class RobotElectroBankMonitor_Instance_ElectroBankStorageChange
        {
            private static void Prefix(RobotElectroBankMonitor.Instance __instance, ref Electrobank __state)
            {
                __state = __instance.electrobank;
                if (robot_battery_max_capacity_fix.Get(__instance) == null)
                {
                    var modifier = new AttributeModifier(Db.Get().Amounts.InternalElectroBank.maxAttribute.Id, 0f, "fix", false, false, false);
                    robot_battery_max_capacity_fix.Set(modifier, __instance);
                    __instance.bankAmount.maxAttribute.Add(modifier);
                }
            }
            private static void Postfix(RobotElectroBankMonitor.Instance __instance, Electrobank __state)
            {
                if (__state != __instance.electrobank)
                {
                    var modifier = robot_battery_max_capacity_fix.Get(__instance);
                    if (modifier != null)
                    {
                        if (__instance.electrobank != null && __instance.electrobank is PotatoElectrobank potato)
                            modifier.SetValue(potato.maxCapacity - Electrobank.capacity);
                        else
                            modifier.SetValue(0f);
                    }
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
