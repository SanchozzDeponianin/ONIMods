using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using TUNING;
using Harmony;

namespace SanchozzONIMods.Lib
{
    public static class Utils
    {
        // информация о моде
        public class ModInfo
        {
            public readonly string assemblyName;
            public readonly string rootDirectory;
            public readonly string langDirectory;
            public readonly string spritesDirectory;
            public readonly string version;
            public ModInfo()
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                assemblyName = assembly.GetName().Name;
                rootDirectory = Path.GetDirectoryName(assembly.Location);
                langDirectory = Path.Combine(rootDirectory, "translations");
                spritesDirectory = Path.Combine(rootDirectory, "sprites");
                version = assembly.GetName().Version.ToString();
            }
        }

        private static ModInfo _modinfo;

        public static ModInfo modInfo
        {
            get
            {
                if (_modinfo == null)
                {
                    _modinfo = new ModInfo();
                }
                return _modinfo;
            }
        }

#if !USESPLIB
        public static void OnLoad()
        {
            Debug.Log($"Mod {modInfo.assemblyName} loaded, version: {modInfo.version}");
        }
#endif

        // логирование со стактрасом
        public static void LogExcWarn(Exception thrown)
        {
            string format = "[{0}] {1} {2} {3}";
            object[] array = new object[4];
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            object obj;
            if (callingAssembly == null)
            {
                obj = null;
            }
            else
            {
                AssemblyName name = callingAssembly.GetName();
                obj = (name?.Name);
            }
            array[0] = (obj ?? "?");
            array[1] = thrown.GetType();
            array[2] = thrown.Message;
            array[3] = thrown.StackTrace;
            Debug.LogWarningFormat(format, array);
        }


        // добавляем постройки в меню
        public static void AddBuildingToPlanScreen(HashedString category, string buildingId, string addAfterBuildingId = null)
        {
            int index = BUILDINGS.PLANORDER.FindIndex(x => x.category == category);
            if (index == -1)
            {
                Debug.LogWarning($"{modInfo.assemblyName}: Could not find '{category}' category in the building menu.");
                return;
            }

            IList<string> planOrderList = BUILDINGS.PLANORDER[index].data as IList<string>;
            if (planOrderList == null)
            {
                Debug.LogWarning($"{modInfo.assemblyName}: Could not add '{buildingId}' to the building menu.");
                return;
            }

            int neighborIdx = planOrderList.IndexOf(addAfterBuildingId);

            if (neighborIdx != -1)
                planOrderList.Insert(neighborIdx + 1, buildingId);
            else
                planOrderList.Add(buildingId);
        }

        // добавляем постройки в технологии
        public static void AddBuildingToTechnology(string tech, string buildingId)
        {
            if (Database.Techs.TECH_GROUPING.ContainsKey(tech))
            {
                List<string> techList = new List<string>(Database.Techs.TECH_GROUPING[tech]) { buildingId };
                Database.Techs.TECH_GROUPING[tech] = techList.ToArray();
            }
            else
            {
                Debug.LogWarning($"{modInfo.assemblyName}: Could not find '{tech}' tech in TECH_GROUPING.");
            }
        }

        // загружаем строки для локализации
        public static void InitLocalization(Type locstring_tree_root, string filename_prefix = "", bool writeStringsTemplate = false)
        {
            // регистрируемся
            Localization.RegisterForTranslation(locstring_tree_root);

            if (writeStringsTemplate)  // для записи шаблона !!!!
            {
                try
                {
                    Localization.GenerateStringsTemplate(locstring_tree_root, modInfo.langDirectory);
                }
                catch (IOException e)
                {
                    Debug.LogWarning($"{modInfo.assemblyName} Failed to write localization template.");
                    LogExcWarn(e);
                }
            }

            Localization.Locale locale = Localization.GetLocale();
            if (locale != null)
            {
                try
                {
                    string langFile = Path.Combine(modInfo.langDirectory, filename_prefix + locale.Code + ".po");
                    if (File.Exists(langFile))
                    {
                        // перезагружаем строки
#if DEBUG
                        Debug.Log($"{modInfo.assemblyName} try load LangFile: {langFile}");
#endif
                        Localization.OverloadStrings(Localization.LoadStringsFile(langFile, false));
                    }
                }
                catch (IOException e)
                {
                    Debug.LogWarning($"{modInfo.assemblyName} Failed to load localization.");
                    LogExcWarn(e);
                }
            }

            // выполняем замену если нужно. тип должен содержать статичный метод "DoReplacement"
            MethodInfo methodInfo = AccessTools.Method(locstring_tree_root, "DoReplacement", new Type[0]);
            if (methodInfo != null)
            {
                methodInfo.Invoke(null, null);
            }
            // дополнительно создаем ключи. обычно и без этого работает, но не всегда.
            // нужно для опций.
            LocString.CreateLocStringKeys(locstring_tree_root, locstring_tree_root.Namespace + ".");
        }

        // замена текста в загруженной локализации
        public static void ReplaceLocString(ref LocString locString, string search, string replacement)
        {
            LocString newlocString = new LocString(locString.text.Replace(search, replacement), locString.key.String);
            locString = newlocString;
        }
        public static void ReplaceLocString(ref LocString locString, string newtext)
        {
            LocString newlocString = new LocString(newtext, locString.key.String);
            locString = newlocString;
        }
    }
}
