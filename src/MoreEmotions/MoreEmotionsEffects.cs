using Klei.AI;

namespace MoreEmotions
{
    using static STRINGS.DUPLICANTS.MODIFIERS;
    internal static class MoreEmotionsEffects
    {
        public static Effect StressedCheering;
        public static Effect FullBladderLaugh;
        public static Effect SawCorpse;
        public static Effect RespectGrave;

        public static void Init()
        {
            var db = Db.Get();
            // успокоение
            StressedCheering = new Effect(
                id: nameof(StressedCheering),
                name: STRESSED_CHEERING.NAME,
                description: STRESSED_CHEERING.TOOLTIP,
                duration: 0.25f * Constants.SECONDS_PER_CYCLE,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false);
            StressedCheering.Add(new AttributeModifier(
                attribute_id: db.Amounts.Stress.deltaAttribute.Id,
                value: ModifierSet.ConvertValue(-15f, Units.PerDay),
                description: STRESSED_CHEERING.NAME));
            StressedCheering.Add(new AttributeModifier(
                attribute_id: db.Attributes.QualityOfLife.Id,
                value: ModifierSet.ConvertValue(1f, Units.Flat),
                description: STRESSED_CHEERING.NAME));
            db.effects.Add(StressedCheering);

            // ржание
            FullBladderLaugh = new Effect(
                id: nameof(FullBladderLaugh),
                name: FULL_BLADDER_LAUGH.NAME,
                description: FULL_BLADDER_LAUGH.TOOLTIP,
                duration: 0.1f * Constants.SECONDS_PER_CYCLE,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: true);
            FullBladderLaugh.Add(new AttributeModifier(
                attribute_id: db.Amounts.Stress.deltaAttribute.Id,
                value: ModifierSet.ConvertValue(30f, Units.PerDay),
                description: FULL_BLADDER_LAUGH.NAME));
            db.effects.Add(FullBladderLaugh);

            // видел труп
            SawCorpse = new Effect(
                id: nameof(SawCorpse),
                name: SAW_CORPSE.NAME,
                description: SAW_CORPSE.TOOLTIP,
                duration: 0.25f * Constants.SECONDS_PER_CYCLE,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: true);
            SawCorpse.Add(new AttributeModifier(
                attribute_id: db.Amounts.Stress.deltaAttribute.Id,
                value: ModifierSet.ConvertValue(25f, Units.PerDay),
                description: SAW_CORPSE.NAME));
            db.effects.Add(SawCorpse);

            // ревел возле могилы
            RespectGrave = new Effect(
                id: nameof(RespectGrave),
                name: RESPECT_GRAVE.NAME,
                description: RESPECT_GRAVE.TOOLTIP,
                duration: 1f * Constants.SECONDS_PER_CYCLE,
                show_in_ui: false,
                trigger_floating_text: true,
                is_bad: false);
            RespectGrave.Add(new AttributeModifier(
                attribute_id: db.Attributes.QualityOfLife.Id,
                value: ModifierSet.ConvertValue(1f, Units.Flat),
                description: RESPECT_GRAVE.NAME));
            db.effects.Add(RespectGrave);
        }
    }
}
