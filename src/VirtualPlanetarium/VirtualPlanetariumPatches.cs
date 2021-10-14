using Klei.AI;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace VirtualPlanetarium
{
    internal sealed class VirtualPlanetariumPatches : KMod.UserMod2
    {
        private static Effect usingEffect;
        private static Effect specificEffect;
        private static Effect trackingEffect;
        private static AttributeModifier stressModifier;
        private static AttributeModifier moraleModifier;

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(VirtualPlanetariumPatches));
            new POptions().RegisterOptions(this, typeof(VirtualPlanetariumOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuildingAndEffects()
        {
            Utils.AddBuildingToPlanScreen("Furniture", VirtualPlanetariumConfig.ID, EspressoMachineConfig.ID);
            Utils.AddBuildingToTechnology("SpaceProgram", VirtualPlanetariumConfig.ID);

            stressModifier = new AttributeModifier(
                attribute_id: "StressDelta",
                value: ModifierSet.ConvertValue(-VirtualPlanetariumOptions.Instance.StressDelta, Units.PerDay),
                description: STRINGS.DUPLICANTS.MODIFIERS.STARGAZING.NAME,
                is_multiplier: false,
                uiOnly: false,
                is_readonly: false
                );

            usingEffect = new Effect(
                id: VirtualPlanetariumWorkable.USING_EFFECT,
                name: STRINGS.DUPLICANTS.MODIFIERS.STARGAZING.NAME,
                description: STRINGS.DUPLICANTS.MODIFIERS.STARGAZING.TOOLTIP,
                duration: 0,
                show_in_ui: true,
                trigger_floating_text: false,
                is_bad: false
                );
            usingEffect.Add(stressModifier);

            moraleModifier = new AttributeModifier(
                attribute_id: "QualityOfLife",
                value: VirtualPlanetariumOptions.Instance.MoraleBonus,
                description: STRINGS.DUPLICANTS.MODIFIERS.STARGAZED.NAME,
                is_multiplier: false,
                uiOnly: false,
                is_readonly: false
                );

            specificEffect = new Effect(
                id: VirtualPlanetariumWorkable.SPECIFIC_EFFECT,
                name: STRINGS.DUPLICANTS.MODIFIERS.STARGAZED.NAME,
                description: STRINGS.DUPLICANTS.MODIFIERS.STARGAZED.TOOLTIP,
                duration: (VirtualPlanetariumOptions.Instance.SpecificEffectDuration - 0.05f) * Constants.SECONDS_PER_CYCLE,
                show_in_ui: false,
                trigger_floating_text: true,
                is_bad: false
                );
            specificEffect.Add(moraleModifier);

            trackingEffect = new Effect(
                id: VirtualPlanetariumWorkable.TRACKING_EFFECT,
                name: "",
                description: "",
                duration: VirtualPlanetariumOptions.Instance.TrackingEffectDuration * Constants.SECONDS_PER_CYCLE,
                show_in_ui: false,
                trigger_floating_text: false,
                is_bad: false
                );

            var effects = Db.Get().effects;
            effects.Add(usingEffect);
            effects.Add(specificEffect);
            effects.Add(trackingEffect);
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            VirtualPlanetariumOptions.Reload();
            stressModifier.SetValue(ModifierSet.ConvertValue(-VirtualPlanetariumOptions.Instance.StressDelta, Units.PerDay));
            moraleModifier.SetValue(VirtualPlanetariumOptions.Instance.MoraleBonus);
            specificEffect.duration = (VirtualPlanetariumOptions.Instance.SpecificEffectDuration - 0.05f) * Constants.SECONDS_PER_CYCLE;
            trackingEffect.duration = VirtualPlanetariumOptions.Instance.TrackingEffectDuration * Constants.SECONDS_PER_CYCLE;
        }
    }
}
