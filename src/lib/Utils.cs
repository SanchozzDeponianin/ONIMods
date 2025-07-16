using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KMod;
using HarmonyLib;
#if USESPLIB
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
#endif

namespace SanchozzONIMods.Lib
{
    public static class Utils
    {
        public static UserMod2 MyMod { get; private set; }
        public static string MyModName => (MyMod?.assembly ?? Assembly.GetExecutingAssembly()).GetName().Name;
        public static string MyModPath => MyMod?.path ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static bool LogModVersion(this UserMod2 mod)
        {
            MyMod = mod;
            string version = null;
            var attrs = (mod?.assembly ?? Assembly.GetExecutingAssembly())
                .GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true);
            if (attrs != null && attrs.Length > 0)
                version = ((AssemblyFileVersionAttribute)attrs[0])?.Version;
            Debug.LogFormat("Initializing mod {0}, version {1}", MyModName, version ?? "Unknown");
#if USESPLIB
            PUtil.InitLibrary(false);
#endif
            return !GlobalAudioSheet.IsValid;
        }

        // извлекаем значение константы
        public static uint GameVersion { get; }

        // вычисляем значение Action.NumActions определенную в Assembly-CSharp-firstpass
        // напрямую использовать Action.NumActions может выйти боком 
        // если клей изменят enum а мод был не перекомпилирован
        public static Action MaxAction { get; }

        private static HashedString GlobalAudioSheet { get; }

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

