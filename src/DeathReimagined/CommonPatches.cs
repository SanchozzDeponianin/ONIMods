using Harmony;
using STRINGS;
using SanchozzONIMods.Lib;

namespace DeathReimagined
{
    internal static class CommonPatches
    {
        /*
        static class Mod_OnLoad
        {
            public static void OnLoad()
            {
            }
        }
        */

        [HarmonyPatch(typeof(Localization), "Initialize")]
        internal static class Localization_Initialize
        {
            private static void Postfix()
            {
                Debug.Log("Localization_Initialize");

                Utils.InitLocalization(typeof(STRINGS));
                // чтобы подтянуть "раздробить в известь" из локализации
                Utils.ReplaceLocString(ref STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.SKELETON.DESC, "{crushed}", ITEMS.INDUSTRIAL_PRODUCTS.CRAB_SHELL.DESC);

                LocString.CreateLocStringKeys(typeof(STRINGS.DUPLICANTS));
                //Config.Initialize();
            }
        }

        // добавляем мониторы меланхолии, незакопанных трупов, инфаркта
        // TODO: и другие
        // добавляем источник болезни
        [HarmonyPatch(typeof(RationalAi), "InitializeStates")]
        internal static class RationalAi_InitializeStates
        {
            private static void Postfix(RationalAi __instance)
            {
                __instance.alive
                    .ToggleStateMachine(smi => new MelancholyMonitor.Instance(smi.master, new MelancholyMonitor.Def()))
                    .ToggleStateMachine(smi => new UnburiedCorpseMonitor.Instance(smi.master))
                    .ToggleStateMachine(smi => new HeartAttackMonitor.Instance(smi.master));
                /*
                __instance.dead
                    ;
                */
            }
        }

    }
}
