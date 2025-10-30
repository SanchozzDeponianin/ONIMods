using HarmonyLib;
using SanchozzONIMods.Lib;

namespace SandboxMutantPlant
{
    using static STRINGS.UI.USERMENUACTIONS;

    internal sealed class Patches : KMod.UserMod2
    {
        private static readonly EventSystem.IntraObjectHandler<MutantPlant> OnRefreshUserMenuDelegate =
            new((component, data) => OnRefreshUserMenu(component));

        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            Utils.RegisterLocalization(typeof(STRINGS));
            base.OnLoad(harmony);
        }

        // при включении песочницы обновляем кнопки в боковом экране
        [HarmonyPatch(typeof(Game), nameof(Game.SandboxModeActive), MethodType.Setter)]
        public static class Game_set_SandboxModeActive
        {
            public static void Postfix()
            {
                if (DetailsScreen.Instance != null && DetailsScreen.Instance.target != null)
                {
                    Game.Instance.userMenu.Refresh(DetailsScreen.Instance.target);
                    DetailsScreen.Instance.Trigger((int)GameHashes.UIRefreshData, null);
                }
            }
        }

        [HarmonyPatch(typeof(MutantPlant), "OnSpawn")]
        public static class MutantPlant_OnSpawn
        {
            public static void Postfix(MutantPlant __instance)
            {
                __instance.Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
            }
        }

        [HarmonyPatch(typeof(MutantPlant), "OnCleanUp")]
        public static class MutantPlant_OnCleanUp
        {
            public static void Prefix(MutantPlant __instance)
            {
                __instance.Unsubscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
            }
        }

        // добавляем кнопки
        // мутатор - для оригинального раснения или любых семян
        // исследование - для неизвестных мутаций
        private static void OnRefreshUserMenu(MutantPlant mutant)
        {
            if (Game.Instance.SandboxModeActive && mutant != null && mutant.TryGetComponent(out KPrefabID prefabID))
            {
                if ((mutant.IsOriginal && !prefabID.HasTag(GameTags.PlantBranch))
                    || prefabID.HasTag(GameTags.Seed) || prefabID.HasTag(GameTags.CropSeed))
                {
                    var binfo = new KIconButtonMenu.ButtonInfo("action_select_research", MUTATOR.NAME, mutant.Mutator, Utils.MaxAction, null, null, null, MUTATOR.TOOLTIP, true);
                    Game.Instance.userMenu.AddButton(mutant.gameObject, binfo, 1f);
                }
                if (!mutant.IsOriginal && !mutant.IsIdentified)
                {
                    var binfo = new KIconButtonMenu.ButtonInfo("action_select_research", IDENTIFY_MUTATION.NAME, mutant.IdentifyMutation, Utils.MaxAction, null, null, null, IDENTIFY_MUTATION.TOOLTIP, true);
                    Game.Instance.userMenu.AddButton(mutant.gameObject, binfo, 1f);
                }
            }
        }
    }
}
