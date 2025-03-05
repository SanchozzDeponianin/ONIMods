using YamlDotNet.Serialization;

namespace SanchozzONIMods
{
    public class ModInfo
    {
        [YamlIgnore]
        public string supportedContent { get; set; }
        public string[] requiredDlcIds { get; set; }
        public string[] forbiddenDlcIds { get; set; }
        public int minimumSupportedBuild { get; set; }
        public string version { get; set; }
        public int APIVersion { get; set; }
    }
}
