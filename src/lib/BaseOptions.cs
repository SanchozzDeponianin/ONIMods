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
            // todo: нужно добавить обработку новых возможностей PLib.Options - дочерние типы
            instance = POptions.ReadSettings<Options>() ?? new Options();
            foreach (var property in typeof(Options).GetProperties())
            {
                // клампинг в соответствии с лимитами
                foreach (var attribute in property.GetCustomAttributes(typeof(LimitAttribute), false))
                {
                    if (attribute != null && attribute is LimitAttribute limit)
                    {
                        switch (property.GetValue(instance, null))
                        {
                            case int value:
                                property.SetValue(instance, limit.ClampToRange(value), null);
                                break;
                            case float value:
                                property.SetValue(instance, limit.ClampToRange(value), null);
                                break;
                            case double value:
                                property.SetValue(instance, (double)limit.ClampToRange((float)value), null);
                                break;
                        }
                    }
                }
                // округление в соответствии с форматами
                foreach (var attribute in property.GetCustomAttributes(typeof(OptionAttribute), false))
                {
                    if (attribute != null && attribute is OptionAttribute option && !option.Format.IsNullOrWhiteSpace())
                    {
                        switch (property.GetValue(instance, null))
                        {
                            case int value:
                                if (int.TryParse(value.ToString(option.Format), out value))
                                    property.SetValue(instance, value, null);
                                break;
                            case float value:
                                if (float.TryParse(value.ToString(option.Format), out value))
                                    property.SetValue(instance, value, null);
                                break;
                            case double value:
                                if (double.TryParse(value.ToString(option.Format), out value))
                                    property.SetValue(instance, value, null);
                                break;
                        }
                    }
                }
            }
        }
    }
}