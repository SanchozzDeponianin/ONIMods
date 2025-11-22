using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.PatchManager;

namespace DualDiningTable
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            base.OnLoad(harmony);
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit(Harmony harmony)
        {
            Utils.AddBuildingToPlanScreen(BUILD_CATEGORY.Furniture, DualMinionDiningTableConfig.ID,
                BUILD_SUBCATEGORY.dining, MultiMinionDiningTableConfig.ID);
            Utils.AddBuildingToTechnology("Luxury", DualMinionDiningTableConfig.ID);

            var room_criteria = RoomConstraints.MULTI_MINION_DINING_TABLE.building_criteria?.Method;
            if (room_criteria != null)
                harmony.Patch(room_criteria, postfix: new HarmonyMethod(typeof(Patches), nameof(RoomCriteriaPatch)));
        }

        private static void RoomCriteriaPatch(KPrefabID bc, ref bool __result)
        {
            __result = __result || bc.IsPrefabID(DualMinionDiningTableConfig.ID) || bc.gameObject.name == DualMinionDiningTable.SEAT_ID;
        }

        // открытие при обыске стола в руинах
        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            Assets.GetPrefab("PropTable").GetComponent<KPrefabID>().prefabSpawnFn += AdjustLoreBearer;
        }

        private static void AdjustLoreBearer(GameObject go)
        {
            if (go.TryGetComponent(out LoreBearer bearer))
                bearer.displayContentAction += UnlockTechItem;
        }

        public static void UnlockTechItem(InfoDialogScreen _)
        {
            Db.Get().TechItems.TryGet(DualMinionDiningTableConfig.ID)?.POIUnlocked();
        }

        // заменяем гвоздями прибитый статичный seats
        [HarmonyPatch(typeof(MultiMinionDiningTable.Seat), nameof(MultiMinionDiningTable.Seat.SeatConfig), MethodType.Getter)]
        private static class Get_Seat_SeatConfig
        {
            private static bool Prefix(MultiMinionDiningTable.Seat __instance, ref MultiMinionDiningTableConfig.Seat __result)
            {
                if (__instance.gameObject.name == DualMinionDiningTable.SEAT_ID)
                {
                    __result = DualMinionDiningTableConfig.seats[__instance.index];
                    return false;
                }
                return true;
            }
        }

        // смещение анимации
        [HarmonyPatch(typeof(EatChore.StatesInstance), nameof(EatChore.StatesInstance.OnEnterMessStation))]
        private static class EatChore_StatesInstance_OnEnterMessStation
        {
            private static void Prefix(GameObject messStation, GameObject diner)
            {
                var iseat = EatChore.ResolveDiningSeat(messStation);
                if (iseat != null && iseat is MultiMinionDiningTable.Seat seat && messStation.name == DualMinionDiningTable.SEAT_ID && diner != null)
                {
                    diner.GetComponent<KBatchedAnimController>().Offset += DualMinionDiningTableConfig.AnimsOffsets[seat.index];
                }
            }
        }

        [HarmonyPatch(typeof(EatChore.StatesInstance), nameof(EatChore.StatesInstance.OnExitMessStation))]
        private static class EatChore_StatesInstance_OnExitMessStation
        {
            private static void Prefix(GameObject messStation, GameObject diner)
            {
                var iseat = EatChore.ResolveDiningSeat(messStation);
                if (iseat != null && iseat is MultiMinionDiningTable.Seat seat && messStation.name == DualMinionDiningTable.SEAT_ID && diner != null)
                {
                    diner.GetComponent<KBatchedAnimController>().Offset -= DualMinionDiningTableConfig.AnimsOffsets[seat.index];
                }
            }
        }
    }
}
