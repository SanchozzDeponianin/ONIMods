using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
#if USESPLIB
using PeterHan.PLib.Core;
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
            public readonly string fileVersion;
            public ModInfo()
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                assemblyName = assembly.GetName().Name;
                rootDirectory = Path.GetDirectoryName(assembly.Location);
                langDirectory = Path.Combine(rootDirectory, "translations");
                spritesDirectory = Path.Combine(rootDirectory, "sprites");
                version = assembly.GetName().Version.ToString();
                fileVersion = null;
                var attrs = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true);
                if (attrs != null && attrs.Length > 0)
                {
                    var assemblyFileVersion = (AssemblyFileVersionAttribute)attrs[0];
                    if (assemblyFileVersion != null)
                        fileVersion = assemblyFileVersion.Version;
                }
            }
        }

        private static ModInfo _modinfo;

        public static ModInfo modInfo
        {
            get
            {
                if (_modinfo == null)
                    _modinfo = new ModInfo();
                return _modinfo;
            }
        }

        public static void LogModVersion()
        {
            Debug.LogFormat("Mod {0} initialized, version {1}", modInfo.assemblyName, modInfo.fileVersion ?? "Unknown");
        }

        // извлекаем значение константы
        public static uint GameVersion { get; }

        // вычисляем значение Action.NumActions определенную в Assembly-CSharp-firstpass
        // напрямую использовать Action.NumActions может выйти боком 
        // если клей изменят enum а мод был не перекомпилирован
        public static Action MaxAction { get; }
        static Utils()
        {
            if (!Enum.TryParse(nameof(Action.NumActions), out Action limit))
            {
                var actions = Enum.GetValues(typeof(Action));
                if (actions.Length > 0)
                    limit = (Action)actions.GetValue(actions.Length - 1);
                else
                    limit = Action.NumActions;
            }
            MaxAction = limit;

            GameVersion = 0u;
            var field = typeof(KleiVersion).GetField(nameof(KleiVersion.ChangeList));
            if (field != null && field.GetRawConstantValue() is uint value)
                GameVersion = value;
        }

        // логирование со стактрасом
        public static void LogExcWarn(Exception thrown)
        {
            Debug.LogWarningFormat("[{0}] {1} {2}\n{3}", (Assembly.GetCallingAssembly()?.GetName()?.Name) ?? "?",
                thrown.GetType(), thrown.Message, thrown.StackTrace);
        }

        // добавляем постройки в технологии
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
            AccessTools.Method(locstring_tree_root, "DoReplacement", new Type[0])
                ?.Invoke(null, null);

            CreateOptionsLocStringKeys(locstring_tree_root);
        }

        public static void CreateOptionsLocStringKeys(Type locstring_tree_root)
        { 
            // дополнительно создаем ключи специально для опций.
            // ключи должны получиться в формате 
            // "STRINGS.{Namespace}.OPTIONS.{Name}.XXX"
            // где ХХХ = NAME TOOLTIP или CATEGORY соотвественно
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
        private static readonly FieldInfo LocStringText = typeof(LocString).GetField("_text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        public static void ReplaceText(this LocString locString, string newtext)
        {
            if (LocStringText != null)
                LocStringText.SetValue(locString, newtext);
            else
            {
                var message = "Could not write the '{1}' text into the '{0}' LocString";
#if USESPLIB
                PUtil.LogWarning(message.F(locString.key, newtext));
#else
                Debug.LogWarningFormat(message, locString.key, newtext);
#endif
            }
        }

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

        // загружаем таблицы звуков
#if USESPLIB
        private delegate void CreateAllSoundsDelegate(AudioSheets sheet, string animFile, AudioSheet.SoundInfo info, string defaultType);
        private static readonly DetouredMethod<CreateAllSoundsDelegate> CREATE_SOUND = typeof(PGameUtils).DetourLazy<CreateAllSoundsDelegate>("CreateAllSounds");

        public static void LoadAudioSheet(string csvText, string name, string defaultType)
        {
            var audioSheet = GameAudioSheets.Get();
#if DEBUG
            foreach (var sheet in audioSheet.sheets)
                Debug.Log($"name = {sheet.asset.name} , defaultType = {sheet.defaultType}");
#endif
            var soundInfos = new ResourceLoader<AudioSheet.SoundInfo>(csvText, name).resources;
            foreach (var info in soundInfos)
            {
                try
                {
                    CREATE_SOUND.Invoke(audioSheet, info.File, info, !string.IsNullOrEmpty(info.Type) ? info.Type : defaultType);
                }
                catch (Exception e)
                {
                    LogExcWarn(e);
                }
            }
        }

        public static void LoadEmbeddedAudioSheet(string path, string name, string defaultType = "SoundEvent")
        {
            var assembly = Assembly.GetCallingAssembly();
            try
            {
                using (var stream = assembly.GetManifestResourceStream(path))
                {
                    if (stream == null)
                    {
                        Debug.LogWarningFormat($"Could not load AudioSheet: {0}", path);
                        return;
                    }
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var text = reader.ReadToEnd();
                        LoadAudioSheet(text, name, defaultType);
                    }
                }
            }
            catch (Exception e)
            {
                LogExcWarn(e);
            }
        }

        public static void LoadAudioSheetFromFile(string path, string name, string defaultType = "SoundEvent")
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarningFormat($"Could not load AudioSheet: {0}", path);
                    return;
                }
                var text = File.ReadAllText(path, Encoding.UTF8);
                LoadAudioSheet(text, name, defaultType);
            }
            catch (Exception e)
            {
                LogExcWarn(e);
            }
        }
