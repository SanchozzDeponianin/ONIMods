using Klei.AI;
using SanchozzONIMods.Lib;

namespace MoreEmotions
{
    using static MoreEmotionsEffects;

    public class STRINGS
    {
        public class DUPLICANTS
        {
            public class MODIFIERS
            {
                public class STRESSED_CHEERING
                {
                    public static LocString NAME = "Psychological Assistance";
                    public static LocString TOOLTIP = "Another Duplicant cheering up this Duplicant and provided psychological assistance";
                }
                public class FULL_BLADDER_LAUGH
                {
                    public static LocString NAME = "Disgraced itself";
                    public static LocString TOOLTIP = "This Duplicant made a mess in front of everyone's eyes\nIt's so embarrassing";
                }
                public class SAW_CORPSE
                {
                    public static LocString NAME = "Saw Corpse";
                    public static LocString TOOLTIP = "This Duplicant recently saw the corpse of another Duplicant\nIt was overcome by a feeling of anxiety and fear";
                }
                public class RESPECT_GRAVE
                {
                    public static LocString NAME = "Respect";
                    public static LocString TOOLTIP = "We honor our dead";
                }
                public class CONTUSION
                {
                    public static LocString NAME = "Concussion";
                    public static LocString TOOLTIP = "This Duplicant recently fell from a height and almost crashed";
                }
            }
        }

        public class OPTIONS
        {
            public class WAKE_UP_LAZY_ASS
            {
                public static LocString NAME = "\"Wake Up Lazy Ass!\"";
                public static LocString TOOLTIP = "Duplicants will kick a narcoleptic sleeping on the floor";
            }
            public class STRESS_CHEERING
            {
                public static LocString NAME = "Psychological Assistance";
                public static LocString TOOLTIP = "Duplicants will provide psychological assistance to stressed Duplicants";
            }
            public class STRESS_CHEERING_EFFECT
            {
                public static LocString NAME = "Adds buff";
                public static LocString TOOLTIP = "";
            }
            public class DOUBLE_GREETING
            {
                public static LocString NAME = "Two \"Dual\" greetings with fists and hands";
            }
            public class MOONWALK_GREETING
            {
                public static LocString NAME = "\"Dancing\" greeting";
            }
            public class FULL_BLADDER_EMOTE
            {
                public static LocString NAME = "\"I'm going to burst!\"";
                public static LocString TOOLTIP = "The emotion when a Duplicant very much wants to go to the Toilet";
            }
            public class FULL_BLADDER_LAUGH
            {
                public static LocString NAME = "\"Haha, it's disgraced itself!\"";
                public static LocString TOOLTIP = "Duplicants laugh at the Duplicant who made a mess";
            }
            public class FULL_BLADDER_ADD_EFFECT
            {
                public static LocString NAME = "Enable debuff";
                public static LocString TOOLTIP = "";
            }
            public class CONTAMINATED_FOOD_EMOTE
            {
                public static LocString NAME = "Yuck! Microbes!";
                public static LocString TOOLTIP = "The emotion when a Duplicant eating contaminated Food";
            }
            public class STARVATION_EMOTE
            {
                public static LocString NAME = "Very hungry emotion";
                public static LocString TOOLTIP = "A starving Duplicant will sometimes try to bite his hand";
            }
            public class ALTERNATIVE_BINGE_EAT_EMOTE
            {
                public static LocString NAME = "Alternative variant of Binge Eater emotion";
                public static LocString TOOLTIP = "A stressed Binge Eater will sometimes try to bite his hand";
            }
            public class ALTERNATIVE_SLEEP_ANIMS
            {
                public static LocString NAME = "Alternative variants of sleeping on a Bed animation";
            }
            public class ALTERNATIVE_NARCOLEPTIC_ANIMS
            {
                public static LocString NAME = "Alternative variant of narcoleptic sleeping animation";
            }
            public class SAW_CORPSE_EMOTE
            {
                public static LocString NAME = "The \"Saw Corpse\" Emotion";
            }
            public class SAW_CORPSE_ADD_EFFECT
            {
                public static LocString NAME = "Enable debuff";
                public static LocString TOOLTIP = "";
            }
            public class RESPECT_GRAVE_EMOTE
            {
                public static LocString NAME = "The \"F to pay respects\" Emotion";
                public static LocString TOOLTIP = "Duplicants occasionally mourn near the Grave";
            }
            public class RESPECT_GRAVE_ADD_EFFECT
            {
                public static LocString NAME = "Enable buff";
                public static LocString TOOLTIP = "";
            }
            public class WET_HANDS_EMOTE
            {
                public static LocString NAME = $"The \"Wet Hands\" Emotion";
                public static LocString TOOLTIP = "Sometimes it happens after mopping or using the Washbasin";
            }
            public class FALL_CONTUSION_EMOTE
            {
                public static LocString NAME = "Animation after falling from a height to the floor";
            }
            public class FALL_CONTUSION_ADD_EFFECT
            {
                public static LocString NAME = "Enable debuff";
                public static LocString TOOLTIP = $"If the height was more than {CONTUSION_HEIGHT}\n";
            }
        }

        internal static void PostProcess()
        {
            OPTIONS.STRESS_CHEERING_EFFECT.TOOLTIP.ReplaceText(Effect.CreateFullTooltip(StressedCheering, true));
            OPTIONS.FULL_BLADDER_ADD_EFFECT.TOOLTIP.ReplaceText(Effect.CreateFullTooltip(FullBladderLaugh, true));
            OPTIONS.SAW_CORPSE_ADD_EFFECT.TOOLTIP.ReplaceText(Effect.CreateFullTooltip(SawCorpse, true));
            OPTIONS.RESPECT_GRAVE_ADD_EFFECT.TOOLTIP.ReplaceText(Effect.CreateFullTooltip(RespectGrave, true));
            OPTIONS.FALL_CONTUSION_ADD_EFFECT.TOOLTIP.ReplaceText(OPTIONS.FALL_CONTUSION_ADD_EFFECT.TOOLTIP.text
                + Effect.CreateFullTooltip(Contusion, true));
            Utils.CreateOptionsLocStringKeys(typeof(STRINGS));
        }
    }
}
