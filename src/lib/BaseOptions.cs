using System.IO;
using PeterHan.PLib.Options;

namespace SanchozzONIMods.Lib
{
    public class BaseOptions<Options> where Options : BaseOptions<Options>, new()
    {
        private static Options instance;

        public static Options Instance
        {
            get
            {
                if (instance == null)
                {
                    Reload();
                }
                return instance;
            }
        }

        public static void Reload()
        {
            instance = null;
            if (File.Exists(POptions.GetConfigFilePath(typeof(Option))))
                instance = POptions.ReadSettings<Options>();
            instance = instance ?? new Options();
            ClampAndRound(instance);
        }

        private static void ClampAndRound(object options)
        {
            if (options != null)
            {
                var optionsType = options.GetType();
                if (optionsType.IsClass)
                {
                    foreach (var property in optionsType.GetProperties())
                    {
                        // клампинг в соответствии с лимитами
                        foreach (var attribute in property.GetCustomAttributes(typeof(LimitAttribute), false))
                        {
                            if (attribute != null && attribute is LimitAttribute limit)
                            {
                                switch (property.GetValue(options, null))
                                {
                                    case int value:
                                        property.SetValue(options, limit.ClampToRange(value), null);
                                        break;
                                    case float value:
                                        property.SetValue(options, limit.ClampToRange(value), null);
                                        break;
                                    case double value:
                                        property.SetValue(options, (double)limit.ClampToRange((float)value), null);
                                        break;
                                }
                            }
                        }
                        // округление в соответствии с форматами
                        foreach (var attribute in property.GetCustomAttributes(typeof(OptionAttribute), false))
                        {
                            if (attribute != null && attribute is OptionAttribute oa && !oa.Format.IsNullOrWhiteSpace())
                            {
                                switch (property.GetValue(options, null))
                                {
                                    case int value:
                                        if (int.TryParse(value.ToString(oa.Format), out value))
                                            property.SetValue(options, value, null);
                                        break;
                                    case float value:
                                        if (float.TryParse(value.ToString(oa.Format), out value))
                                            property.SetValue(options, value, null);
                                        break;
                                    case double value:
                                        if (double.TryParse(value.ToString(oa.Format), out value))
                                            property.SetValue(options, value, null);
                                        break;
                                }
                            }
                        }
                        // обработка дочерних типов
                        {
                            var value = property.GetValue(options, null);
                            if (value != null)
                            {
                                var valueType = value.GetType();
                                if (valueType.IsClass && valueType.IsNested && valueType.DeclaringType == optionsType)
                                {
                                    ClampAndRound(value);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}