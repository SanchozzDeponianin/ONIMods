using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TUNING;
using HarmonyLib;
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
        public static void AddBuildingToPlanScreen(HashedString category, string buildingId, string subcategoryID = "uncategorized", string addAfterBuildingId = null)
        {
            int index = BUILDINGS.PLANORDER.FindIndex(x => x.category == category);
            if (index == -1)
            {
                Debug.LogWarning($"{modInfo.assemblyName}: Could not find '{category}' category in the building menu.");
                return;
            }
            var planOrderList = BUILDINGS.PLANORDER[index].buildingAndSubcategoryData;
            if (planOrderList == null)
            {
                Debug.LogWarning($"{modInfo.assemblyName}: Could not add '{buildingId}' to the building menu.");
                return;
            }
            var item = new KeyValuePair<string, string>(buildingId, subcategoryID);
            int neighborIdx = (addAfterBuildingId == null) ? -1 : planOrderList.FindIndex(x => x.Key == addAfterBuildingId);
            if (neighborIdx != -1)
                planOrderList.Insert(neighborIdx + 1, item);
            else
                planOrderList.Add(item);
        }

        // добавляем постройки в технологии
        // новая ванилька и длц
        public static void AddBuildingToTechnology(string tech, params string[] buildingIds)
        {
            var targetTech = Db.Get().Techs.TryGet(tech);
            if (targetTech != null)
            {
                targetTech.AddUnlockedItemIDs(buildingIds);
            }
            else
            {
                Debug.LogWarning($"{modInfo.assemblyName}: Could not find '{tech}' tech.");
            }
        }

        // получаем длительность анимации в аним файле. или общую длительность нескольких анимаций
        public static float GetAnimDuration(KAnimFile kAnimFile, params string[] anims)
        {
            if (kAnimFile == null)
                throw new ArgumentNullException(nameof(kAnimFile));
            if (anims == null)
                throw new ArgumentNullException(nameof(anims));
            var kanim_data = kAnimFile.GetData();
            float duration = 0;
            for (int i = 0; i < kanim_data.animCount; i++)
            {
                var anim = kanim_data.GetAnim(i);
                if (anims.Contains(anim.name))
                    duration += anim.numFrames / anim.frameRate;
            }
            return duration;
        }

        // загружаем строки для локализации
        public static void InitLocalization(Type locstring_tree_root, bool writeStringsTemplate = false)
        {
            // регистрируемся
            Localization.RegisterForTranslation(locstring_tree_root);

            if (writeStringsTemplate)  // для записи шаблона
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

            // перезагружаем строки
            var localeCode = Localization.GetLocale()?.Code;
            if (string.IsNullOrEmpty(localeCode))
                localeCode = Localization.GetCurrentLanguageCode();
            if (!string.IsNullOrEmpty(localeCode))
            {
                try
                {
                    string langFile = Path.Combine(modInfo.langDirectory, localeCode + ".po");
                    if (File.Exists(langFile))
                    {
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
            //LocString.CreateLocStringKeys(locstring_tree_root, locstring_tree_root.Namespace + ".");

            // дополнительно создаем ключи специально для опций.
            // ключи должны получиться в формате 
            // "STRINGS.{Namespace}.OPTIONS.{Name}.XXX"
            // где ХХХ = NAME TOOLTIP или CATEGORY соотвественно
            // todo: теперь надо все моды причесать в соответсвии
            var locstring_tree_options = locstring_tree_root.GetNestedType("OPTIONS");
            if (locstring_tree_options != null)
            {
                string path = $"STRINGS.{locstring_tree_options.Namespace.ToUpperInvariant()}.";
                LocString.CreateLocStringKeys(locstring_tree_options, path);
                CreateEmptyLocStringKeys(locstring_tree_options, path);
            }
        }

        // создание "фейковых" пустых ключей для опций, если настоящий не существует
        private static readonly string[] OptionsKeyNames = new string[] { "NAME", "TOOLTIP", "CATEGORY" };
        private static void CreateEmptyLocStringKeys(Type type, string parent_path = "STRINGS.", int depth = 0)
        {
            string path = (parent_path ?? string.Empty) + type.Name + ".";
            if (depth > 0)
            {
                int x = 0;
                foreach (string name in OptionsKeyNames)
                {
                    string key = path + name;
                    if (!Strings.TryGet(key, out var entry))
                    {
                        Strings.Add(key, string.Empty);
#if DEBUG
                        Debug.Log($"{key} = \"{string.Empty}\"");
#endif
                        x++;
                    }
#if DEBUG
                    else
                    {
                        Debug.Log($"{key} = \"{entry.String}\"");
                    }
#endif
                }
                if (x >= OptionsKeyNames.Length)
                {
                    Debug.LogWarning($"{modInfo.assemblyName}: No LocStrings ({string.Join(", ", OptionsKeyNames)}) provided for Options '{type.FullName}'");
                }
            }
            var nestedTypes = type.GetNestedTypes(LocString.data_member_fields);
            foreach (var nestedType in nestedTypes)
            {
                CreateEmptyLocStringKeys(nestedType, path, depth + 1);
            }
        }

        // замена текста в загруженной локализации
        // todo: старый вариант. оставить для совместимости, потом постепенно убрать.
        [Obsolete("need to replace it to 'ReplaceText'", false)]
        public static void ReplaceLocString(ref LocString locString, string search, string replacement)
        {
            LocString newlocString = new LocString(locString.text.Replace(search, replacement), locString.key.String);
            locString = newlocString;
        }
        [Obsolete("need to replace it to 'ReplaceText'", false)]
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
            locString.ReplaceText(locString.text.Replace(search, replacement));
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
                    foreach (var replacement in replacementDictionary)
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
