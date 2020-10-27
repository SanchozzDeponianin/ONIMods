using System.IO;
using Newtonsoft.Json;

namespace SanchozzONIMods.Lib
{
    public class BaseConfig<Config> where Config : BaseConfig<Config>, new()
    {
        private static Config Instance;
        public static Config Get()
        {
            if (Instance == null)
            {
                Instance = new Config();
            }
            return Instance;
        }
        
        public static void ReadConfig(string filename)
        {
            try
            {
                using (JsonTextReader jsonTextReader = new JsonTextReader(File.OpenText(filename)))
                {
                    Instance = new JsonSerializer { MaxDepth = new int?(8) }.Deserialize<Config>(jsonTextReader);
                }
            }
            catch (FileNotFoundException)
            {
            }
            catch (IOException thrown)
            {
                Utils.LogExcWarn(thrown);
            }
            catch (JsonException thrown2)
            {
                Utils.LogExcWarn(thrown2);
            }
        }

        public static void WriteConfig(string filename)
        {
            try
            {
                using (JsonTextWriter jsonTextWriter = new JsonTextWriter(File.CreateText(filename)))
                {
                    jsonTextWriter.Formatting = Formatting.Indented;
                    new JsonSerializer { MaxDepth = new int?(8) }.Serialize(jsonTextWriter, Get());
                }
            }
            catch (IOException thrown)
            {
                Utils.LogExcWarn(thrown);
            }
            catch (JsonException thrown2)
            {
                Utils.LogExcWarn(thrown2);
            }
        }

        public static void Initialize()
        {
            string configFile = Path.Combine(Utils.modInfo.rootDirectory, "config.json");
            if (File.Exists(configFile))
            {
                ReadConfig(configFile);
            }
            WriteConfig(configFile);
        }
    }
}
