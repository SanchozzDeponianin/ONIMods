﻿using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace MoreEmotions
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [Option]
        public bool wake_up_lazy_ass { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool stress_cheering { get; set; } = true;

        [JsonIgnore]
        [Option]
        public LocText stress_cheering_effect { get; set; }

        [JsonProperty]
        [Option]
        public bool double_greeting { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool moonwalk_greeting { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool full_bladder_emote { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool full_bladder_laugh { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool full_bladder_add_effect { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool contaminated_food_emote { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool starvation_emote { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool alternative_binge_eat_emote { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool alternative_sleep_anims { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool alternative_narcoleptic_anims { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool saw_corpse_emote { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool saw_corpse_add_effect { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool respect_grave_emote { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool respect_grave_add_effect { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool wet_hands_emote { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool fall_contusion_emote { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool fall_contusion_add_effect { get; set; } = true;
    }
}
