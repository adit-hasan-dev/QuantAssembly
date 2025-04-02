using Newtonsoft.Json;

namespace QuantAssembly.Common.Config
{
    public static class ConfigurationLoader
    {
        public const string DefaultConfigPath = "appsettings.json";
        public static T LoadConfiguration<T>(string filePath = DefaultConfigPath) where T : BaseConfig
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
        }
    }
}