using HarmonyLib;
using PeterHan.PLib.Core;

namespace SanchozzONIMods.Shared
{
    // подавим нотификацию для гнили
    public static class RotPileSilentNotification
    {
        private const string PATCH_KEY = "Patch.RotPile.SilentNotification";

        public static void Patch(Harmony harmony)
        {
            if (!PRegistry.GetData<bool>(PATCH_KEY))
            {
                harmony.Patch(typeof(RotPile), nameof(RotPile.TryCreateNotification),
                    prefix: new HarmonyMethod(typeof(RotPileSilentNotification), nameof(Prefix)));
                PRegistry.PutData(PATCH_KEY, true);
            }
        }

        private static bool Prefix(RotPile __instance)
        {
            return __instance.GetProperName() != STRINGS.ITEMS.FOOD.ROTPILE.NAME;
        }
    }
}
