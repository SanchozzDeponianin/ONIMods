using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SanchozzONIMods
{
    /*
     * Получение информации о версии dll игры в процессе компиляции.
     * 
     * ВНИМАНИЕ !!!
     * изза технических ограничений следует убедиться что одновременно компилируемые проекты 
     * используют одну и туже версию Assembly-CSharp.dll, иначе МСБуилд кинет ошибку 
     * так как выгрузка загруженных библиотек в .НЕТ человеческим путём не предусмотрена
     * чтобы после компиляции сам МСБуилд закрылся и не висел в памяти, и не кидал ошибку при следующей сборке
     * нужно установить переменную среды MSBUILDDISABLENODEREUSE=1
     */
    public class GetKleiAssemblyInfo : Task
    {
        [Required]
        public string AssemblyCSharp { get; set; }
        [Required]
        public string LibraryPath { get; set; }

        [Output]
        public string KleiGameVersion { get; set; }
        [Output]
        public string KleiBuildNumber { get; set; }
        [Output]
        public string KleiBuildBranch { get; set; }

        public const string INVALID = "??";

        public override bool Execute()
        {
            bool result = false;
            try
            {
                Log.LogMessage(MessageImportance.High, $"Reading assembly '{AssemblyCSharp}'");
                var assembly = Assembly.ReflectionOnlyLoadFrom(AssemblyCSharp);
                var flag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
                var KleiVersion = assembly.GetType("KleiVersion", true);
                KleiBuildNumber = ((uint)KleiVersion.GetField("ChangeList", flag).GetRawConstantValue()).ToString();
                KleiBuildBranch = (string)KleiVersion.GetField("BuildBranch", flag).GetRawConstantValue();
                try
                {
                    // лядь, ReflectionOnlyLoad не подтягивает зависимости, будем вручную дёргать
                    foreach (var dll in new string[] { "UnityEngine.CoreModule.dll", "netstandard.dll" })
                    {
                        var file = Path.Combine(LibraryPath, dll);
                        if (File.Exists(file))
                            Assembly.ReflectionOnlyLoadFrom(file);
                    }
                    var LaunchInitializer = assembly.GetType("LaunchInitializer", true);
                    KleiGameVersion = (string)LaunchInitializer.GetField("PREFIX", flag).GetRawConstantValue() +
                        ((int)LaunchInitializer.GetField("UPDATE_NUMBER", flag).GetRawConstantValue()).ToString();
                }
                catch (Exception e)
                {
                    Log.LogWarningFromException(e, true);
                    KleiGameVersion = INVALID;
                }
                Log.LogMessage(MessageImportance.High, $"Game Version: {KleiGameVersion}-{KleiBuildNumber}\t Branch: {KleiBuildBranch}");
                result = true;
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.High, $"An error occurred while executing '{nameof(GetKleiAssemblyInfo)}'");
                Log.LogErrorFromException(e, true);
            }
            return result;
        }
    }
}
