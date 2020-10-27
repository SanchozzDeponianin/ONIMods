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
        public static void AddBuildingToPlanScreen(HashedString category, string buildingId, string addAfterBuildingId = null)
        {
            int index = BUILDINGS.PLANORDER.FindIndex(x => x.category == category);

            if (index == -1)
                return;

            IList<string> planOrderList = BUILDINGS.PLANORDER[index].data as IList<string>;
            if (planOrderList == null)
            {
                return;
            }

            int neighborIdx = planOrderList.IndexOf(addAfterBuildingId);

            if (neighborIdx != -1)
                planOrderList.Insert(neighborIdx + 1, buildingId);
            else
                planOrderList.Add(buildingId);
        }

        public static void AddBuildingToTechnology(string tech, string buildingId)
        {
            List<string> techList = new List<string>(Database.Techs.TECH_GROUPING[tech]) { buildingId };
            Database.Techs.TECH_GROUPING[tech] = techList.ToArray();
        }

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
            global::Debug.LogWarningFormat(format, array);
        }

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

        public static void InitLocalization(Type locstring_tree_root, Localization.Locale locale, string filename_prefix = "", bool writeStringsTemplate = false)
        {
            // регистрируемся
#if DEBUG
            Debug.Log(modInfo.assemblyName + " Loaded: Version " + modInfo.version);
#endif
            Localization.RegisterForTranslation(locstring_tree_root);
            if (writeStringsTemplate)
            {
                Localization.GenerateStringsTemplate(locstring_tree_root, modInfo.langDirectory);    // для записи шаблона !!!!
            }
            if (locale != null)
            {
                string langFile = Path.Combine(modInfo.langDirectory, filename_prefix + locale.Code + ".po");
                if (File.Exists(langFile))
                {
                    // перезагружаем строки
#if DEBUG
                    Debug.Log(modInfo.assemblyName + " try load LangFile: " + langFile);
#endif
                    Localization.OverloadStrings(Localization.LoadStringsFile(langFile, false));
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
    }
}
	