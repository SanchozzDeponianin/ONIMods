using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace Test
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [ModInfo("Test", null, null, false)]
    internal sealed class Options
    {
        [Option("Wattage", "How many watts you can use before exploding.")]
        [Limit(1, 50000)]
        [JsonProperty]
        public float Watts { get; set; }

        public Options()
        {
            Watts = 10000f; // defaults to 10000, e.g. if the config doesn't exist
        }
    }
}
