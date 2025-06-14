using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using YamlDotNet.Serialization;

namespace SanchozzONIMods
{
    /*
    На основе анализа файлов mod_info.yaml вычисляем наиболее старые и ненужные архивы для удаления
    */
    public class FindTooOldArchivedVersions : Task
    {
        [Required]
        public string KnownGameVersionsFile { get; set; }
        [Required]
        public string RootModInfoFile { get; set; }
        [Required]
        public string[] ArchivedModInfoFiles { get; set; }

        [Output]
        public string[] TooOldArchivedVersions { get; set; }

        public override bool Execute()
        {
            try
            {
                var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
                KnownGameVersions data;
                // загружаем исвестные версии
                if (File.Exists(KnownGameVersionsFile))
                    data = deserializer.Deserialize<KnownGameVersions>(File.ReadAllText(KnownGameVersionsFile));
                else
                    data = new();
                data.PreserveVersion ??= GetKleiAssemblyInfo.INVALID;
                data.KnownVersions.Sort();

                var candidates = new List<string>();
                if (data.KnownVersions.Count > 3)
                {
                    // будем считать что
                    // три последние версии это "бета", "основная" и "предыдущая"
                    // либо две последние версии это "основная" и "предыдущая"
                    // "предыдущая" должна быть указана в 'PreserveVersion'
                    int i = data.KnownVersions.FindIndex(info => info.GameVersion == data.PreserveVersion);
                    if (i == -1 || i > data.KnownVersions.Count - 2)
                    {
                        Log.LogError("'PreserveVersion' not specified or invalid, abort cleaning!");
                    }
                    else
                    {
                        int prew = data.KnownVersions[i].MinimumBuildNumber;
                        int live = data.KnownVersions[i + 1].MinimumBuildNumber;

                        var ArchivedVersions = new Dictionary<int, string>();

                        if (File.Exists(RootModInfoFile))
                        {
                            int build = deserializer.Deserialize<ModInfo>(File.ReadAllText(RootModInfoFile)).minimumSupportedBuild;
                            ArchivedVersions[build] = RootModInfoFile;
                        }

                        if (ArchivedModInfoFiles != null)
                        {
                            foreach (var file in ArchivedModInfoFiles)
                            {
                                if (File.Exists(file))
                                {
                                    int build = deserializer.Deserialize<ModInfo>(File.ReadAllText(file)).minimumSupportedBuild;
                                    ArchivedVersions[build] = file;
                                }
                            }
                        }

                        // сохраняем версии от "предыдущей" и новее
                        // если не было версии == "предыдущая" сохраняем ещё одну наиболее новую из всех
                        var buildNumbers = ArchivedVersions.Keys.ToList();
                        buildNumbers.Sort();
                        buildNumbers.RemoveAll(build => build >= live);
                        if (buildNumbers.RemoveAll(build => build >= prew) == 0 && buildNumbers.Count > 0)
                        {
                            buildNumbers.RemoveAt(buildNumbers.Count - 1);
                        }
                        foreach (int build in buildNumbers)
                        {
                            if (ArchivedVersions[build] != RootModInfoFile)
                                candidates.Add(Path.GetDirectoryName(ArchivedVersions[build]));
                        }
                    }
                }
                TooOldArchivedVersions = candidates.ToArray();
                return true;
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.High, $"An error occurred while executing '{nameof(TestInstallFolder)}'");
                Log.LogErrorFromException(e, true);
            }
            return false;
        }
    }
}
