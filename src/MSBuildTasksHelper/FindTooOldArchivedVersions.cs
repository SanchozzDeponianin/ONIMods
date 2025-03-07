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

                List<GameVersionInfo> KnownVersions;
                // загружаем исвестные версии
                if (File.Exists(KnownGameVersionsFile))
                    KnownVersions = deserializer.Deserialize<KnownGameVersions>(File.ReadAllText(KnownGameVersionsFile)).KnownVersions.ToList();
                else
                    KnownVersions = new List<GameVersionInfo>();
                KnownVersions.Sort();

                var candidates = new List<string>();
                if (KnownVersions.Count > 3)
                {
                    // для простоты будем считать что три последние версии это "бета", "основная" и "предыдущая"
                    int prew = KnownVersions[KnownVersions.Count - 3].MinimumBuildNumber;
                    int live = KnownVersions[KnownVersions.Count - 2].MinimumBuildNumber;

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
                if (candidates.Count > 0)
                    Log.LogWarning("Too Old Archived Versions, delete:\n" + string.Join("\n", candidates));
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
