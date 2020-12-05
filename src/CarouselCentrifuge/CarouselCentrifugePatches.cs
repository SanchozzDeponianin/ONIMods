using Klei.AI;

using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace CarouselCentrifuge
{
    internal static class CarouselCentrifugePatches
    {
        public static void OnLoad()
        {
            PUtil.InitLibrary();
            PUtil.RegisterPatchClass(typeof(CarouselCentrifugePatches));
            POptions.RegisterOptions(typeof(CarouselCentrifugeOptions));
        }

        [PLibMethod(RunAt.AfterModsLoad)]
        private static void InitLocalization()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void AddBuilding()
        {
            Utils.AddBuildingToPlanScreen("Furniture", CarouselCentrifugeConfig.ID, EspressoMachineConfig.ID);
            Utils.AddBuildingToTechnology("SkyDetectors", CarouselCentrifugeConfig.ID);
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddEffects()
        {
            Effect specificEffect = new Effect(
                id: CarouselCentrifugeWorkable.specificEffect,
                name: STRINGS.DUPLICANTS.MODIFIERS.RIDEONCAROUSEL.NAME,
                description: STRINGS.DUPLICANTS.MODIFIERS.RIDEONCAROUSEL.TOOLTIP,
                duration: (CarouselCentrifugeOptions.Instance.SpecificEffectDuration - 0.05f) * Constants.SECONDS_PER_CYCLE,
                show_in_ui: false,
                trigger_floating_text: true,
                is_bad: false
                );

            specificEffect.Add(new AttributeModifier(
                attribute_id: "QualityOfLife",
                value: CarouselCentrifugeOptions.Instance.MoraleBonus,
                description: STRINGS.DUPLICANTS.MODIFIERS.RIDEONCAROUSEL.NAME,
                is_multiplier: false,
                uiOnly: false,
                is_readonly: true
                ));

            Effect trackingEffect = new Effect(
                id: CarouselCentrifugeWorkable.trackingEffect,
                name: "",
                description: "",
                duration: CarouselCentrifugeOptions.Instance.TrackingEffectDuration * Constants.SECONDS_PER_CYCLE,
                show_in_ui: false,
                trigger_floating_text: false,
                is_bad: false
                );

            Db.Get().effects.Add(specificEffect);
            Db.Get().effects.Add(trackingEffect);
        }
    }
}
