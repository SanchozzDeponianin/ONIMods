﻿using System.Reflection;
using Klei.AI;
using STRINGS;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;

namespace SuitRecharger
{
    public class SuitRechargerWorkable : Workable
    {
        private static StatusItem SuitRecharging;
        private static float сhargeTime = 1f;
        private static float warmupTime;
        private static float LeadSuitChargeWattage;
        private static float TeleportSuitChargeWattage;
        private static FieldInfo TeleportSuitBatteryCharge;
        private float elapsedTime;

#pragma warning disable CS0649
        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private Storage storage;

        [MyCmpReq]
        private SuitRecharger recharger;

        [MyCmpReq]
        private EnergyConsumer energyConsumer;
#pragma warning restore CS0649

        private SuitTank suitTank;
        private JetSuitTank jetSuitTank;
        private LeadSuitTank leadSuitTank;
        private Component teleportSuitTank;
        private Durability durability;
        private SuitRecharger.RepairSuitCost repairCost;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            resetProgressOnStop = true;
            showProgressBar = false;
            SetWorkTime(float.PositiveInfinity);
            workLayer = Grid.SceneLayer.BuildingFront;
            var kanim = Assets.GetAnim("anim_interacts_suitrecharger_kanim");
            overrideAnims = new KAnimFile[] { kanim };
            synchronizeAnims = true;
            if (SuitRecharging == null)
            {
                SuitRecharging = new StatusItem(
                    id: nameof(SuitRecharging),
                    name: DUPLICANTS.CHORES.RECHARGE.STATUS,
                    tooltip: DUPLICANTS.CHORES.RECHARGE.TOOLTIP,
                    icon: string.Empty,
                    icon_type: StatusItem.IconType.Info,
                    notification_type: NotificationType.Neutral,
                    allow_multiples: false,
                    render_overlay: OverlayModes.None.ID,
                    status_overlays: (int)StatusItem.StatusItemOverlays.None);
                // привязываемся к длительности анимации
                /*
                working_pre = 4.033333
                working_loop = 2
                working_pst = 4.333333
                */
                warmupTime = Utils.GetAnimDuration(kanim, "working_pre");
                сhargeTime = 2 * Utils.GetAnimDuration(kanim, "working_loop");
                // дополнительная мощность при зарядке свинцового костюма
                var def = Assets.GetBuildingDef(LeadSuitLockerConfig.ID);
                if (def != null && def.BuildingComplete.TryGetComponent<LeadSuitLocker>(out var locker))
                {
                    var batteryChargeTime = 60f;
                    try
                    {
                        batteryChargeTime = Traverse.Create(locker).Field<float>("batteryChargeTime").Value;
                    }
                    catch (System.Exception e)
                    {
                        PUtil.LogExcWarn(e);
                    }
                    LeadSuitChargeWattage = def.EnergyConsumptionWhenActive * batteryChargeTime / сhargeTime;
                }
                // дополнительная мощность при зарядке портального костюма из мода
                var def2 = Assets.GetBuildingDef("TeleportSuitLocker");
                if (def2 != null)
                {
                    var batteryChargeTime = 60f;
                    try
                    {
                        TeleportSuitBatteryCharge = PPatchTools.GetTypeSafe("TeleportSuitMod.TeleportSuitTank")?.GetFieldSafe("batteryCharge", false);
                        var options = Traverse.CreateWithType("TeleportSuitMod.TeleportSuitOptions").Property("Instance").GetValue();
                        batteryChargeTime = Traverse.Create(options).Property<float>("suitBatteryChargeTime").Value;
                    }
                    catch (System.Exception e)
                    {
                        PUtil.LogExcWarn(e);
                    }
                    TeleportSuitChargeWattage = def2.EnergyConsumptionWhenActive * batteryChargeTime / сhargeTime;
                }
            }
            workerStatusItem = SuitRecharging;
        }

