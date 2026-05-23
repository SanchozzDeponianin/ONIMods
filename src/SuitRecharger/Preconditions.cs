using SanchozzONIMods.Lib;
using static STRINGS.DUPLICANTS.CHORES;

namespace SuitRecharger
{
    using static STRINGS.DUPLICANTS.CHORES.PRECONDITIONS;

    public class Preconditions
    {
        private static Preconditions instance;

        public static Preconditions Instance
        {
            get
            {
                if (instance == null)
                    instance = new Preconditions();
                return instance;
            }
        }

        public static void DestroyInstance() => instance = null;

        // проверка что костюм действительно надет
        public Chore.Precondition IsSuitEquipped = new()
        {
            id = nameof(IsSuitEquipped),
            description = IS_SUIT_EQUIPPED,
            sortOrder = -1,
            canExecuteOnAnyThread = true,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                if (context.consumerState.prefabid.HasTag(GameTags.HasSuitTank)
                    && context.consumerState.equipment != null)
                {
                    var equippable = context.consumerState.equipment.GetSlot(Db.Get().AssignableSlots.Suit)?.assignable as Equippable;
                    context.data = equippable;
                    return equippable != null;
                }
                return false;
            }
        };

        // проверка что заправка не "уже выполняется"
        public Chore.Precondition NotCurrentlyRecharging = new()
        {
            id = nameof(NotCurrentlyRecharging),
            description = CURRENTLY_RECHARGING,
            sortOrder = 0,
            canExecuteOnAnyThread = true,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                var currentChore = context.consumerState.choreDriver.GetCurrentChore();
                if (currentChore != null)
                {
                    var id = currentChore.choreType.IdHash;
                    return id != SuitRecharger.RecoverBreathRecharge.IdHash && id != Db.Get().ChoreTypes.Recharge.IdHash;
                }
                return true;
            }
        };

        // проверка что костюму требуется заправка.
        public Chore.Precondition DoesSuitNeedRecharging = new()
        {
            id = nameof(DoesSuitNeedRecharging),
            description = PRECONDITIONS.DOES_SUIT_NEED_RECHARGING_URGENT,
            sortOrder = 1,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                if (data is SuitRecharger recharger && recharger != null
                    && context.data is Equippable suit && suit != null)
                {
                    bool bionic = context.consumerState.prefabid.PrefabTag == GameTags.Minions.Models.Bionic;
                    if ((!bionic || recharger.fillBionicSuitTank)
                        && suit.TryGetComponent(out SuitTank suit_tank) && suit_tank.NeedsRecharging())
                    {
                        context.data = suit_tank;
                        return true;
                    }
                    if (suit.TryGetComponent(out JetSuitTank jet_suit_tank) && jet_suit_tank.NeedsRecharging())
                    {
                        context.data = jet_suit_tank;
                        return true;
                    }
                    if (suit.TryGetComponent(out LeadSuitTank lead_suit_tank) && lead_suit_tank.NeedsRecharging())
                    {
                        context.data = lead_suit_tank;
                        return true;
                    }
                    if (TeleportSuitCompat.NeedsRecharging != null && suit.TryGetComponent(TeleportSuitCompat.TankType, out var teleport_suit_tank)
                        && teleport_suit_tank != null && TeleportSuitCompat.NeedsRecharging(teleport_suit_tank))
                    {
                        context.data = teleport_suit_tank;
                        return true;
                    }
                    if (bionic && recharger.fillBionicInternalTank)
                    {
                        var internal_tank = context.consumerState.prefabid.GetSMI<BionicOxygenTankMonitor.Instance>();
                        if (internal_tank.OxygenPercentage < SuitTank.REFILL_PERCENT)
                        {
                            context.data = internal_tank;
                            return true;
                        }
                    }
                }
                return false;
            }
        };

        // проверка что костюму требуется срочная заправка 
        public Chore.Precondition DoesSuitNeedRechargingUrgent = new()
        {
            id = nameof(DoesSuitNeedRechargingUrgent),
            description = PRECONDITIONS.HAS_URGE,
            sortOrder = 1,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                if (data is SuitRecharger recharger && recharger != null
                    && context.data is Equippable suit && suit != null)
                {
                    bool bionic = context.consumerState.prefabid.PrefabTag == GameTags.Minions.Models.Bionic;
                    if ((!bionic || recharger.fillBionicSuitTank)
                        && suit.TryGetComponent(out SuitTank suit_tank) && suit_tank.IsEmpty())
                    {
                        context.data = suit_tank;
                        return true;
                    }
                }
                return false;
            }
        };

        // проверка что кислорода достаточно для полной заправки
        public Chore.Precondition IsEnoughOxygen = new()
        {
            id = nameof(IsEnoughOxygen),
            description = IS_ENOUGH_OXYGEN,
            sortOrder = 2,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                if (data is SuitRecharger recharger && recharger != null)
                {
                    if (recharger.OxygenAvailable >= ModOptions.Instance.o2_capacity)
                        return true;
                    if (context.data is SuitTank suit_tank && suit_tank != null
                        && recharger.OxygenAvailable < (suit_tank.capacity - suit_tank.GetTankAmount()))
                    {
                        return false;
                    }
                    else if (context.data is BionicOxygenTankMonitor.Instance internal_tank && internal_tank != null
                        && recharger.OxygenAvailable < internal_tank.SpaceAvailableInTank)
                    {
                        return false;
                    }
                }
                return true;
            }
        };

        // проверка что топлива достаточно для полной заправки
        public Chore.Precondition IsEnoughFuel = new()
        {
            id = nameof(IsEnoughFuel),
            description = IS_ENOUGH_FUEL,
            sortOrder = 3,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                if (data is SuitRecharger recharger && recharger != null)
                {
                    if (recharger.FuelAvailable >= ModOptions.Instance.fuel_capacity)
                        return true;
                    if (context.data is JetSuitTank jet_suit_tank && jet_suit_tank != null
                       && recharger.FuelAvailable < (JetSuitTank.FUEL_CAPACITY - jet_suit_tank.amount))
                    {
                        return false;
                    }
                }
                return true;
            }
        };

        // проверка возможности ремонта и что костюм имеет достаточную прочность чтобы не сломаться в процессе зарядки
        public Chore.Precondition IsSuitHasEnoughDurability = new()
        {
            id = nameof(IsSuitHasEnoughDurability),
            description = IS_SUIT_HAS_ENOUGH_DURABILITY,
            sortOrder = 5,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                if (data is SuitRecharger recharger && recharger != null
                    && context.consumerState.equipment.GetSlot(Db.Get().AssignableSlots.Suit).assignable is Equippable equippable
                    && equippable.TryGetComponent(out Durability durability))
                {
                    float d = durability.GetTrueDurability(context.consumerState.resume);
                    if (d >= recharger.DurabilityThreshold)
                        return true;
                    if (recharger.EnableRepair && SuitRecharger.AllRepairSuitCost.TryGetValue(equippable.def.Id.ToTag(), out var costs))
                    {
                        foreach (var cost in costs)
                        {
                            float need = cost.amount * (1f - d);
                            recharger.RepairMaterialsAvailable.TryGetValue(cost.material, out float available);
                            if (need <= available)
                                return true;
                        }
                    }
                    return false;
                }
                return true;
            }
        };
    }
}
