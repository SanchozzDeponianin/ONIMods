using Harmony;
using SanchozzONIMods.Lib;

namespace SandboxMutantPlant
{
    using static STRINGS.UI.USERMENUACTIONS;

    internal static class SandboxMutantPlantPatches
    {
        private static readonly EventSystem.IntraObjectHandler<MutantPlant> OnRefreshUserMenuDelegate = new EventSystem.IntraObjectHandler<MutantPlant>(delegate (MutantPlant component, object data)
        {
            OnRefreshUserMenu(component);
        });

        [HarmonyPatch(typeof(Localization), nameof(Localization.Initialize))]
        internal static class Localization_Initialize
        {
            private static void Postfix()
            {
                Utils.InitLocalization(typeof(STRINGS));
            }
        }

        // при включении песочницы обновляем кнопки в боковом экране
        [HarmonyPatch(typeof(Game), "set_SandboxModeActive")]
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
            if (Game.Instance.SandboxModeActive && mutant != null)
            {
                if (mutant.IsOriginal || mutant.HasTag(GameTags.Seed) || mutant.HasTag(GameTags.CropSeed))
                {
                    var binfo = new KIconButtonMenu.ButtonInfo("action_select_research", MUTATOR.NAME, new System.Action(mutant.Mutator), Action.NumActions, null, null, null, MUTATOR.TOOLTIP, true);
                    Game.Instance.userMenu.AddButton(mutant.gameObject, binfo, 1f);
                }
                if (!mutant.IsOriginal && !mutant.IsIdentified)
                {
                    var binfo = new KIconButtonMenu.ButtonInfo("action_select_research", IDENTIFY_MUTATION.NAME, new System.Action(mutant.IdentifyMutation), Action.NumActions, null, null, null, IDENTIFY_MUTATION.TOOLTIP, true);
                    Game.Instance.userMenu.AddButton(mutant.gameObject, binfo, 1f);
                }
            }
        }

        // применяем случайную мутацию и обновляем кнопки в боковом экране
        private static void Mutator(this MutantPlant mutant)
        {
            if (mutant != null)
            {
                mutant.Mutate();
                mutant.ApplyMutations();
                PlantSubSpeciesCatalog.Instance.DiscoverSubSpecies(mutant.GetSubSpeciesInfo(), mutant);
                DetailsScreen.Instance.Trigger((int)GameHashes.UIRefreshData, null);
            }
        }

        // исследуем мутацию и обновляем кнопки в боковом экране
        private static void IdentifyMutation(this MutantPlant mutant)
        {
            if (mutant != null)
            {
                PlantSubSpeciesCatalog.Instance.IdentifySubSpecies(mutant.SubSpeciesID);
                DetailsScreen.Instance.Trigger((int)GameHashes.UIRefreshData, null);
            }
        }
    }
}
