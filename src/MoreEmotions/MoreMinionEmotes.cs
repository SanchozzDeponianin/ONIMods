using Klei.AI;
using SanchozzONIMods.Lib;

namespace MoreEmotions
{
    public class MoreMinionEmotes : ResourceSet<Emote>
    {
        public static MoreMinionEmotes Instance;

        private static readonly EmoteStep[] DEFAULT_STEPS = new EmoteStep[] { new EmoteStep { anim = "react" } };

        private static readonly EmoteStep[] DEFAULT_IDLE_STEPS = new EmoteStep[]
        {
            new EmoteStep{anim = "idle_pre"},
            new EmoteStep{anim = "idle_default"},
            new EmoteStep{anim = "idle_pst"}
        };

        private static readonly EmoteStep[] DEFAULT_WORK_STEPS = new EmoteStep[]
        {
            new EmoteStep{anim = "working_pre"},
            new EmoteStep{anim = "working_loop"},
            new EmoteStep{anim = "working_pst"}
        };

        public Emote FullBladder;
        public Emote EatHand;
        public Emote HandWipe;
        public Emote Laugh;
        public Emote Respect;
        public Emote Respect_NoHat;
        public Emote PutOff;
        public Emote Stressed;
        public Emote Cheering;
        public Emote FistBump;
        public Emote HighFive;

        public MoreMinionEmotes(ResourceSet parent) : base(nameof(MoreMinionEmotes), parent)
        {
            InitializeEmotes();
            Instance = this;
            Utils.MuteMouthFlapSpeech(FullBladder, Laugh, PutOff);
        }

        private void InitializeEmotes()
        {
            FullBladder = new Emote(this, nameof(FullBladder), DEFAULT_IDLE_STEPS, "anim_idle_bladder_kanim");
            EatHand = new Emote(this, nameof(EatHand), new EmoteStep[]
            {
                new EmoteStep{anim = "working_pre"},
                new EmoteStep{anim = "working_loop"},
                new EmoteStep{anim = "work_pst"}
            }, "anim_interrupt_binge_eat_kanim");
            HandWipe = new Emote(this, nameof(HandWipe), DEFAULT_STEPS, "anim_react_hand_wipe_kanim");
            Laugh = new Emote(this, nameof(Laugh), DEFAULT_STEPS, "anim_react_laugh_kanim");
            Respect = new Emote(this, nameof(Respect), DEFAULT_STEPS, "anim_react_respect_kanim");
            Respect_NoHat = new Emote(this, nameof(Respect_NoHat), new EmoteStep[] { new EmoteStep { anim = "react_no_hat" } }, "anim_react_respect_kanim");
            PutOff = new Emote(this, nameof(PutOff), new EmoteStep[]
            {
                new EmoteStep{anim = "putoff_pre"},
                new EmoteStep{anim = "putoff_loop"},
                new EmoteStep{anim = "putoff_pst"}
            }, "anim_putoff_kanim");
            Stressed = new Emote(this, nameof(Stressed), DEFAULT_WORK_STEPS, "anim_react_stressed_dupe_kanim");
            Cheering = new Emote(this, nameof(Cheering), DEFAULT_WORK_STEPS, "anim_react_cheering_dupe_kanim");
            FistBump = new Emote(this, nameof(FistBump), new EmoteStep[] { new EmoteStep { anim = "react_l" } }, "anim_react_fistbump_kanim");
            HighFive = new Emote(this, nameof(HighFive), new EmoteStep[] { new EmoteStep { anim = "react_l" } }, "anim_react_highfive_kanim");
        }
    }
}
