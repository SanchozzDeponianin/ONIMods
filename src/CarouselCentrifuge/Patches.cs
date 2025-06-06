using Klei.AI;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace CarouselCentrifuge
{
    internal sealed class Patches : KMod.UserMod2
    {
        private static Effect specificEffect;
        private static Effect trackingEffect;
        private static AttributeModifier moraleModifier;

        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuildingAndEffects()
        {
            Utils.AddBuildingToPlanScreen(BUILD_CATEGORY.Furniture, CarouselCentrifugeConfig.ID, BUILD_SUBCATEGORY.recreation, EspressoMachineConfig.ID);
            string requiredTech = DlcManager.IsExpansion1Active() ? "SpaceProgram" : "ArtificialFriends";
            Utils.AddBuildingToTechnology(requiredTech, CarouselCentrifugeConfig.ID);

            moraleModifier = new AttributeModifier(
                attribute_id: "QualityOfLife",
                value: ModOptions.Instance.MoraleBonus,
                description: STRINGS.DUPLICANTS.MODIFIERS.RIDEONCAROUSEL.NAME,
                is_multiplier: false,
                uiOnly: false,
                is_readonly: false
                );

            specificEffect = new Effect(
                id: CarouselCentrifugeWorkable.specificEffectName,
                name: STRINGS.DUPLICANTS.MODIFIERS.RIDEONCAROUSEL.NAME,
                description: STRINGS.DUPLICANTS.MODIFIERS.RIDEONCAROUSEL.TOOLTIP,
                duration: (ModOptions.Instance.SpecificEffectDuration - 0.05f) * Constants.SECONDS_PER_CYCLE,
                show_in_ui: false,
                trigger_floating_text: true,
                is_bad: false
                );
            specificEffect.Add(moraleModifier);

            trackingEffect = new Effect(
                id: CarouselCentrifugeWorkable.trackingEffectName,
                name: "",
                description: "",
                duration: ModOptions.Instance.TrackingEffectDuration * Constants.SECONDS_PER_CYCLE,
                show_in_ui: false,
                trigger_floating_text: false,
                is_bad: false
                );

            Db.Get().effects.Add(specificEffect);
            Db.Get().effects.Add(trackingEffect);
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            ModOptions.Reload();
            moraleModifier.SetValue(ModOptions.Instance.MoraleBonus);
            specificEffect.duration = (ModOptions.Instance.SpecificEffectDuration - 0.05f) * Constants.SECONDS_PER_CYCLE;
            trackingEffect.duration = ModOptions.Instance.TrackingEffectDuration * Constants.SECONDS_PER_CYCLE;
        }
    }
}