#endif

        // предотвращаем разговоры при проигрывании этих анимаций
        public static void MuteMouthFlapSpeech(HashedString kanim_file, params HashedString[] anims)
        {
            var sheet = GameAudioSheets.Get();
            var muted_kanims = Traverse.Create(sheet).Field<Dictionary<HashedString, HashSet<HashedString>>>("animsNotAllowedToPlaySpeech").Value;
            if (!muted_kanims.TryGetValue(kanim_file, out var hashset))
            {
                hashset = new HashSet<HashedString>();
                muted_kanims[kanim_file] = hashset;
            }
            foreach (var anim_name in anims)
                hashset.Add(anim_name);
        }

        public static void MuteMouthFlapSpeech(params Klei.AI.Emote[] muted_emotes)
        {
            foreach (var emote in muted_emotes)
            {
                if (emote.AnimSet != null && emote.StepCount > 0)
                {
                    HashedString kanim_name = emote.AnimSet.name;
                    emote.CollectStepAnims(out var anims, 1);
                    MuteMouthFlapSpeech(kanim_name, anims);
                }
            }
        }
    }

    public static class BUILD_CATEGORY
    {
        public const string Base = nameof(Base);
        public const string Oxygen = nameof(Oxygen);
        public const string Power = nameof(Power);
        public const string Food = nameof(Food);
        public const string Plumbing = nameof(Plumbing);
        public const string HVAC = nameof(HVAC);
        public const string Refining = nameof(Refining);
        public const string Medical = nameof(Medical);
        public const string Furniture = nameof(Furniture);
        public const string Equipment = nameof(Equipment);
        public const string Utilities = nameof(Utilities);
        public const string Automation = nameof(Automation);
        public const string Conveyance = nameof(Conveyance);
        public const string Rocketry = nameof(Rocketry);
        public const string HEP = nameof(HEP);
    }

    public static class BUILD_SUBCATEGORY
    {
        public const string UNCATEGORIZED = "uncategorized";
        public const string ladders = nameof(ladders);
        public const string tiles = nameof(tiles);
        public const string printingpods = nameof(printingpods);
        public const string doors = nameof(doors);
        public const string storage = nameof(storage);
        public const string transport = nameof(transport);
        public const string producers = nameof(producers);
        public const string scrubbers = nameof(scrubbers);
        public const string generators = nameof(generators);
        public const string wires = nameof(wires);
        public const string batteries = nameof(batteries);
        public const string powercontrol = nameof(powercontrol);
        public const string switches = nameof(switches);
        public const string cooking = nameof(cooking);
        public const string farming = nameof(farming);
        public const string ranching = nameof(ranching);
        public const string washroom = nameof(washroom);
        public const string pipes = nameof(pipes);
        public const string pumps = nameof(pumps);
        public const string valves = nameof(valves);
        public const string sensors = nameof(sensors);
        public const string buildmenuports = nameof(buildmenuports);
        public const string materials = nameof(materials);
        public const string oil = nameof(oil);
        public const string advanced = nameof(advanced);
        public const string hygiene = nameof(hygiene);
        public const string medical = nameof(medical);
        public const string wellness = nameof(wellness);
        public const string beds = nameof(beds);
        public const string lights = nameof(lights);
        public const string dining = nameof(dining);
        public const string recreation = nameof(recreation);
        public const string decor = nameof(decor);
        public const string research = nameof(research);
        public const string archaeology = nameof(archaeology);
        public const string industrialstation = nameof(industrialstation);
        public const string manufacturing = nameof(manufacturing);
        public const string equipment = nameof(equipment);
        public const string temperature = nameof(temperature);
        public const string sanitation = nameof(sanitation);
        public const string logicmanager = nameof(logicmanager);
        public const string logicaudio = nameof(logicaudio);
        public const string logicgates = nameof(logicgates);
        public const string transmissions = nameof(transmissions);
        public const string conveyancestructures = nameof(conveyancestructures);
        public const string automated = nameof(automated);
        public const string telescopes = nameof(telescopes);
        public const string rocketstructures = nameof(rocketstructures);
        public const string rocketnav = nameof(rocketnav);
        public const string engines = nameof(engines);
        public const string fuel_and_oxidizer = "fuel and oxidizer";
        public const string cargo = nameof(cargo);
        public const string utility = nameof(utility);
        public const string command = nameof(command);
        public const string fittings = nameof(fittings);
    }
}
