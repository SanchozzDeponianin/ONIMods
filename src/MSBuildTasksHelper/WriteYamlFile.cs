using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SanchozzONIMods
{
    /*
     * ВНИМАНИЕ !!!
     * изза тех. ограничений следует убедиться что одновременно компилируемые проекты 
     * используют одну и туже версию Assembly-CSharp.dll, иначе МСБуилд кинет ошибку 
     * так как выгрузка загруженных библиотек в .НЕТ человеческим путём не предусмотрена
     * чтобы после компиляции сам МСБуилд закрылся и не висел в памяти, и не кидал ошибку при следующей сборке
     * нужно установить переменную среды MSBUILDDISABLENODEREUSE=1
     */ 
    public class WriteYamlFile : Task
    {
        [Required]
        public string ConfigurationName { get; set; }
        [Required]
        public string GameFolder { get; set; }
        [Required]
        public string OutputYamlFile { get; set; }
        [Output]
        public string KleiBuildVersion { get; set; }
        [Output]
        public string KleiBuildBranch { get; set; }

        public override bool Execute()
        {
            // сигнатура для записи в ямл. 
            // для начала будем записывать только какую нибуть одну сигнатуру, хотя игра может воспринимать несколько.
            string signature;
            switch (ConfigurationName)
            {
                case "VANILLA":
                    {
                        signature = "VANILLA_ID";
                        break;
                    }
                case "EXPANSION1":
                    {
                        signature = "EXPANSION1_ID";
                        break;
                    }
                default:
                    {
                        signature = "ALL";
                        break;
                    }
            }

            bool result = false;
            try
            {
                var path = Path.Combine(GameFolder, "Assembly-CSharp.dll");
                Log.LogMessage(MessageImportance.High, $"Reading assembly '{path}'");

                var assembly = Assembly.ReflectionOnlyLoadFrom(path);
                var kleiversion = assembly.GetType("KleiVersion", true);
                KleiBuildVersion = ((uint)kleiversion.GetField("ChangeList").GetRawConstantValue()).ToString();
                KleiBuildBranch = (string)kleiversion.GetField("BuildBranch").GetRawConstantValue();

                string yaml = $"supportedContent: {signature}\nlastWorkingBuild: {KleiBuildVersion}\n";
                Log.LogMessage(MessageImportance.High, $"Writing file '{OutputYamlFile}'\n{yaml}");
                File.WriteAllText(OutputYamlFile, yaml, Encoding.ASCII);
                result = true;
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.High, $"An error occurred while executing '{nameof(WriteYamlFile)}'");
                Log.LogErrorFromException(e, true);
            }
            return result;
        }
    }
}
