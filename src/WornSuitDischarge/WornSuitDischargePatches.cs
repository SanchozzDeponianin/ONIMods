using HarmonyLib;
using PeterHan.PLib.Core;

namespace WornSuitDischarge
{
    internal sealed class WornSuitDischargePatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
        }

        [HarmonyPatch(typeof(SuitLocker), nameof(SuitLocker.UnequipFrom))]
        private static class SuitLocker_UnequipFrom
        {
            private static void Prefix(SuitLocker __instance, Equipment equipment)
            {
                var resume = equipment.GetTargetGameObject()?.GetComponent<MinionResume>();
                var durability = equipment.GetAssignable(Db.Get().AssignableSlots.Suit)?.GetComponent<Durability>();
                if (durability != null && durability.IsTrueWornOut(resume))
                {
                    var lockerStorage = __instance.GetComponent<Storage>();
                    var suitStorage = durability.GetComponent<Storage>();
                    var suitTank = durability.GetComponent<SuitTank>();
                    if (suitStorage != null && suitTank != null)
                    {
                        // повторяем, так как в той же маске могут быть два разных кислорода
                        while (suitStorage.Transfer(lockerStorage, suitTank.elementTag, suitTank.capacity, false, true) > 0f) ;
                    }
                    var jetSuitTank = durability.GetComponent<JetSuitTank>();
                    if (jetSuitTank)
                    {
                        lockerStorage.AddLiquid(SimHashes.Petroleum, jetSuitTank.amount, durability.GetComponent<PrimaryElement>().Temperature, byte.MaxValue, 0, false, true);
                        jetSuitTank.amount = 0f;
                    }
                }
            }
        }
    }
}
