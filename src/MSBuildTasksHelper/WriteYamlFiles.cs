using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using YamlDotNet.Serialization;

namespace SanchozzONIMods
{
    public class WriteYamlFiles : Task
    {
        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public string StaticID { get; set; }

        public string RequiredDlcIds { get; set; }

        public string ForbiddenDlcIds { get; set; }

        [Required]
        public int MinimumSupportedBuild { get; set; }

        public string Version { get; set; }

        [Required]
        public int APIVersion { get; set; }

        public override bool Execute()
        {
            var mod = new Mod
            {
                title = Title,
                description = Description,
                staticID = StaticID
            };

            var modInfo = new ModInfo
            {
                requiredDlcIds = !string.IsNullOrEmpty(RequiredDlcIds) ? RequiredDlcIds.Split(',') : Array.Empty<string>(),
                forbiddenDlcIds = !string.IsNullOrEmpty(ForbiddenDlcIds) ? ForbiddenDlcIds.Split(',') : Array.Empty<string>(),
                minimumSupportedBuild = MinimumSupportedBuild,
                APIVersion = APIVersion,
                version = Version
            };

            var serializer = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections).Build();
            var modYaml = serializer.Serialize(mod);
            var modInfoYaml = serializer.Serialize(modInfo);

            var modPath = Path.Combine(OutputPath, "mod.yaml");
            var modInfoPath = Path.Combine(OutputPath, "mod_info.yaml");

            try
            {
                File.WriteAllText(modPath, modYaml);
                File.WriteAllText(modInfoPath, modInfoYaml);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
