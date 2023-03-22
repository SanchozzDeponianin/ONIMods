using Database;
using HarmonyLib;

namespace WrangleCarry
{
    internal sealed class WrangleCarryPatches : KMod.UserMod2
    {
        // сделать перенос жеготных равноприоритетным с поимкой
        // однако врядли сильно поможет в случае отлова нескольких жеготных одновременно
        // так как есть ещё учёт стоимости пути, а задача переноса инициируется точкой доставки а не самим жеготным
        [HarmonyPatch(typeof(ChoreTypes), "Add")]
        private static class ChoreTypes_Add
        {
            private static void Prefix(string id, ref bool skip_implicit_priority_change)
            {
                if (id == nameof(Db.ChoreTypes.CreatureFetch))
                    skip_implicit_priority_change = true;
            }
        }

        // обычно после поимки жеготного есть лаг до появления задачи переноса
        // изза чего жеготновод убегает делать другие дела
        // делаем жеготное доступным для переноса и роняем его на пол - сразу, в обход ИИ жеготных
        // а также добавляем жеготноводу эмоцию после поимки
        [HarmonyPatch(typeof(Capturable), "OnCompleteWork")]
        private static class Capturable_OnCompleteWork
        {
            private const string kanim = "anim_rage_kanim";
            private static readonly HashedString[] anims = { "rage_pre", "rage_loop", "rage_loop", "rage_loop", "rage_pst" };
            private static void Postfix(Capturable __instance, Worker worker)
            {
                __instance.AddTag(GameTags.Creatures.Deliverable);
                __instance.GetSMI<BaggedStates.Instance>()?.UpdateFaller(true);
                if (__instance.TryGetComponent<Pickupable>(out var pickupable) && pickupable.IsReachable())
                    return;
                if (worker != null && worker.TryGetComponent<ChoreProvider>(out var provider))
                    new EmoteChore(provider, Db.Get().ChoreTypes.EmoteHighPriority, kanim, anims);
            }
        }
    }
}
