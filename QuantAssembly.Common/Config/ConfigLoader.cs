using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuantAssembly.Common.Config
{
    public static class ConfigurationLoader
    {
        public const string DefaultConfigPath = "appsettings.json";
        public static T LoadConfiguration<T>(string filePath = DefaultConfigPath) where T : class, new()
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
        }
    }
}