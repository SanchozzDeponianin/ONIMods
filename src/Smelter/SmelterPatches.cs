using Harmony;

using SanchozzONIMods.Lib;
using PeterHan.PLib;

namespace Smelter
{
    internal static class SmelterPatches
    {
        public static void OnLoad()
        {
            PUtil.InitLibrary();
            PUtil.RegisterPatchClass(typeof(SmelterPatches));
            // todo: обдумать нужность опций
            //POptions.RegisterOptions(typeof(SquirrelGeneratorOptions));
        }

        [PLibMethod(RunAt.AfterModsLoad)]
        private static void InitLocalization()
        {
            // todo: сделать строки
            //Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void AddBuilding()
        {
            Utils.AddBuildingToPlanScreen("Refining", SmelterConfig.ID, KilnConfig.ID);
            Utils.AddBuildingToTechnology("BasicRefinement", SmelterConfig.ID);
        }

        // добавляем рецепты
        [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
        internal static class GeneratedBuildings_LoadGeneratedBuildings
        {
            private static void Postfix()
            {
                SmelterConfig.ConfigureRecipes();
            }
        }
    }
}
