using Klei.AI;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace SuitRecharger
{
    public class SuitRechargerWorkable : Workable
    {
        public static float сhargeTime = 1f;
        public static float warmupTime;
        private float elapsedTime;

#pragma warning disable CS0649
        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private Storage storage;

        [MyCmpReq]
        private SuitRecharger recharger;
#pragma warning restore CS0649

        private SuitTank suitTank;
        private JetSuitTank jetSuitTank;
        private LeadSuitTank leadSuitTank;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            resetProgressOnStop = true;
            showProgressBar = false;
            SetWorkTime(float.PositiveInfinity);
            workerStatusItem = Db.Get().ChoreTypes.Recharge.statusItem;
            workLayer = Grid.SceneLayer.BuildingFront;
            overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_suitrecharger_kanim") };
            synchronizeAnims = true;
        }

        protected override void OnStartWork(Worker worker)
        {
            var suit = worker.GetComponent<MinionIdentity>().GetEquipment().GetAssignable(Db.Get().AssignableSlots.Suit);
            if (suit != null)
            {
                suitTank = suit.GetComponent<SuitTank>();
                jetSuitTank = suit.GetComponent<JetSuitTank>();
                leadSuitTank = suit.GetComponent<LeadSuitTank>();
            }
            operational.SetActive(true, false);
            elapsedTime = 0;
        }

        protected override void OnStopWork(Worker worker)
        {
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
        }

        protected override void OnCompleteWork(Worker worker)
        {
            CleanAndBreakSuit(worker);
        }

        protected override bool OnWorkTick(Worker worker, float dt)
        {
            elapsedTime += dt;
            if (elapsedTime <= warmupTime) // ничего не заряжаем во время начальной анимации
                return false;
            bool oxygen_charged = ChargeSuit(dt);
            bool fuel_charged = FuelSuit(dt);
            bool battery_charged = FillBattery(dt);
            return oxygen_charged && fuel_charged && battery_charged;
        }

        public override bool InstantlyFinish(Worker worker)
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

        private void CleanAndBreakSuit(Worker worker)
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
                        var effects = worker?.GetComponent<Effects>();
                        if (effects != null && effects.HasEffect("SoiledSuit"))
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
                var durability = suitTank.GetComponent<Durability>();
                if (durability != null && durability.IsTrueWornOut(worker?.GetComponent<MinionResume>()))
                {
                    suitTank.storage.Transfer(storage, suitTank.elementTag, suitTank.capacity, false, true);
                    if (jetSuitTank != null)
                    {
                        storage.AddLiquid(SimHashes.Petroleum, jetSuitTank.amount, durability.GetComponent<PrimaryElement>().Temperature, byte.MaxValue, 0, false, true);
                        jetSuitTank.amount = 0f;
                    }
                    durability.GetComponent<Assignable>()?.Unassign();
                }
            }
        }
    }
}
