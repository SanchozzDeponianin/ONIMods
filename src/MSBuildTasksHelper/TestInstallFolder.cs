﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using YamlDotNet.Serialization;

namespace SanchozzONIMods
{
    /*
    Тестируем конечную папку в которую должен быть установлен свеже скомпиленый мод
    Принимаем решение
     * архивировать или нет ранее установленный мод
     * устанавливать свежий мод в корень папки или в архив
    На основе наличия файла mod_info.yaml и сравнения версий игры для которой предназначены моды
    */
    public class TestInstallFolder : Task
    {
        [Required]
        public string KnownGameVersionsFile { get; set; }
        [Required]
        public string RootModInfoFile { get; set; }
        [Required]
        public string CurrentGameVersion { get; set; }
        [Required]
        public int CurrentBuildNumber { get; set; }

        [Output]
        public string PreviousGameVersion { get; set; }
        [Output]
        public int PreviousBuildNumber { get; set; }
        [Output]
        public bool DoInstallToRootFolder { get; set; }
        [Output]
        public bool NeededArchiving { get; set; }

        public override bool Execute()
        {
            try
            {
                KnownGameVersions data;
                // загружаем исвестные версии
                if (File.Exists(KnownGameVersionsFile))
                {
                    data = new DeserializerBuilder().IgnoreUnmatchedProperties().Build()
                        .Deserialize<KnownGameVersions>(File.ReadAllText(KnownGameVersionsFile));
                }
                else
                    data = new();
                data.PreserveVersion ??= GetKleiAssemblyInfo.INVALID;
                data.KnownVersions.Sort();
                // добавляем туды нынешнюю версию если надо
                bool need_write = false;
                if (CurrentGameVersion != GetKleiAssemblyInfo.INVALID)
                {
                    int i = data.KnownVersions.FindIndex(info => info.GameVersion == CurrentGameVersion);
                    if (i == -1)
                    {
                        data.KnownVersions.Add(new GameVersionInfo() { GameVersion = CurrentGameVersion, MinimumBuildNumber = CurrentBuildNumber });
                        need_write = true;
                    }
                    else if (data.KnownVersions[i].MinimumBuildNumber > CurrentBuildNumber)
                    {
                        var info = data.KnownVersions[i];
                        info.MinimumBuildNumber = CurrentBuildNumber;
                        data.KnownVersions[i] = info;
                        need_write = true;
                    }
                    data.KnownVersions.Sort();
                }
                // и записываем
                if (need_write)
                {
                    File.WriteAllText(KnownGameVersionsFile,
                        new SerializerBuilder().Build().Serialize(data));
                }

                // в корне мода нет инфо файла == ставим прям в корень
                if (!File.Exists(RootModInfoFile))
                {
                    PreviousBuildNumber = 0;
                    PreviousGameVersion = GetKleiAssemblyInfo.INVALID;
                    DoInstallToRootFolder = true;
                    NeededArchiving = false;
                    return true;
                }

                // считываем существующий инфо файл
                var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
                var modInfo = deserializer.Deserialize<ModInfo>(File.ReadAllText(RootModInfoFile));
                PreviousBuildNumber = modInfo.minimumSupportedBuild;
                PreviousGameVersion = GetKleiAssemblyInfo.INVALID;

                // пытаемся примерно определить соответсвующую ему версию
                for (int j = 0; j < data.KnownVersions.Count; j++)
                {
                    if (PreviousBuildNumber < data.KnownVersions[j].MinimumBuildNumber)
                        break;
                    PreviousGameVersion = data.KnownVersions[j].GameVersion;
                }

                if (CurrentGameVersion == GetKleiAssemblyInfo.INVALID)
                {
                    // по какой то причине не удалось корректно считать нынешнюю версию
                    // == на всякий случай ставим вовнутрь архива
                    DoInstallToRootFolder = false;
                    NeededArchiving = false;
                }
                else if (CurrentGameVersion == PreviousGameVersion) // версии совпадают == ставим прям в корень, архивация не нужна
                {
                    DoInstallToRootFolder = true;
                    NeededArchiving = false;
                }
                else // версии не совпадают
                {
                    if (CurrentBuildNumber > PreviousBuildNumber) // нынешняя новее == ставим прям в корень, нужна архивация
                    {
                        DoInstallToRootFolder = true;
                        NeededArchiving = true;
                    }
                    else // нынешняя древнее == ставим вовнутрь архива, архивация не нужна
                    {
                        DoInstallToRootFolder = false;
                        NeededArchiving = false;
                    }
                }
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
