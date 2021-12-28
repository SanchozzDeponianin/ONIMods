using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SanchozzONIMods.Lib;
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

        private static bool ShouldTransfer(Assignable assignable, Equipment equipment)
        {
            var resume = equipment?.GetTargetGameObject()?.GetComponent<MinionResume>();
            var durability = assignable?.GetComponent<Durability>();
            return durability != null && durability.IsTrueWornOut(resume);
        }

        private static void Transfer(Assignable assignable, Storage lockerStorage)
        {
            if (assignable != null && lockerStorage != null)
            {
                var suitStorage = assignable.GetComponent<Storage>();
                var suitTank = assignable.GetComponent<SuitTank>();
                if (suitStorage != null && suitTank != null)
                {
                    suitStorage.Transfer(lockerStorage, suitTank.elementTag, suitTank.capacity, false, true);
                }
                // todo: проверка что тип локера подходит
                var jetSuitTank = assignable.GetComponent<JetSuitTank>();
                if (jetSuitTank != null && lockerStorage.HasTag(JetSuitLockerConfig.ID))
                {
                    lockerStorage.AddLiquid(SimHashes.Petroleum, jetSuitTank.amount, assignable.GetComponent<PrimaryElement>().Temperature, byte.MaxValue, 0, false, true);
                    jetSuitTank.amount = 0f;
                }
            }
        }

        // штатная разэкипировка костюма 
        [HarmonyPatch(typeof(SuitLocker), nameof(SuitLocker.UnequipFrom))]
        private static class SuitLocker_UnequipFrom
        {
            private static void Prefix(SuitLocker __instance, Equipment equipment)
            {
                var assignable = equipment?.GetAssignable(Db.Get().AssignableSlots.Suit);
                var storage = __instance?.GetComponent<Storage>();
                if (ShouldTransfer(assignable, equipment))
                    Transfer(assignable, storage);
            }
        }

        // снятие костюма задачей "вернуть костюм" если док занят
        // не удалось протестировать, возможно мёртвая ветка кода игры.
        [HarmonyPatch(typeof(SuitLocker.ReturnSuitWorkable), "OnCompleteWork")]
        private static class SuitLocker_ReturnSuitWorkable_OnCompleteWork
        {
            private static void TryTransfer(Assignable assignable, SuitLocker.ReturnSuitWorkable workable, Equipment equipment)
            {
                var storage = workable?.GetComponent<Storage>();
                if (ShouldTransfer(assignable, equipment))
                    Transfer(assignable, storage);
            }
            /*
                else
		        {
			---     equipment.GetAssignable(Db.Get().AssignableSlots.Suit).Unassign();
            +++     var assignable = equipment.GetAssignable(Db.Get().AssignableSlots.Suit);
            +++     TryTransfer(assignable, this, equipment);
            +++     assignable.Unassign();
		        }
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var unassign = typeof(Assignable).GetMethodSafe(nameof(Assignable.Unassign), false, PPatchTools.AnyArguments);
                var trytransfer = typeof(SuitLocker_ReturnSuitWorkable_OnCompleteWork).GetMethodSafe(nameof(TryTransfer), true, PPatchTools.AnyArguments);

                bool result = false;
                if (unassign != null && trytransfer != null)
                {
                    for (int i = 0; i < instructionsList.Count(); i++)
                    {
                        var instruction = instructionsList[i];
                        if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info) && info == unassign)
                        {
                            instructionsList.Insert(i++, new CodeInstruction(OpCodes.Dup));     // assignable
                            instructionsList.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0)); // workable
                            instructionsList.Insert(i++, new CodeInstruction(OpCodes.Ldloc_0)); // equipment
                            instructionsList.Insert(i++, new CodeInstruction(OpCodes.Call, trytransfer));
                            result = true;
#if DEBUG
                            PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                            break;
                        }
                    }
                }
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }
        }

        // снятие костюма при прохождении мимо маркера, если нет подходящих доков
        // ищем док с наименьшей массой
        [HarmonyPatch]
        private static class SuitMarker_SuitMarkerReactable_Run
        {
            private static void TryTransfer(Assignable assignable, SuitMarker marker, Equipment equipment)
            {
                if (ShouldTransfer(assignable, equipment))
                {
                    Storage storage = null;
                    float mass = float.PositiveInfinity;
                    var pooledList = ListPool<SuitLocker, SuitMarker>.Allocate();
                    marker?.GetAttachedLockers(pooledList);
                    foreach (var locker in pooledList)
                    {
                        var s = locker.GetComponent<Storage>();
                        var m = s.MassStored();
                        if (m < mass)
                        {
                            mass = m;
                            storage = s;
                        }
                    }
                    pooledList.Recycle();
                    Transfer(assignable, storage);
                }
            }

            private static MethodBase TargetMethod()
            {
                return typeof(SuitMarker).GetNestedType("SuitMarkerReactable", PPatchTools.BASE_FLAGS)
                    .GetMethodSafe("Run", false, PPatchTools.AnyArguments);
            }
            /*
                    assignable = equipment.GetAssignable(Db.Get().AssignableSlots.Suit);
            +++     TryTransfer(assignable, this.suitMarker, equipment);
		            assignable.Unassign();
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                var instructionsList = instructions.ToList();
                string methodName = method.DeclaringType.FullName + "." + method.Name;

                var unassign = typeof(Assignable).GetMethodSafe(nameof(Assignable.Unassign), false, PPatchTools.AnyArguments);
                var trytransfer = typeof(SuitMarker_SuitMarkerReactable_Run).GetMethodSafe(nameof(TryTransfer), true, PPatchTools.AnyArguments);
                var suitMarker = typeof(SuitMarker).GetNestedType("SuitMarkerReactable", PPatchTools.BASE_FLAGS).GetFieldSafe("suitMarker", false);

                bool result = false;
                if (unassign != null && trytransfer != null && suitMarker != null)
                {
                    for (int i = 0; i < instructionsList.Count(); i++)
                    {
                        var instruction = instructionsList[i];
                        if (((instruction.opcode == OpCodes.Call) || (instruction.opcode == OpCodes.Callvirt)) && (instruction.operand is MethodInfo info) && info == unassign)
                        {
                            instructionsList.Insert(i++, new CodeInstruction(OpCodes.Dup));     // assignable
                            instructionsList.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
                            instructionsList.Insert(i++, new CodeInstruction(OpCodes.Ldfld, suitMarker));
                            instructionsList.Insert(i++, new CodeInstruction(OpCodes.Ldloc_0)); // equipment
                            instructionsList.Insert(i++, new CodeInstruction(OpCodes.Call, trytransfer));
                            result = true;
#if DEBUG
                            PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                            break;
                        }
                    }
                }
                if (!result)
                {
                    PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
                }
                return instructionsList;
            }
        }
    }
}