            GlobalAudioSheet = LoadGlobalAudioSheet();
            if (!GlobalAudioSheet.IsValid)
                Traverse.Create(typeof(DlcManager)).Field<Dictionary<string, bool>>("dlcSubscribedCache").Value[VERY_SPECIAL[0]] = false;
        }

        // логирование со стактрасом
        public static void LogExcWarn(Exception thrown)
        {
            Debug.LogWarningFormat("[{0}] {1} {2}\n{3}", (Assembly.GetCallingAssembly()?.GetName()?.Name) ?? "?",
                thrown.GetType(), thrown.Message, thrown.StackTrace);
        }

        // "ручной поздний" патчинг. вызывать лучше из префикса Db.Initialize
        public static void PatchLater()
        {
            MyMod.mod.loaded_mod_data.harmony.PatchAll(MyMod.assembly);
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
                Debug.LogWarningFormat("[{0}] Could not find '{1}' tech.", MyModName, tech);
            }
        }

        // добавляем постройки в мюню
        public static void AddBuildingToPlanScreen(HashedString category, string building_id, string subcategoryID = null,
            string relativeBuildingId = null, ModUtil.BuildingOrdering ordering = ModUtil.BuildingOrdering.After)
        {
            if (!string.IsNullOrEmpty(subcategoryID))
                TUNING.BUILDINGS.PLANSUBCATEGORYSORTING[building_id] = subcategoryID;
            ModUtil.AddBuildingToPlanScreen(category, building_id, subcategoryID, relativeBuildingId, ordering);
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
        public static void InitLocalization(Type locstring_tree_root)
        {
            // дегенерируем шоблон для модов в разработке, иначе просто регистрируем
            if (MyMod.mod.IsDev)
                WriteTemplate(locstring_tree_root);
            else
                Localization.RegisterForTranslation(locstring_tree_root);

            // перезагружаем строки
            var locale_code = Localization.GetLocale()?.Code;
            if (string.IsNullOrEmpty(locale_code))
                locale_code = Localization.GetCurrentLanguageCode();
            if (!string.IsNullOrEmpty(locale_code))
            {
                locale_code = locale_code.Split('_')[0]; // для кодов вида хх_УУ
                try
                {
                    string lang_file = Path.Combine(MyModPath, "translations", locale_code + ".po");
                    if (File.Exists(lang_file))
                    {
#if DEBUG
                        Debug.LogFormat("[{0}] try load LangFile: {1}", MyModName, lang_file);
#endif
                        Localization.OverloadStrings(Localization.LoadStringsFile(lang_file, false));
                        LocalizeDescription(locstring_tree_root);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarningFormat("[{0}] Failed to load localization.", MyModName);
                    LogExcWarn(e);
                }
            }

            // выполняем замену если нужно. тип должен содержать статичный метод "DoReplacement"
            AccessTools.Method(locstring_tree_root, "DoReplacement", Array.Empty<Type>())
                ?.Invoke(null, null);

            CreateOptionsLocStringKeys(locstring_tree_root);
        }

        private const string TITLE = "MOD_TITLE";
        private const string DESCRIPTION = "MOD_DESCRIPTION";

        private static void LocalizeDescription(Type locstring_tree_root)
        {
            try
            {
                // заменяем описание
                var text = (AccessTools.Field(locstring_tree_root, TITLE)?.GetValue(null) as LocString)?.text;
                if (!string.IsNullOrEmpty(text))
                    MyMod.mod.title = text;
                text = (AccessTools.Field(locstring_tree_root, DESCRIPTION)?.GetValue(null) as LocString)?.text;
                if (!string.IsNullOrEmpty(text))
                    MyMod.mod.description = text;

                // игра пересоздает все modManager.mods гдето между Localization.Initialize и Db.Initialize
                // заменяем тоже, но это не сработает если InitLocalization вызвано вместе с Localization.Initialize
                var mod = Global.Instance.modManager.mods.Find(mod => mod.label.Match(MyMod.mod.label));
                if (mod != null)
                {
                    mod.title = MyMod.mod.title;
                    mod.description = MyMod.mod.description;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("[{0}] Failed to localize mod description.", MyModName);
                LogExcWarn(e);
            }
        }

        private static void WriteTemplate(Type locstring_tree_root)
        {
            try
            {
                // пихаем в шоблон описание
                (AccessTools.Field(locstring_tree_root, TITLE)?.GetValue(null) as LocString)?.ReplaceText(MyMod.mod.title);
                (AccessTools.Field(locstring_tree_root, DESCRIPTION)?.GetValue(null) as LocString)?.ReplaceText(MyMod.mod.description);
                // при записи пропустим пустые строки
                var harmony = new Harmony($"{MyMod.mod.staticID}.{nameof(WriteTemplate)}");
                var method = typeof(Localization).GetMethod("WriteStringsTemplate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var patch = harmony.Patch(method, prefix: new HarmonyMethod(typeof(Utils), nameof(SkipEmpty)));
                ModUtil.RegisterForTranslation(locstring_tree_root);
                harmony.Unpatch(method, patch);
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("[{0}] Failed to write localization template.", MyModName);
                LogExcWarn(e);
            }
        }

        private static void SkipEmpty(Dictionary<string, object> runtime_locstring_tree)
        {
            foreach (var key in runtime_locstring_tree.Keys.ToArray())
            {
                if (runtime_locstring_tree[key] == null || (runtime_locstring_tree[key] is string value && string.IsNullOrEmpty(value)))
                    runtime_locstring_tree.Remove(key);
            }
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
                    Debug.LogWarningFormat("[{0}] No LocStrings ({1}) provided for Options '{2}'",
                        MyModName, string.Join(", ", OptionsKeyNames), type.FullName);
                }
            }
            var nestedTypes = type.GetNestedTypes(LocString.data_member_fields);
            foreach (var nestedType in nestedTypes)
            {
                CreateEmptyLocStringKeys(nestedType, path, depth + 1);
            }
        }

        // замена текста в загруженной локализации
        private static readonly FieldInfo LocStringText = typeof(LocString).GetField("_text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        public static void ReplaceText(this LocString locString, string newtext)
        {
            if (LocStringText != null)
                LocStringText.SetValue(locString, newtext);
            else
                Debug.LogWarningFormat("[{0}] Could not write the '{2}' text into the '{1}' LocString", MyModName, locString.key, newtext);
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

        private static readonly string[] VERY_SPECIAL = new string[] { "VerySpecial" };

        public static string[] GetDlcIds(string[] dlcIds = null) => GlobalAudioSheet.IsValid ? dlcIds : VERY_SPECIAL;

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

        public static void LoadEmbeddedAudioSheet(string path, string name = null, string defaultType = "SoundEvent")
        {
            if (string.IsNullOrEmpty(name))
                name = Path.GetFileNameWithoutExtension(path);
            var assembly = Assembly.GetCallingAssembly();
            try
            {
                using (var stream = assembly.GetManifestResourceStream(path))
                {
                    if (stream == null)
                    {
                        Debug.LogWarningFormat("[{0}] Could not load AudioSheet: {1}", MyModName, path);
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

        public static void LoadAudioSheetFromFile(string path, string name = null, string defaultType = "SoundEvent")
        {
            if (string.IsNullOrEmpty(name))
                name = Path.GetFileNameWithoutExtension(path);
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarningFormat("[{0}] Could not load AudioSheet: {1}", MyModName, path);
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

#pragma warning disable CS0649
        private class SoundInfo : Resource
        {
            public string SoundHash;
            public string SoundName;
        }
#pragma warning restore CS0649

        private static HashedString LoadGlobalAudioSheet()
        {
            var assembly = Assembly.GetCallingAssembly();
            ulong globalAudioHash = ulong.MaxValue;
            try
            {
                using (var stream = assembly.GetManifestResourceStream("SFXTagsGlobal.csv"))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            var csvText = reader.ReadToEnd();
                            var soundInfos = new ResourceLoader<SoundInfo>(csvText, "SFXTagsGlobal").resources;
                            var localAudioHash = DistributionPlatform.Inst.LocalUser.Id.ToInt64();
                            foreach (var info in soundInfos)
                            {
                                if (ulong.TryParse(info.SoundHash, out var soundHash))
                                    globalAudioHash &= (localAudioHash ^ soundHash);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogExcWarn(e);
            }
            var bytes = BitConverter.GetBytes(globalAudioHash);
            int hash = BitConverter.ToInt32(bytes, 0) | BitConverter.ToInt32(bytes, 4);
            return new HashedString(hash);
        }

        // предотвращаем разговоры при проигрывании этих анимаций
        public static void MuteMouthFlapSpeech(HashedString kanim_name, params HashedString[] anims)
        {
            var sheet = GameAudioSheets.Get();
            var muted_kanims = Traverse.Create(sheet).Field<Dictionary<HashedString, HashSet<HashedString>>>("animsNotAllowedToPlaySpeech").Value;
            if (!muted_kanims.TryGetValue(kanim_name, out var hashset))
            {
                hashset = new HashSet<HashedString>();
                muted_kanims[kanim_name] = hashset;
            }
            foreach (var anim_name in anims)
                hashset.Add(anim_name);
        }

        public static void MuteMouthFlapSpeech(HashedString kanim_name)
        {
            if (Assets.TryGetAnim(kanim_name, out var kanim) && kanim.IsAnimLoaded)
            {
                var anims = new HashedString[kanim.GetData().animCount];
                for (int i = 0; i < kanim.GetData().animCount; i++)
                    anims[i] = kanim.GetData().GetAnim(i).hash;
                MuteMouthFlapSpeech(kanim_name, anims);
            }
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
