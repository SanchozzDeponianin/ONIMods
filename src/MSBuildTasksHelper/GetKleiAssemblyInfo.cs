using System;
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

        [Output]
        public string KleiBuildVersion { get; set; }
        [Output]
        public string KleiBuildBranch { get; set; }

        public override bool Execute()
        {
            bool result = false;
            try
            {
                Log.LogMessage(MessageImportance.High, $"Reading assembly '{AssemblyCSharp}'");
                var assembly = Assembly.ReflectionOnlyLoadFrom(AssemblyCSharp);
                var kleiversion = assembly.GetType("KleiVersion", true);
                KleiBuildVersion = ((uint)kleiversion.GetField("ChangeList").GetRawConstantValue()).ToString();
                KleiBuildBranch = (string)kleiversion.GetField("BuildBranch").GetRawConstantValue();
                Log.LogMessage(MessageImportance.High, $"KleiBuildVersion: {KleiBuildVersion}\nKleiBuildBranch:  {KleiBuildBranch}");
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
