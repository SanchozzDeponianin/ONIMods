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

        public Emote Kick;
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
        public Emote MoonWalk;
        public Emote Rage;
        public Emote BreakKick;
        public Emote BreakPunch;

        public MoreMinionEmotes(ResourceSet parent) : base(nameof(MoreMinionEmotes), parent)
        {
            InitializeEmotes();
            Instance = this;
            Utils.MuteMouthFlapSpeech(FullBladder, Laugh, PutOff, MoonWalk);
        }

        private void InitializeEmotes()
        {
            Kick = new Emote(this, nameof(Kick), new EmoteStep[]
            {
                new EmoteStep{anim = "kick_pre"},
                new EmoteStep{anim = "kick_loop"},
                new EmoteStep{anim = "kick_pst"}
            }, "anim_emotes_default_kanim");
            FullBladder = new Emote(this, nameof(FullBladder), DEFAULT_IDLE_STEPS, "anim_idle_bladder_kanim");
            EatHand = new Emote(this, nameof(EatHand), new EmoteStep[]
            {
                new EmoteStep{anim = "working_pre"},
                new EmoteStep{anim = "working_loop"},
                new EmoteStep{anim = "work_pst"}
            }, "anim_out_of_reach_binge_eat_kanim");
            HandWipe = new Emote(this, nameof(HandWipe), DEFAULT_STEPS, "anim_react_hand_wipe_kanim");
            Laugh = new Emote(this, nameof(Laugh), DEFAULT_STEPS, "anim_react_laugh_kanim");
            Respect = new Emote(this, nameof(Respect), DEFAULT_STEPS, "anim_react_respect_kanim");
            Respect_NoHat = new Emote(this, nameof(Respect_NoHat), new EmoteStep[] { new EmoteStep { anim = "react_no_hat" } }, "anim_react_respect_kanim");
            PutOff = new Emote(this, nameof(PutOff), new EmoteStep[]
            {
                new EmoteStep{anim = "putoff_pre"},
                new EmoteStep{anim = "putoff_loop"},
                new EmoteStep{anim = "putoff_loop"},
                new EmoteStep{anim = "putoff_pst"}
            }, "anim_putoff_kanim");
            Stressed = new Emote(this, nameof(Stressed), DEFAULT_WORK_STEPS, "anim_react_stressed_dupe_kanim");
            Cheering = new Emote(this, nameof(Cheering), DEFAULT_WORK_STEPS, "anim_react_cheering_dupe_kanim");
            FistBump = new Emote(this, nameof(FistBump), new EmoteStep[] { new EmoteStep { anim = "react_l" } }, "anim_react_fistbump_kanim");
            HighFive = new Emote(this, nameof(HighFive), new EmoteStep[] { new EmoteStep { anim = "react_l" } }, "anim_react_highfive_kanim");
            MoonWalk = new Emote(this, nameof(MoonWalk), new EmoteStep[]
            {
                new EmoteStep{anim = "floor_floor_moonwalk_1_0_pre"},
                //new EmoteStep{anim = "floor_floor_moonwalk_1_0_loop"},
                new EmoteStep{anim = "floor_floor_moonwalk_1_0_pst"}
            }, "anim_react_moonwalk_kanim");
            Rage = new Emote(this, nameof(Rage), new EmoteStep[]
            {
                new EmoteStep{anim = "idle_pre"},
                new EmoteStep{anim = "rage_pre"},
                new EmoteStep{anim = "rage_loop"},
                new EmoteStep{anim = "rage_pst"},
                new EmoteStep{anim = "idle_pst"},
            }, "anim_rage_kanim");
            // эти требуют дополнительных анимов anim_emotes_default_kanim и anim_break_kanim
            BreakKick = new Emote(this, nameof(BreakKick), new EmoteStep[]
            {
                new EmoteStep{anim = "idle_pre"},
                new EmoteStep{anim = "working_pre"},
                new EmoteStep{anim = "break_loop_kick"},
                new EmoteStep{anim = "working_pst"},
                new EmoteStep{anim = "idle_pst"},
            }, "anim_rage_kanim");
            BreakPunch = new Emote(this, nameof(BreakPunch), new EmoteStep[]
            {
                new EmoteStep{anim = "idle_pre"},
                new EmoteStep{anim = "working_pre"},
                new EmoteStep{anim = "break_loop_punch"},
                new EmoteStep{anim = "working_pst"},
                new EmoteStep{anim = "idle_pst"},
            }, "anim_rage_kanim");
        }
    }
}