        protected override void OnStartWork(WorkerBase worker)
        {
            SetWorkTime(float.PositiveInfinity);
            repairCost = default;
            var suit = worker.GetComponent<MinionIdentity>().GetEquipment().GetAssignable(Db.Get().AssignableSlots.Suit);
            if (suit != null)
            {
                suit.TryGetComponent(out suitTank);
                suit.TryGetComponent(out jetSuitTank);
                suit.TryGetComponent(out leadSuitTank);
                suit.TryGetComponent(out durability);
                teleportSuitTank = suit.GetComponent("TeleportSuitMod.TeleportSuitTank");

                durability.ApplyEquippedDurability(worker.GetComponent<MinionResume>());
                // если есть несколько материалов подходящих для ремонта
                // берём тот которого больше в наличии
                float max = -1f;
                if (SuitRecharger.repairSuitCost.TryGetValue(suit.PrefabID(), out var costs))
                {
                    foreach (var cost in costs)
                    {
                        recharger.repairMaterialsAvailable.TryGetValue(cost.material, out float available);
                        if (available > max)
                        {
                            repairCost = cost;
                            max = available;
                        }
                    }
                }
            }
            energyConsumer.BaseWattageRating = energyConsumer.WattsNeededWhenActive;
            operational.SetActive(true, false);
            elapsedTime = 0;
        }

        protected override void OnStopWork(WorkerBase worker)
        {
            energyConsumer.BaseWattageRating = energyConsumer.WattsNeededWhenActive;
            operational.SetActive(false, false);
            if (worker != null)
            {
                if (jetSuitTank != null && !jetSuitTank.IsEmpty())
                {
                    worker.RemoveTag(GameTags.JetSuitOutOfFuel);
                }
                if (leadSuitTank != null)
                {
                    if (!leadSuitTank.IsEmpty())
                        worker.RemoveTag(GameTags.SuitBatteryOut);
                    if (!leadSuitTank.NeedsRecharging())
                        worker.RemoveTag(GameTags.SuitBatteryLow);
                }
            }
            suitTank = null;
            jetSuitTank = null;
            leadSuitTank = null;
            teleportSuitTank = null;
            durability = null;
        }

        protected override void OnCompleteWork(WorkerBase worker)
        {
            if (worker != null)
                CleanAndBreakSuit(worker);
        }

        protected override bool OnWorkTick(WorkerBase worker, float dt)
        {
            elapsedTime += dt;
            if (elapsedTime <= warmupTime) // ничего не заряжаем во время начальной анимации
                return false;
            bool oxygen_charged = ChargeSuit(dt);
            bool fuel_charged = FuelSuit(dt);
            bool battery_charged = FillBattery(dt);
            bool teleport_charged = FillTeleportBattery(dt);
            bool repaired = RepairSuit(dt);

            var wattage = energyConsumer.WattsNeededWhenActive;
            if (!battery_charged)
                wattage += LeadSuitChargeWattage;
            if (!teleport_charged)
                wattage += TeleportSuitChargeWattage;
            if (!repaired)
                wattage += repairCost.energy / сhargeTime;
            energyConsumer.BaseWattageRating = wattage;

            return oxygen_charged && fuel_charged && battery_charged && teleport_charged;
        }

        public override bool InstantlyFinish(WorkerBase worker)
        {
            return false;
        }

        private bool ChargeSuit(float dt)
        {
            if (suitTank != null && !suitTank.IsFull())
            {
                float amount_to_refill = suitTank.capacity * dt / сhargeTime;
                var oxygen = storage.FindFirstWithMass(GameTags.Oxygen, amount_to_refill);
                if (oxygen != null)
                {
                    amount_to_refill = Mathf.Min(amount_to_refill, suitTank.capacity - suitTank.GetTankAmount());
                    amount_to_refill = Mathf.Min(amount_to_refill, oxygen.Mass);
                    if (amount_to_refill > 0f)
                    {
                        storage.Transfer(suitTank.storage, suitTank.elementTag, amount_to_refill, false, true);
                        return false;
                    }
                }
            }
            return true;
        }

