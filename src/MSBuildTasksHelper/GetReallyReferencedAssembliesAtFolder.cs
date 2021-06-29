using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SanchozzONIMods
{
    /*
     * Так как теперь PLib имеет модульную артитектуру
     * Получение списка PLib*.dll которые реально необходимы компилируемому проекту, чтобы сделать илрепак
     * Позволяет уменьшить размер выходного файла
     * Нужно указать исходный свежескоплилированный dll файл и папку где лежать все PLib*.dll
     * 
     * ВНИМАНИЕ !!!
     * так как выгрузка загруженных библиотек в .НЕТ человеческим путём не предусмотрена:
     * А) придется сделать чтобы илрепак создавал новый выходной файл
     * Б) чтобы после компиляции сам МСБуилд закрылся и не висел в памяти, и не кидал ошибку при следующей сборке
     * нужно установить переменную среды MSBUILDDISABLENODEREUSE=1
     */
    public class GetReallyReferencedAssembliesAtFolder : Task
    {
        [Required]
        public string AssemblyName { get; set; }
        [Required]
        public string ReferencedAssembliesFolder { get; set; }

        [Output]
        public string[] ReallyReferencedAssemblies { get; set; }

        private void GetReallyReferencedAssemblies(string AssemblyName, List<string> list)
        {
            var assembly = Assembly.ReflectionOnlyLoadFrom(AssemblyName);
            foreach (AssemblyName an in assembly.GetReferencedAssemblies())
            {
                var file = Path.Combine(ReferencedAssembliesFolder, an.Name + ".dll");
                if (!list.Contains(file) && File.Exists(file))
                {
                    list.Add(file);
                    GetReallyReferencedAssemblies(file, list);
                }
            }
        }

        public override bool Execute()
        {
            bool result = false;
            try
            {
                var list = new List<string>();
                GetReallyReferencedAssemblies(AssemblyName, list);
                ReallyReferencedAssemblies = list.ToArray();
                result = true;
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.High, $"An error occurred while executing '{nameof(GetReallyReferencedAssembliesAtFolder)}'");
                Log.LogErrorFromException(e, true);
            }
            return result;
        }
    }
}
