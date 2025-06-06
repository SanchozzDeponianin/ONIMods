﻿using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace AthleticsGenerator
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [Option]
        [Limit(1, 30)]
        public int watts_per_level { get; set; } = 10;

        [JsonProperty]
        [Option]
        public bool enable_meter { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool enable_light { get; set; } = true;
    }
}