        private bool FuelSuit(float dt)
        {
            if (jetSuitTank != null && !jetSuitTank.IsFull())
            {
                float amount_to_refill = JetSuitTank.FUEL_CAPACITY * dt / сhargeTime;
                var fuel = storage.FindFirstWithMass(recharger.fuelTag, amount_to_refill);
                if (fuel != null)
                {
                    amount_to_refill = Mathf.Min(amount_to_refill, JetSuitTank.FUEL_CAPACITY - jetSuitTank.amount);
                    amount_to_refill = Mathf.Min(amount_to_refill, fuel.Mass);
                    if (amount_to_refill > 0f)
                    {
                        fuel.Mass -= amount_to_refill;
                        jetSuitTank.amount += amount_to_refill;
                        return false;
                    }
                }
            }
            return true;
        }

        private bool FillBattery(float dt)
        {
            if (leadSuitTank != null && !leadSuitTank.IsFull())
            {
                leadSuitTank.batteryCharge += dt / сhargeTime;
                return false;
            }
            return true;
        }

        private bool FillTeleportBattery(float dt)
        {
            if (TeleportSuitBatteryCharge != null && teleportSuitTank != null)
            {
                float batteryCharge = (float)TeleportSuitBatteryCharge.GetValue(teleportSuitTank);
                if (batteryCharge < 1f)
                {
                    TeleportSuitBatteryCharge.SetValue(teleportSuitTank, batteryCharge + dt / сhargeTime);
                    return false;
                }
            }
            return true;
        }

        private bool RepairSuit(float dt)
        {
            if (recharger.EnableRepair && durability != null)
            {
                float d = DurabilityExtensions.durability.Get(durability);
                if (d < 1f)
                {
                    float delta = Mathf.Min(dt / сhargeTime, 1f - d);
                    if (repairCost.material.IsValid)
                    {
                        float consume_mass = repairCost.amount * delta;
                        var material = storage.FindFirstWithMass(repairCost.material, consume_mass);
                        if (material != null)
                        {
                            material.Mass -= consume_mass;
                            durability.DeltaDurabilityDifficultySettingIndependent(delta);
                            return false;
                        }
                    }
                    else
                    {
                        durability.DeltaDurabilityDifficultySettingIndependent(delta);
                        return false;
                    }
                }
            }
            return true;
        }

        private void CleanAndBreakSuit(WorkerBase worker)
        {
            if (suitTank != null)
            {
                // очистка ссанины
                if (recharger.liquidWastePipeOK)
                {
                    var list = ListPool<GameObject, SuitRecharger>.Allocate();
                    suitTank.storage.Find(GameTags.AnyWater, list);
                    if (list.Count > 0)
                    {
                        foreach (var go in list)
                            suitTank.storage.Transfer(go, storage, false, true);
                        if (worker.TryGetComponent<Effects>(out var effects) && effects.HasEffect("SoiledSuit"))
                            effects.Remove("SoiledSuit");
                    }
                    list.Recycle();
                }
                // очистка перегара
                if (recharger.gasWastePipeOK)
                {
                    var list = ListPool<GameObject, SuitRecharger>.Allocate();
                    suitTank.storage.Find(GameTags.Gas, list);
                    foreach (var go in list)
                    {
                        if (!go.HasTag(suitTank.elementTag))
                            suitTank.storage.Transfer(go, storage, false, true);
                    }
                    list.Recycle();
                }
                // проверка целостности
                // если пора ломать, то перекачиваем всё обратно и снимаем
                if (suitTank.TryGetComponent<Durability>(out var durability)
                    && durability.IsTrueWornOut(worker.GetComponent<MinionResume>()))
                {
                    suitTank.storage.Transfer(storage, suitTank.elementTag, suitTank.capacity, false, true);
                    if (jetSuitTank != null)
                    {
                        storage.AddLiquid(SimHashes.Petroleum, jetSuitTank.amount, durability.GetComponent<PrimaryElement>().Temperature, byte.MaxValue, 0, false, true);
                        jetSuitTank.amount = 0f;
                    }
                    if (durability.TryGetComponent<Assignable>(out var assignable))
                        assignable.Unassign();
                }
            }
        }
    }
}
