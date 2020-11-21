using Harmony;
using Klei.AI;
using SanchozzONIMods.Lib;

namespace CarouselCentrifuge
{
    internal static class CarouselCentrifugePatches
	{
        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        internal static class GeneratedBuildings_LoadGeneratedBuildings
		{
			private static void Prefix()
			{
				Utils.AddBuildingToPlanScreen("Furniture", CarouselCentrifugeConfig.ID, "EspressoMachine");
			}
		}

		[HarmonyPatch(typeof(Db), "Initialize")]
        internal static class Db_Initialize
		{
            private static void Prefix()
			{
				Utils.AddBuildingToTechnology("SkyDetectors", CarouselCentrifugeConfig.ID);
            }
		}

        [HarmonyPatch(typeof(ModifierSet), "LoadEffects")]
        internal static class ModifierSet_LoadEffects
        {
            private static void Postfix(ModifierSet __instance)
            {
                string text = STRINGS.DUPLICANTS.MODIFIERS.RIDEONCAROUSEL.NAME;
                string description = STRINGS.DUPLICANTS.MODIFIERS.RIDEONCAROUSEL.TOOLTIP;

                Effect specificEffect = new Effect(CarouselCentrifugeWorkable.specificEffect, text, description, Config.Get().SpecificEffectDuration * 600f, false, true, false);
                specificEffect.Add(new AttributeModifier("QualityOfLife", Config.Get().MoraleBonus, text, false, false, true));

                Effect trackingEffect = new Effect(CarouselCentrifugeWorkable.trackingEffect, "", "", Config.Get().TrackingEffectDuration * 600f, false, false, false);

                __instance.effects.Add(specificEffect);
                __instance.effects.Add(trackingEffect);
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        internal static class Localization_Initialize
        {
            private static void Postfix()
            {
                Utils.InitLocalization(typeof(STRINGS));
                LocString.CreateLocStringKeys(typeof(STRINGS.BUILDINGS));

                Config.Initialize();
            }
        }
    }
}
