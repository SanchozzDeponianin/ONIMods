using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Database;
using TUNING;
using Harmony;
#if USESPLIB
using PeterHan.PLib.Detours;
#endif

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

            var planOrderList = Traverse.Create(BUILDINGS.PLANORDER[index])?.Field("data")?.GetValue<List<string>>();
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
        // ванилька
        /* 
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
        */
        // длц
        /*
        public static void AddBuildingToTechnology(string tech, string buildingId)
        {
            var targetTech = Db.Get().Techs.TryGet(tech);
            if (targetTech != null)
            {
                targetTech.unlockedItemIDs.Add(buildingId);
            }
            else
            {
                Debug.LogWarning($"{modInfo.assemblyName}: Could not find '{tech}' tech.");
            }
        }
        */
        // "а теперь тушим оба окурка одновременно" (с)
        public static void AddBuildingToTechnology(string tech, string buildingId)
        {
            var tech_grouping = Traverse.Create(typeof(Techs))?.Field("TECH_GROUPING")?.GetValue<Dictionary<string, string[]>>();
            if (tech_grouping != null)
            {
                if (tech_grouping.ContainsKey(tech))
                {
                    List<string> techList = new List<string>(tech_grouping[tech]) { buildingId };
                    tech_grouping[tech] = techList.ToArray();
                }
                else
                {
                    Debug.LogWarning($"{modInfo.assemblyName}: Could not find '{tech}' tech in TECH_GROUPING.");
                }
            }
            else
            {
                var targetTech = Db.Get().Techs.TryGet(tech);
                if (targetTech != null)
                {
                    //targetTech.unlockedItemIDs.Add(buildingId);
                    Traverse.Create(targetTech)?.Field("unlockedItemIDs")?.GetValue<List<string>>()?.Add(buildingId);
                }
                else
                {
                    Debug.LogWarning($"{modInfo.assemblyName}: Could not find '{tech}' tech.");
                }
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
        // todo: старый вариант. оставить для совместимости, потом постепенно убрать.
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

        // замена текста в загруженной локализации
#if USESPLIB
        private static readonly IDetouredField<LocString, string> LocStringText = PDetours.DetourField<LocString, string>(nameof(LocString.text));
        public static void ReplaceText(this LocString locString, string newtext)
        {
            LocStringText.Set(locString, newtext);
        }
#else
        private static readonly MethodInfo LocStringSetText = typeof(LocString).GetProperty(nameof(LocString.text)).GetSetMethod(true);
        public static void ReplaceText(this LocString locString, string newtext)
        {
            LocStringSetText.Invoke(locString, new object[] { newtext });
        }
#endif
        public static void ReplaceText(this LocString locString, string search, string replacement)
        {
            locString.ReplaceText(locString, locString.text.Replace(search, replacement));
        }

        public static Dictionary<string, string> PrepareReplacementDictionary(this Dictionary<string, string> dictionary, string[] search, string replacementKeyTemplate)
        {
            if (dictionary == null)
                dictionary = new Dictionary<string, string>();
            foreach (string key in search)
            {
                if (!key.IsNullOrWhiteSpace())
                {
                    if (Strings.TryGet(string.Format(replacementKeyTemplate, key.Replace("{", "").Replace("}", "")), out StringEntry entry))
                    {
                        dictionary[key] = entry;
                    }
                }
            }
            return dictionary;
        }

        public static void ReplaceAllLocStringTextByDictionary(Type type, Dictionary<string, string> replacementDictionary)
        {
            var fields = type.GetFields(LocString.data_member_fields);
            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.FieldType == typeof(LocString))
                {
                    var locString = (LocString)fieldInfo.GetValue(null);
                    string text = locString.text;
                    foreach(var replacement in replacementDictionary)
                    {
                        text = text.Replace(replacement.Key, replacement.Value);
                    }
                    if (text != locString.text)
                    {
                        locString.ReplaceText(text);
                    }
                }
            }
            var nestedTypes = type.GetNestedTypes(LocString.data_member_fields);
            foreach (var nestedType in nestedTypes)
            {
                ReplaceAllLocStringTextByDictionary(nestedType, replacementDictionary);
            }
        }
    }
}
