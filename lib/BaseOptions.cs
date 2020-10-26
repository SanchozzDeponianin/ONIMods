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
            instance = POptions.ReadSettings<Options>() ?? new Options();
        }
    }
}